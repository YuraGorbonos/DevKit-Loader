using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace DevKitLoader
{
    public class ManageListWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _editingIndex = -1;

        // Поля формы
        private string _nameField = "";
        private string _descriptionField = "";
        private SourceType _typeField = SourceType.GitHubRelease;
        private string _urlField = "";
        private string _licenseField = "";
        private string _tagsField = "";

        private ToolList _cachedList;

        private void OnEnable()
        {
            LoadList();
        }

        public static void Open()
        {
            var window = GetWindow<ManageListWindow>("DevKit Loader - Manage List");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void LoadList()
        {
            _cachedList = ToolListManager.LoadList();
        }

        private void SaveAndReload()
        {
            // Сохранение происходит внутри методов ToolListManager, нам нужно перезагрузить кэш
            _cachedList = ToolListManager.LoadList();
            Repaint();
        }

        private void OnGUI()
        {
            DrawForm();
            GUILayout.Space(10);
            DrawList();
        }

        private void DrawForm()
        {
            GUILayout.Label(_editingIndex == -1 ? "Add New Tool" : "Edit Tool", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            _nameField = EditorGUILayout.TextField("Name*", _nameField);
            _typeField = (SourceType)EditorGUILayout.EnumPopup("Source Type", _typeField);
            _urlField = EditorGUILayout.TextField("URL*", _urlField);
            _descriptionField = EditorGUILayout.TextField("Description", _descriptionField);
            _licenseField = EditorGUILayout.TextField("License (optional)", _licenseField);
            _tagsField = EditorGUILayout.TextField("Tags (optional)", _tagsField);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(_editingIndex == -1 ? "Add" : "Update"))
            {
                if (ValidateForm())
                {
                    var entry = new ToolEntry
                                {
                                    Name = _nameField.Trim(),
                                    Description = _descriptionField,
                                    Type = _typeField,
                                    Url = _urlField.Trim(),
                                    License = _licenseField,
                                    Tags = _tagsField
                                };

                    if (_editingIndex == -1)
                    {
                        ToolListManager.AddEntry(entry);
                    }
                    else
                    {
                        ToolListManager.UpdateEntry(_editingIndex, entry);
                    }

                    ClearForm();
                    SaveAndReload();
                }
            }

            if (_editingIndex != -1 && GUILayout.Button("Cancel"))
            {
                ClearForm();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(_nameField))
            {
                EditorUtility.DisplayDialog("Validation Error", "Tool Name is required.", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_urlField))
            {
                EditorUtility.DisplayDialog("Validation Error", "URL is required.", "OK");
                return false;
            }

            return true;
        }

        private void ClearForm()
        {
            _editingIndex = -1;
            _nameField = "";
            _descriptionField = "";
            _typeField = SourceType.GitHubRelease;
            _urlField = "";
            _licenseField = "";
            _tagsField = "";
            GUI.FocusControl(null);
        }

        private async void TestInstall(ToolEntry entry)
        {
            var progress = new Progress<(string, float)>(update => { Debug.Log($"[Progress] {update.Item1}: {update.Item2 * 100:F0}%"); });
            var cts = new CancellationTokenSource();

            DownloadManager.InstallAssetsAsync(
                new List<ToolEntry> { entry },
                (msg, prog) => Debug.Log($"{msg} ({prog * 100:F0}%)"),
                err => Debug.LogError(err),
                cts.Token
            );
        }

        private void DrawList()
        {
            GUILayout.Label("Installed Tools List", EditorStyles.boldLabel);

            if (_cachedList == null || _cachedList.Entries.Count == 0)
            {
                GUILayout.Label("No tools added yet. Use the form above to add your first tool.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            for (int i = 0; i < _cachedList.Entries.Count; i++)
            {
                var entry = _cachedList.Entries[i];
                EditorGUILayout.BeginHorizontal("box");

                // Отображение информации
                EditorGUILayout.BeginVertical();
                GUILayout.Label($"[{entry.Type}] {entry.Name}", EditorStyles.boldLabel);

                if (!string.IsNullOrEmpty(entry.Description))
                {
                    GUILayout.Label(entry.Description, EditorStyles.miniLabel);
                }

                GUILayout.Label($"URL: {entry.Url}", EditorStyles.miniLabel);

                if (!string.IsNullOrEmpty(entry.License))
                {
                    GUILayout.Label($"License: {entry.License}", EditorStyles.miniLabel);
                }

                if (!string.IsNullOrEmpty(entry.Tags))
                {
                    GUILayout.Label($"Tags: {entry.Tags}", EditorStyles.miniLabel);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.Width(80));

                if (GUILayout.Button("Test Install"))
                {
                    TestInstall(entry);
                }

                if (GUILayout.Button("Edit"))
                {
                    _editingIndex = i;
                    _nameField = entry.Name;
                    _descriptionField = entry.Description;
                    _typeField = entry.Type;
                    _urlField = entry.Url;
                    _licenseField = entry.License;
                    _tagsField = entry.Tags;
                }

                if (GUILayout.Button("Delete"))
                {
                    if (EditorUtility.DisplayDialog("Confirm Delete", $"Delete tool '{entry.Name}'?", "Delete", "Cancel"))
                    {
                        ToolListManager.RemoveEntry(i);
                        SaveAndReload();

                        if (_editingIndex == i)
                        {
                            ClearForm();
                        }
                        else if (_editingIndex > i)
                        {
                            _editingIndex--;
                        }
                    }
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
