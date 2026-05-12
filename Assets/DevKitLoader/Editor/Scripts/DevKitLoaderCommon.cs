using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace DevKitLoader
{
    /// <summary>
    /// Общий набор утилит и констант для DRY-модерации кода редакторской части
    /// </summary>
    public static class DevKitLoaderCommon
    {
        /// <summary>
        /// User-Agent заголовок для запросов к API
        /// </summary>
        public const string UserAgent = "DevKitLoader";

        /// <summary>
        /// Базовый URL GitHub API
        /// </summary>
        public const string GitHubApiBase = "https://api.github.com/repos/";

        /// <summary>
        /// Базовый URL GitLab API
        /// </summary>
        public const string GitLabApiBase = "https://gitlab.com/api/v4/projects/";

        /// <summary>
        /// Очищает имя папки от недопустимых символов
        /// </summary>
        /// <param name="name">Имя для очистки</param>
        /// <returns>Очищенное имя папки</returns>
        public static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Unknown";
            }

            // Используем StringBuilder для эффективной работы со строками
            var sb = new System.Text.StringBuilder(name);

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                sb.Replace(c, '_');
            }

            foreach (char c in Path.GetInvalidPathChars())
            {
                sb.Replace(c, '_');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Очищает значение заголовка от недопустимых символов
        /// </summary>
        /// <param name="value">Значение для очистки</param>
        /// <returns>Очищенное значение заголовка</returns>
        public static string SanitizeHeaderValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            // Ограничиваем длину строки для безопасности
            if (value.Length > 1000)
            {
                value = value.Substring(0, 1000);
            }

            // Разрешённые символы: буквы, цифры, пробелы, дефис, подчёркивание, двоеточие, точка, звёздочка
            // Используем StringBuilder для эффективной работы со строками
            var sb = new System.Text.StringBuilder(value);

            // Удаляем недопустимые символы
            for (int i = sb.Length - 1; i >= 0; i--)
            {
                char c = sb[i];

                if (!IsAllowedChar(c))
                {
                    sb.Remove(i, 1);
                }
            }

            // Дополнительная проверка на пустую строку после очистки
            string result = sb.ToString();

            if (string.IsNullOrEmpty(result))
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Получает путь к целевой папке для инструмента
        /// </summary>
        /// <param name="name">Имя инструмента</param>
        /// <returns>Путь к целевой папке</returns>
        public static string GetTargetFolderForName(string name)
        {
            return $"Assets/DevKitInstalled/{SanitizeFolderName(name)}";
        }

        /// <summary>
        /// Скачивает файл по URL
        /// </summary>
        /// <param name="url">URL файла для скачивания</param>
        /// <param name="destPath">Путь назначения</param>
        /// <param name="onProgress">Callback для отслеживания прогресса</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Задача для ожидания завершения скачивания</returns>
        public static async Task DownloadFileAsync(string url, string destPath, Action<string, float> onProgress, CancellationToken cancellationToken)
        {
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler = new DownloadHandlerFile(destPath);
                var asyncOp = request.SendWebRequest();

                while (!asyncOp.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        throw new OperationCanceledException();
                    }

                    onProgress?.Invoke("Скачивание...", 0.4f + asyncOp.progress * 0.4f);
                    await Task.Delay(50, cancellationToken);
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Ошибка загрузки: {request.error}");
                }
            }
        }

        /// <summary>
        /// Проверяет, является ли символ разрешённым для заголовков
        /// </summary>
        /// <param name="c">Символ для проверки</param>
        /// <returns>True если символ разрешён</returns>
        private static bool IsAllowedChar(char c)
        {
            // Разрешённые символы: буквы, цифры, пробелы, дефис, подчёркивание, двоеточие, точка, звёздочка
            return char.IsLetterOrDigit(c) ||
                   c == ' ' ||
                   c == '-' ||
                   c == '_' ||
                   c == ':' ||
                   c == '.' ||
                   c == '*';
        }
    }
}