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
        private Vector2 _scrollPosition;
        private List<bool> _selectedStates = new();
        private ToolList _toolList;
        private bool _isInstalling;
        private CancellationTokenSource _cancellationTokenSource;
        private string _currentProgressMessage = string.Empty;
        private float _currentProgress;
        private List<string> _errorMessages = new();
        private int _successCount;
        private bool _showReport;

        private void OnEnable()
        {
            RefreshList();
        }

        public static void Open()
        {
            var window = GetWindow<QuickSetupWindow>("DevKit Quick Setup");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void RefreshList()
        {
            _toolList = ToolListManager.LoadList();

            if (_selectedStates.Count != _toolList.Entries.Count)
            {
                _selectedStates = new List<bool>(new bool[_toolList.Entries.Count]);
            }

            Repaint();
        }

        private void OnGUI()
        {
            if (_isInstalling)
            {
                DrawProgress();
                return;
            }

            if (_showReport)
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
                for (int i = 0; i < _selectedStates.Count; i++)
                {
                    _selectedStates[i] = true;
                }
            }

            if (GUILayout.Button("Снять всё", EditorStyles.toolbarButton))
            {
                for (int i = 0; i < _selectedStates.Count; i++)
                {
                    _selectedStates[i] = false;
                }
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
            if (_toolList == null || _toolList.Entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No tools added. Please use 'Manage List' to add some tools.", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < _toolList.Entries.Count; i++)
            {
                var entry = _toolList.Entries[i];
                EditorGUILayout.BeginHorizontal("box");

                _selectedStates[i] = EditorGUILayout.Toggle(_selectedStates[i], GUILayout.Width(20));

                EditorGUILayout.BeginVertical();
                GUILayout.Label(entry.Name, EditorStyles.boldLabel);
                GUILayout.Label("Type: " + entry.Type, EditorStyles.miniLabel);

                if (!string.IsNullOrEmpty(entry.Description))
                {
                    GUILayout.Label(entry.Description, EditorStyles.wordWrappedMiniLabel);
                }

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

            GUI.enabled = !_isInstalling && _toolList != null && _toolList.Entries.Count > 0 && _selectedStates.Any(s => s);

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

            for (int i = 0; i < _toolList.Entries.Count; i++)
            {
                if (_selectedStates[i])
                {
                    selectedEntries.Add(_toolList.Entries[i]);
                }
            }

            if (selectedEntries.Count == 0)
            {
                return;
            }

            _isInstalling = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _errorMessages.Clear();
            _successCount = 0;
            _currentProgressMessage = "Starting...";
            _currentProgress = 0f;

            try
            {
                await DownloadManager.InstallAssetsAsync(
                    selectedEntries,
                    (msg, prog) =>
                    {
                        _currentProgressMessage = msg;
                        _currentProgress = prog;
                        Repaint();
                    },
                    err =>
                    {
                        _errorMessages.Add(err);
                        Repaint();
                    },
                    _cancellationTokenSource.Token
                );

                _successCount = selectedEntries.Count - _errorMessages.Count;
                _showReport = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DevKitLoader] Installation failed: {ex.Message}");
                _errorMessages.Add($"General error: {ex.Message}");
                _showReport = true;
            }
            finally
            {
                _isInstalling = false;
                _cancellationTokenSource = null;
                Repaint();
            }
        }

        private void DrawProgress()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Installing selected tools...", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.LabelField(_currentProgressMessage);
            EditorGUILayout.Slider(_currentProgress, 0f, 1f);
            GUILayout.Space(10);

            if (GUILayout.Button("Cancel"))
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _isInstalling = false;
                _showReport = true;
            }

            GUILayout.FlexibleSpace();
        }

        private void DrawReport()
        {
            GUILayout.Label("Installation Report", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.LabelField($"Successfully installed: {_successCount}");

            if (_errorMessages.Count > 0)
            {
                EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);

                foreach (var err in _errorMessages)
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
                _showReport = false;
                RefreshList();
                Repaint();
            }
        }
    }
}