using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Spreadsheet.Core.Model;

namespace Spreadsheet.Storage;

/// <summary>
/// Збереження та завантаження таблиці у файловій системі через атомарний запис.
/// </summary>
public sealed class LocalFileStorage : IStorage
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <inheritdoc />
    public async Task<string> SaveAsync(Worksheet sheet, string location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new StorageException("Шлях до файлу не вказано");
        }

        string fullPath;
        string? directory;
        try
        {
            fullPath = Path.GetFullPath(location);
            directory = Path.GetDirectoryName(fullPath);
        }
        catch (Exception ex)
        {
            throw new StorageException($"Некоректний шлях до файлу '{location}': {ex.Message}", ex);
        }

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            throw new StorageException($"Папка '{directory}' не існує");
        }

        string tempPath = Path.Combine(directory, $"{Path.GetFileName(fullPath)}.tmp.{Guid.NewGuid():N}");
        string json = SpreadsheetSerializer.ToJson(sheet);

        try
        {
            await File.WriteAllTextAsync(tempPath, json, Utf8NoBom, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, fullPath, overwrite: true);
            return location;
        }
        catch (OperationCanceledException)
        {
            TryDeleteFile(tempPath);
            throw;
        }
        catch (Exception ex)
        {
            TryDeleteFile(tempPath);
            throw new StorageException($"Не вдалося зберегти файл '{location}': {ex.Message}", ex);
        }
        finally
        {
            TryDeleteFile(tempPath);
        }
    }

    /// <inheritdoc />
    public async Task<Worksheet> LoadAsync(string location, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new StorageException("Шлях до файлу не вказано");
        }

        if (!File.Exists(location))
        {
            throw new StorageException($"Файл '{location}' не знайдено");
        }

        string json;
        try
        {
            json = await File.ReadAllTextAsync(location, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new StorageException($"Не вдалося прочитати файл '{location}': {ex.Message}", ex);
        }

        return SpreadsheetSerializer.FromJson(json);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ігноруємо помилки очищення тимчасового файлу
        }
    }
}
