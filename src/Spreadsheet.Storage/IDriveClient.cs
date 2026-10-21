using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Spreadsheet.Storage;

/// <summary>
/// Внутрішній інтерфейс клієнта Google Drive для ізоляції мережевих запитів під час тестування.
/// </summary>
internal interface IDriveClient
{
    /// <summary>Отримує список JSON-файлів таблиць, доступних програмі на Google Drive.</summary>
    Task<IReadOnlyList<DriveFileInfo>> ListJsonFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>Завантажує вміст JSON-файлу за вказаним ідентифікатором.</summary>
    Task<string> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>Створює новий JSON-файл у корені Google Drive і повертає його новий ідентифікатор.</summary>
    Task<string> CreateFileAsync(string name, string jsonContent, CancellationToken cancellationToken = default);

    /// <summary>Оновлює вміст наявного JSON-файлу за його ідентифікатором.</summary>
    Task UpdateFileAsync(string fileId, string jsonContent, CancellationToken cancellationToken = default);
}
