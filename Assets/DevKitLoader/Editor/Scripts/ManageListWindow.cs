using UnityEditor;
using UnityEngine;

namespace DevKitLoader
{
    public class ManageListWindow : EditorWindow
    {
        public static void Open()
        {
            GetWindow<ManageListWindow>("DevKit Manage List");
        }

        private void OnGUI()
        {
            GUILayout.Label("Manage List Window (will be implemented later)");
        }
    }
}