using System;
using System.IO;

namespace Spreadsheet.Storage;

/// <summary>
/// Знаходить шлях до файлу client_secret.json, переглядаючи поточний каталог і його батьківські каталоги до кореня.
/// </summary>
internal static class GoogleClientSecretLocator
{
    public const string SecretFileName = "client_secret.json";

    /// <summary>
    /// Шукає client_secret.json починаючи з указаного каталогу (або AppContext.BaseDirectory) вгору до кореня.
    /// </summary>
    /// <param name="startDirectory">Каталог для початку пошуку (для тестування).</param>
    /// <returns>Абсолютний шлях до знайденого файлу client_secret.json.</returns>
    /// <exception cref="StorageException">Кидається, якщо файл не знайдено.</exception>
    public static string FindSecretPath(string? startDirectory = null)
    {
        string? current = startDirectory;
        if (string.IsNullOrWhiteSpace(current))
        {
            current = AppContext.BaseDirectory;
        }

        while (!string.IsNullOrEmpty(current))
        {
            string candidate = Path.Combine(current, SecretFileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            try
            {
                string[] matches = Directory.GetFiles(current, "client_secret*.json");
                if (matches.Length > 0)
                {
                    return matches[0];
                }
            }
            catch
            {
                // Ігноруємо помилки доступу до окремих папок
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new StorageException(DriveErrorMessages.SecretNotFound);
    }
}
