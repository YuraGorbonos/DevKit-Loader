using System.IO;
using SharpCompress.Readers;
using UnityEditor;
using UnityEngine;

namespace DevKitLoader
{
    public static class PackageImporter
    {
        /// <summary>
        /// Extracts a .unitypackage (tar.gz) into the specified project folder.
        /// </summary>
        /// <param name="unityPackagePath">Full path to the .unitypackage file</param>
        /// <param name="targetFolder">Relative folder under Assets (e.g. "Assets/DevKit/MyTool")</param>
        public static void ExtractUnityPackage(string unityPackagePath, string targetFolder)
        {
            EnsureTargetFolder(targetFolder);

            using (var stream = File.OpenRead(unityPackagePath))
            using (var reader = ReaderFactory.OpenReader(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        // Inside unitypackage paths start with "Assets/..."
                        string entryPath = reader.Entry.Key;

                        // Normalize: remove leading "Assets/" and then combine with targetFolder
                        string relativePath = entryPath;

                        if (relativePath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                        {
                            relativePath = relativePath.Substring("Assets/".Length);
                        }

                        string finalPath = Path.Combine(targetFolder, relativePath);
                        string finalDir = Path.GetDirectoryName(finalPath);

                        if (!Directory.Exists(finalDir))
                        {
                            Directory.CreateDirectory(finalDir);
                        }

                        reader.WriteEntryToFile(finalPath);
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[DevKitLoader] Распакован {Path.GetFileName(unityPackagePath)} в {targetFolder}");
        }

        /// <summary>
        /// Extracts a .zip archive into the specified project folder.
        /// </summary>
        public static void ExtractZip(string zipPath, string targetFolder)
        {
            EnsureTargetFolder(targetFolder);

            using (var stream = File.OpenRead(zipPath))
            using (var reader = ReaderFactory.OpenReader(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        string finalPath = Path.Combine(targetFolder, reader.Entry.Key);
                        string finalDir = Path.GetDirectoryName(finalPath);

                        if (!Directory.Exists(finalDir))
                        {
                            Directory.CreateDirectory(finalDir);
                        }

                        reader.WriteEntryToFile(finalPath);
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[DevKitLoader] Распакован {Path.GetFileName(zipPath)} в {targetFolder}");
        }

        private static void EnsureTargetFolder(string targetFolder)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", targetFolder);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        /// <summary>
        /// Альтернативный метод для .unitypackage через встроенный импортер (интерактивный).
        /// Используется, если ручная распаковка не требуется.
        /// </summary>
        public static void ImportUnityPackageInteractive(string filePath)
        {
            AssetDatabase.ImportPackage(filePath, false);
        }
    }
}