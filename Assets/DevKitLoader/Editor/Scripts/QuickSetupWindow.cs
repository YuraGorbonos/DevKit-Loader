using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace DevKitLoader
{
    public class QuickSetupWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<bool> selectedStates = new List<bool>();
        private ToolList toolList;
        private bool isInstalling = false;
        private CancellationTokenSource cancellationTokenSource;
        private string currentProgressMessage = string.Empty;
        private float currentProgress = 0f;
        private List<string> errorMessages = new List<string>();
        private int successCount = 0;
        private bool showReport = false;

        public static void Open()
        {
            var window = GetWindow<QuickSetupWindow>("DevKit Quick Setup");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshList();
        }

        private void RefreshList()
        {
            toolList = ToolListManager.LoadList();
            if (selectedStates.Count != toolList.Entries.Count)
            {
                selectedStates = new List<bool>(new bool[toolList.Entries.Count]);
            }
            Repaint();
        }

        private void OnGUI()
        {
            if (isInstalling)
            {
                DrawProgress();
                return;
            }

            if (showReport)
            {
                DrawReport();
                return;
            }

            DrawToolbar();
            DrawAssetList();
            DrawBottomButtons();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Выбрать всё", EditorStyles.toolbarButton))
            {
                for (int i = 0; i < selectedStates.Count; i++) selectedStates[i] = true;
            }
            if (GUILayout.Button("Снять всё", EditorStyles.toolbarButton))
            {
                for (int i = 0; i < selectedStates.Count; i++) selectedStates[i] = false;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Управление списком", EditorStyles.toolbarButton))
            {
                ManageListWindow.Open();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetList()
        {
            if (toolList == null || toolList.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No tools added. Please use 'Manage List' to add some tools.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < toolList.Entries.Count; i++)
            {
                var entry = toolList.Entries[i];
                EditorGUILayout.BeginHorizontal("box");

                selectedStates[i] = EditorGUILayout.Toggle(selectedStates[i], GUILayout.Width(20));

                EditorGUILayout.BeginVertical();
                GUILayout.Label(entry.Name, EditorStyles.boldLabel);
                GUILayout.Label("Type: " + entry.Type, EditorStyles.miniLabel);
                if (!string.IsNullOrEmpty(entry.Description))
                    GUILayout.Label(entry.Description, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBottomButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Управление списком", GUILayout.Height(30)))
            {
                ManageListWindow.Open();
            }
            GUI.enabled = !isInstalling && toolList != null && toolList.Entries.Count > 0 && selectedStates.Any(s => s);
            if (GUILayout.Button("Установить выбранные", GUILayout.Height(30)))
            {
                StartInstallation();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private async void StartInstallation()
        {
            var selectedEntries = new List<ToolEntry>();
            for (int i = 0; i < toolList.Entries.Count; i++)
            {
                if (selectedStates[i])
                    selectedEntries.Add(toolList.Entries[i]);
            }

            if (selectedEntries.Count == 0) return;

            isInstalling = true;
            cancellationTokenSource = new CancellationTokenSource();
            errorMessages.Clear();
            successCount = 0;
            currentProgressMessage = "Starting...";
            currentProgress = 0f;

            try
            {
                await DownloadManager.InstallAssetsAsync(
                    selectedEntries,
                    (msg, prog) =>
                    {
                        currentProgressMessage = msg;
                        currentProgress = prog;
                        Repaint();
                    },
                    (err) =>
                    {
                        errorMessages.Add(err);
                        Repaint();
                    },
                    cancellationTokenSource.Token
                );

                successCount = selectedEntries.Count - errorMessages.Count;
                showReport = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DevKitLoader] Installation failed: {ex.Message}");
                errorMessages.Add($"General error: {ex.Message}");
                showReport = true;
            }
            finally
            {
                isInstalling = false;
                cancellationTokenSource = null;
                Repaint();
            }
        }

        private void DrawProgress()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Installing selected tools...", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.LabelField(currentProgressMessage);
            EditorGUILayout.Slider(currentProgress, 0f, 1f);
            GUILayout.Space(10);
            if (GUILayout.Button("Cancel"))
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                isInstalling = false;
                showReport = true;
            }
            GUILayout.FlexibleSpace();
        }

        private void DrawReport()
        {
            GUILayout.Label("Installation Report", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Successfully installed: {successCount}");
            if (errorMessages.Count > 0)
            {
                EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);
                foreach (var err in errorMessages)
                {
                    EditorGUILayout.HelpBox(err, MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("All tools installed successfully.", MessageType.Info);
            }
            GUILayout.Space(20);
            if (GUILayout.Button("OK"))
            {
                showReport = false;
                RefreshList();
                Repaint();
            }
        }
    }
}
