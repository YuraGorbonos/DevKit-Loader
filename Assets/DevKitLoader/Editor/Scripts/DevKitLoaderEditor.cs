using System.IO;
using UnityEditor;

namespace DevKitLoader
{
    [InitializeOnLoad]
    public static class DevKitLoaderEditor
    {
        private const string _GlobalFirstRunKey = "DevKitLoader_FirstRun_Global";
        private const string _ProjectFlagPath = "Assets/DevKitLoader/.firstrun";

        static DevKitLoaderEditor()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            // Глобальный первый запуск
            if (!EditorPrefs.HasKey(_GlobalFirstRunKey))
            {
                EditorPrefs.SetBool(_GlobalFirstRunKey, true);
                OpenManageList();
                return;
            }

            // Первый запуск в проекте
            if (!File.Exists(_ProjectFlagPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_ProjectFlagPath) ?? string.Empty);
                File.WriteAllText(_ProjectFlagPath, "");
                AssetDatabase.Refresh();
                OpenQuickSetup();
            }
        }

        [MenuItem("Tools/DevKit Loader/Quick Setup")]
        public static void OpenQuickSetup()
        {
            QuickSetupWindow.Open();
        }

        [MenuItem("Tools/DevKit Loader/Manage List")]
        public static void OpenManageList()
        {
            ManageListWindow.Open();
        }
    }
}
