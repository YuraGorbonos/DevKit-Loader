using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace DevKitLoader
{
    public class QuickSetupWindow : EditorWindow
    {
        public static void Open()
        {
            var window = GetWindow<QuickSetupWindow>("DevKit Quick Setup");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }
    }
}
