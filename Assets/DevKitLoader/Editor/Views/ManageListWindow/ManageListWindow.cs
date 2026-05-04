using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace DevKitLoader
{
    public class ManageListWindow : EditorWindow
    {
        public static void Open()
        {
            var window = GetWindow<ManageListWindow>("DevKit Loader - Manage List");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }
    }
}
