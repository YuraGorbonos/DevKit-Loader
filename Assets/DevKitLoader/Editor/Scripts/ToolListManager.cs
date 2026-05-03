using UnityEditor;
using UnityEngine;
using System.Linq;

namespace DevKitLoader
{
    public static class ToolListManager
    {
        private const string PrefKey = "DevKitLoader_ToolList";

        /// <summary> Загружает список из EditorPrefs. Если нет – возвращает пустой список. </summary>
        public static ToolList LoadList()
        {
            if (EditorPrefs.HasKey(PrefKey))
            {
                string json = EditorPrefs.GetString(PrefKey);
                ToolList list = JsonUtility.FromJson<ToolList>(json);
                if (list != null && list.Entries != null)
                    return list;
            }
            return new ToolList();
        }

        /// <summary> Сохраняет список в EditorPrefs. </summary>
        public static void SaveList(ToolList list)
        {
            string json = JsonUtility.ToJson(list);
            EditorPrefs.SetString(PrefKey, json);
        }

        /// <summary> Добавляет новый ToolEntry в конец списка и сохраняет. </summary>
        public static void AddEntry(ToolEntry entry)
        {
            var list = LoadList();
            list.Entries.Add(entry);
            SaveList(list);
        }

        /// <summary> Удаляет запись по индексу и сохраняет. </summary>
        public static void RemoveEntry(int index)
        {
            var list = LoadList();
            if (index >= 0 && index < list.Entries.Count)
            {
                list.Entries.RemoveAt(index);
                SaveList(list);
            }
        }

        /// <summary> Обновляет запись по индексу и сохраняет. </summary>
        public static void UpdateEntry(int index, ToolEntry newEntry)
        {
            var list = LoadList();
            if (index >= 0 && index < list.Entries.Count)
            {
                // Copy data to avoid shared reference mutations
                list.Entries[index] = new ToolEntry
                {
                    Name = newEntry.Name,
                    Description = newEntry.Description,
                    Type = newEntry.Type,
                    Url = newEntry.Url,
                    License = newEntry.License,
                    Tags = newEntry.Tags
                };
                SaveList(list);
            }
        }
    }
}
