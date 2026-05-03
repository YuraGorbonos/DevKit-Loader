using UnityEditor;
using UnityEngine;

namespace DevKitLoader
{
    public class ManageListWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private int editingIndex = -1;

        // Поля формы
        private string nameField = "";
        private string descriptionField = "";
        private SourceType typeField = SourceType.GitHubRelease;
        private string urlField = "";
        private string licenseField = "";
        private string tagsField = "";

        private ToolList cachedList;

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
            cachedList = ToolListManager.LoadList();
        }

        private void SaveAndReload()
        {
            // Сохранение происходит внутри методов ToolListManager, нам нужно перезагрузить кэш
            cachedList = ToolListManager.LoadList();
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
            GUILayout.Label(editingIndex == -1 ? "Add New Tool" : "Edit Tool", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");
            nameField = EditorGUILayout.TextField("Name*", nameField);
            typeField = (SourceType)EditorGUILayout.EnumPopup("Source Type", typeField);
            urlField = EditorGUILayout.TextField("URL*", urlField);
            descriptionField = EditorGUILayout.TextField("Description", descriptionField);
            licenseField = EditorGUILayout.TextField("License (optional)", licenseField);
            tagsField = EditorGUILayout.TextField("Tags (optional)", tagsField);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(editingIndex == -1 ? "Add" : "Update"))
            {
                if (ValidateForm())
                {
                    var entry = new ToolEntry
                                {
                                    Name = nameField.Trim(),
                                    Description = descriptionField,
                                    Type = typeField,
                                    Url = urlField.Trim(),
                                    License = licenseField,
                                    Tags = tagsField
                                };

                    if (editingIndex == -1)
                    {
                        ToolListManager.AddEntry(entry);
                    }
                    else
                    {
                        ToolListManager.UpdateEntry(editingIndex, entry);
                    }

                    ClearForm();
                    SaveAndReload();
                }
            }

            if (editingIndex != -1 && GUILayout.Button("Cancel"))
            {
                ClearForm();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(nameField))
            {
                EditorUtility.DisplayDialog("Validation Error", "Tool Name is required.", "OK");
                return false;
            }

            if (string.IsNullOrWhiteSpace(urlField))
            {
                EditorUtility.DisplayDialog("Validation Error", "URL is required.", "OK");
                return false;
            }

            return true;
        }

        private void ClearForm()
        {
            editingIndex = -1;
            nameField = "";
            descriptionField = "";
            typeField = SourceType.GitHubRelease;
            urlField = "";
            licenseField = "";
            tagsField = "";
            GUI.FocusControl(null);
        }

        private void DrawList()
        {
            GUILayout.Label("Installed Tools List", EditorStyles.boldLabel);

            if (cachedList == null || cachedList.Entries.Count == 0)
            {
                GUILayout.Label("No tools added yet. Use the form above to add your first tool.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            for (int i = 0; i < cachedList.Entries.Count; i++)
            {
                var entry = cachedList.Entries[i];
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

                if (GUILayout.Button("Edit"))
                {
                    editingIndex = i;
                    nameField = entry.Name;
                    descriptionField = entry.Description;
                    typeField = entry.Type;
                    urlField = entry.Url;
                    licenseField = entry.License;
                    tagsField = entry.Tags;
                }

                if (GUILayout.Button("Delete"))
                {
                    if (EditorUtility.DisplayDialog("Confirm Delete", $"Delete tool '{entry.Name}'?", "Delete", "Cancel"))
                    {
                        ToolListManager.RemoveEntry(i);
                        SaveAndReload();

                        if (editingIndex == i)
                        {
                            ClearForm();
                        }
                        else if (editingIndex > i)
                        {
                            editingIndex--;
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