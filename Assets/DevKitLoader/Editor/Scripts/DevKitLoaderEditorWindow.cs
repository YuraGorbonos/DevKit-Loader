using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DevKitLoader.Editor
{
    public class DevKitLoaderEditorWindow : EditorWindow
    {
        [MenuItem("DevKit Loader/Open Editor")]
        public static void ShowWindow()
        {
            var win = GetWindow<DevKitLoaderEditorWindow>("DevKit Loader");
            win.minSize = new Vector2(360, 180);
        }

        private void OnGUI()
        {
            GUILayout.Label("DevKit Loader Editor", EditorStyles.boldLabel);
            GUILayout.Space(6);

            if (GUILayout.Button("Create .firstrun marker"))
            {
                CreateFirstRunMarker();
            }

            if (GUILayout.Button("Check Libs & .firstrun"))
            {
                CheckStatus();
            }
        }

        private static void CreateFirstRunMarker()
        {
            string markerPath = "Assets/DevKitLoader/.firstrun";
            string dir = Path.GetDirectoryName(markerPath);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(markerPath))
            {
                File.WriteAllText(markerPath, "firstrun marker created at " + DateTime.Now.ToString("o"));
                AssetDatabase.ImportAsset(markerPath);
                Debug.Log("Created " + markerPath);
            }
            else
            {
                Debug.Log("Marker already exists: " + markerPath);
            }
        }

        private static void CheckStatus()
        {
            string dllPath = "Assets/DevKitLoader/Editor/Libs/SharpCompress.dll";
            bool hasDll = File.Exists(dllPath);
            string markerPath = "Assets/DevKitLoader/.firstrun";
            bool hasMarker = File.Exists(markerPath);
            string message = $"SharpCompress.dll: {(hasDll ? "found" : "missing")} | .firstrun: {(hasMarker ? "present" : "missing")}";
            Debug.Log(message);
            EditorUtility.DisplayDialog("DevKit Loader Status", message, "OK");
        }
    }
}