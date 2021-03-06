﻿using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityDebugViewer
{
    public static class UnityDebugViewerWindowUtility 
    {
        public static int activeControlID = 0;

        public static string CopyPasteTextField(string value, GUIStyle style, params GUILayoutOption[] options)
        {
            int textFieldID = GUIUtility.GetControlID("TextField".GetHashCode(), FocusType.Keyboard) + 1;
            if (textFieldID == 0)
            {
                return value;
            }

            // Handle custom copy-paste
            value = HandleCopyPaste(textFieldID) ?? value;
            
            string text = GUILayout.TextField(value, style, options);

            if (GUILayout.Button("", UnityDebugViewerWindowStyleUtility.toolbarCancelButtonStyle))
            {
                text = string.Empty;
            }

            return text;
        }
        public static string HandleCopyPaste(int controlID)
        {
            EventType eventType = Event.current.GetTypeForControl(controlID);
            if (controlID == GUIUtility.keyboardControl)
            {
#if UNITY_5 || UNITY_5_2_OR_NEWER
                if (eventType == EventType.KeyUp && (Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Command))
#else
                if (eventType == EventType.KeyUp && (Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Command))
#endif
                {
                    if (Event.current.keyCode == KeyCode.C)
                    {
                        Event.current.Use();
                        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        editor.Copy();
                    }
                    else if (Event.current.keyCode == KeyCode.V)
                    {
                        Event.current.Use();
                        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        editor.Paste();
#if UNITY_5_3_OR_NEWER || UNITY_5_3
                        return editor.text; 
#else
                        return editor.content.text;
#endif
                    }
                    else if(Event.current.keyCode == KeyCode.A)
                    {
                        Event.current.Use();
                        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        editor.SelectAll();
                    }
                    else if(Event.current.keyCode == KeyCode.X)
                    {
                        Event.current.Use();
                        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        editor.Copy();
                        editor.DeleteSelection();
#if UNITY_5_3_OR_NEWER || UNITY_5_3
                        return editor.text;
#else
                        return editor.content.text;
#endif
                    }
                }
#if UNITY_5_3_OR_NEWER || UNITY_5_3
                else if (eventType == EventType.Ignore)
#else
                else if (eventType == EventType.ignore)
#endif
                {
                    GUI.FocusControl(null);
                }
            }

            return null;
        }


        public static bool JumpToSource(LogData log)
        {
            if (log != null)
            {
                for (int i = 0; i < log.stackList.Count; i++)
                {
                    var stack = log.stackList[i];
                    if (stack == null)
                    {
                        continue;
                    }

                    if (JumpToSource(stack))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool JumpToSource(LogStackData stack)
        {
            if (stack == null)
            {
                return false;
            }
            else
            {
                return JumpToSource(stack.filePath, stack.lineNumber);
            }
        }

        public static bool JumpToSource(string filePath, int lineNumber)
        {
            var validFilePath = UnityDebugViewerEditorUtility.ConvertToSystemFilePath(filePath);
            if (File.Exists(validFilePath))
            {
                if (InternalEditorUtility.OpenFileAtLineExternal(validFilePath, lineNumber))
                {
                    return true;
                }
            }

            return false;
        }

        public static void ClearNativeConsoleWindow()
        {
            Assembly unityEditorAssembly = Assembly.GetAssembly(typeof(EditorWindow));
            Type logEntriesType = unityEditorAssembly.GetType("UnityEditor.LogEntries");
            if (logEntriesType == null)
            {
                logEntriesType = unityEditorAssembly.GetType("UnityEditorInternal.LogEntries");
                if (logEntriesType == null)
                {
                    return;
                }
            }

            object logEntriesInstance = Activator.CreateInstance(logEntriesType);
            var clearMethod = logEntriesType.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (clearMethod != null)
            {
                clearMethod.Invoke(logEntriesInstance, null);
            }

            var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (getCountMethod == null)
            {
                return;
            }

            int count = (int)getCountMethod.Invoke(logEntriesInstance, null);
            if (count > 0)
            {
                Type logEntryType = unityEditorAssembly.GetType("UnityEditor.LogEntry");
                if (logEntryType == null)
                {
                    logEntryType = unityEditorAssembly.GetType("UnityEditorInternal.LogEntry");
                    if (logEntryType == null)
                    {
                        return;
                    }
                }
                object logEntryInstacne = Activator.CreateInstance(logEntryType);

                var startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (startGettingEntriesMethod == null || endGettingEntriesMethod == null || getEntryInternalMethod == null)
                {
                    return;
                }

                var infoFieldInfo = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (infoFieldInfo == null)
                {
                    infoFieldInfo = logEntryType.GetField("condition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (infoFieldInfo == null)
                    {
                        return;
                    }
                }

                string info;
                startGettingEntriesMethod.Invoke(logEntriesInstance, null);
                for (int i = 0; i < count; i++)
                {
                    getEntryInternalMethod.Invoke(logEntriesInstance, new object[] { i, logEntryInstacne });
                    if (logEntryInstacne == null)
                    {
                        continue;
                    }

                    info = infoFieldInfo.GetValue(logEntryInstacne).ToString();
                    UnityDebugViewerLogger.AddLog(info, string.Empty, LogType.Error, UnityDebugViewerDefaultMode.Editor);
                }
                endGettingEntriesMethod.Invoke(logEntriesInstance, null);
            }
        }


        public static bool ShouldRectShow(Rect showRect, Rect rect, bool showComplete = false)
        {
            float rectTop = rect.y + rect.height;
            float rectBottom = rect.y ;
            float showTop = showRect.y + showRect.height;
            float showBottom = showRect.y;

            return ShouldRectShow(showTop, showBottom, rectTop, rectBottom, showComplete);
        }

        public static bool ShouldRectShow(float showTop, float showBottom, float rectTop, float rectBottom, bool showComplete = false)
        {
            if (showComplete)
            {
                float rectHeight = rectTop - rectBottom;
                showTop -= rectHeight;
                showBottom += rectHeight;
            }

            return !(rectTop <= showBottom || rectBottom >= showTop);
        }

        public static void MoveToSpecificRect(Rect showRect, Rect rect, ref Vector2 scrollPos)
        {
            if(ShouldRectShow(showRect, rect, true))
            {
                return;
            }

            float rectTop = rect.y + rect.height;
            float rectBottom = rect.y;

            float showRectTop = showRect.y + showRect.height;
            float showRectBottom = showRect.y;

            MoveToSpecificRect(showRectTop, showRectBottom, rectTop, rectBottom, ref scrollPos);
        }

        public static void MoveToSpecificRect(float showRectTop, float showRectBottom, float rectTop, float rectBottom, ref Vector2 scrollPos)
        {
            if (ShouldRectShow(showRectTop, showRectBottom, rectTop, rectBottom, true))
            {
                return;
            }

            float topDistance = showRectTop - rectTop;
            float bottomDistance = showRectBottom - rectBottom;
            float moveDistacne = Mathf.Abs(topDistance) > Mathf.Abs(bottomDistance) ? bottomDistance : topDistance;

            scrollPos.y -= moveDistacne;
        }

        public static bool CheckADBStatus()
        {
            string adbPath = GetAdbPath();

            if (string.IsNullOrEmpty(adbPath))
            {
                EditorUtility.DisplayDialog("Unity Debug Viewer", "Cannot find adb path", "OK");
                return false;
            }

            if (UnityDebugViewerADBUtility.CheckDevice(adbPath) == false)
            {
                EditorUtility.DisplayDialog("Unity Debug Viewer", "Cannot detect any connected devices", "OK");
                return false;
            }

            return true;
        }


        private static string adbPath = string.Empty;
        public static string GetAdbPath()
        {
            if (!String.IsNullOrEmpty(adbPath))
            {
                return adbPath;
            }

#if UNITY_2019_1_OR_NEWER
            ADB adb = ADB.GetInstance();
            if(abd != null)
            {
                adbPath = adb.GetADBPath();
            }
#else
            string androidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot");
            if (!string.IsNullOrEmpty(androidSdkRoot))
            {
                adbPath = Path.Combine(androidSdkRoot, Path.Combine("platform-tools", "adb"));
            }
#endif

            if (string.IsNullOrEmpty(adbPath))
            {
                MonoScript ms = MonoScript.FromScriptableObject(UnityDebugViewerADBUtility.GetADBInstance());
                string filePath = AssetDatabase.GetAssetPath(ms);
                filePath = UnityDebugViewerEditorUtility.ConvertToSystemFilePath(filePath);

                string currentScriptDirectory = Path.GetDirectoryName(filePath);
                string parentDirectory = Directory.GetParent(currentScriptDirectory).FullName;
                parentDirectory = Directory.GetParent(parentDirectory).FullName;

                string defaultAdbPath = UnityDebugViewerEditorUtility.ConvertToSystemFilePath(UnityDebugViewerADBUtility.DEFAULT_ADB_PATH);

                adbPath = Path.Combine(Path.Combine(parentDirectory, defaultAdbPath), "adb");
            }

            return adbPath;
        }

        
        public static string GetSelectedFilePath()
        {
            string filePath = string.Empty;
            string defaultName = string.Format("{0}_logfile.log", DateTime.Now.ToString("MMdd_HHmmss"));
            filePath = EditorUtility.SaveFilePanel("Export log to file", Application.dataPath, defaultName, "log");

            return filePath;
        }
    }
}