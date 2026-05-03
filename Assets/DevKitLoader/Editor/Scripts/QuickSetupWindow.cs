using UnityEditor;
using UnityEngine;

namespace DevKitLoader
{
    public class QuickSetupWindow : EditorWindow
    {
        public static void Open()
        {
            GetWindow<QuickSetupWindow>("DevKit Quick Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("Quick Setup Window (will be implemented later)");
        }
    }
}