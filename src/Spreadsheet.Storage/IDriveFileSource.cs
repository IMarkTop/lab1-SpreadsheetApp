using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Spreadsheet.Storage;

/// <summary>
/// Джерело файлів на Google Drive для діалогу вибору файлу.
/// </summary>
public interface IDriveFileSource
{
    /// <summary>
    /// Отримує список файлів таблиць (.json), доступних застосунку на Google Drive.
    /// </summary>
    Task<IReadOnlyList<DriveFileInfo>> ListFilesAsync(CancellationToken cancellationToken = default);
}
