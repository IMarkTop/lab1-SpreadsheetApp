using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Spreadsheet.Core.Model;

namespace Spreadsheet.Storage;

/// <summary>
/// Сховище для збереження та завантаження таблиць на Google Drive.
/// </summary>
public sealed class GoogleDriveStorage : IStorage, IDriveFileSource, IDisposable
{
    public const string DrivePrefix = "gdrive:";
    public const string NewFilePrefix = "gdrive:new:";

    private readonly IDriveClient _driveClient;
    private readonly bool _ownsClient;

    /// <summary>Створює екземпляр сховища з реальним Google Drive клієнтом.</summary>
    public GoogleDriveStorage() : this(new GoogleDriveClient(), ownsClient: true)
    {
    }

    /// <summary>Створює екземпляр сховища з підставним клієнтом для тестування.</summary>
    internal GoogleDriveStorage(IDriveClient driveClient) : this(driveClient, ownsClient: false)
    {
    }

    private GoogleDriveStorage(IDriveClient driveClient, bool ownsClient)
    {
        _driveClient = driveClient ?? throw new ArgumentNullException(nameof(driveClient));
        _ownsClient = ownsClient;
    }

    public void Dispose()
    {
        if (_ownsClient && _driveClient is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<string> SaveAsync(Worksheet sheet, string location, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new StorageException(DriveErrorMessages.InvalidDriveLocation);
        }

        // Серіалізуємо до будь-яких асинхронних очікувань (await)
        string json = SpreadsheetSerializer.ToJson(sheet);

        if (location.StartsWith(NewFilePrefix, StringComparison.OrdinalIgnoreCase))
        {
            string fileName = location[NewFilePrefix.Length..].Trim();
            ValidateFileName(fileName);

            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".json";
            }

            string newId = await _driveClient.CreateFileAsync(fileName, json, cancellationToken).ConfigureAwait(false);
            return $"{DrivePrefix}{newId}";
        }

        if (location.StartsWith(DrivePrefix, StringComparison.OrdinalIgnoreCase))
        {
            string fileId = location[DrivePrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(fileId) || fileId.StartsWith("new:", StringComparison.OrdinalIgnoreCase))
            {
                throw new StorageException(DriveErrorMessages.InvalidDriveLocation);
            }

            await _driveClient.UpdateFileAsync(fileId, json, cancellationToken).ConfigureAwait(false);
            return location;
        }

        throw new StorageException(DriveErrorMessages.InvalidDriveLocation);
    }

    /// <inheritdoc />
    public async Task<Worksheet> LoadAsync(string location, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location) ||
            !location.StartsWith(DrivePrefix, StringComparison.OrdinalIgnoreCase) ||
            location.StartsWith(NewFilePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new StorageException(DriveErrorMessages.InvalidDriveLocation);
        }

        string fileId = location[DrivePrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(fileId))
        {
            throw new StorageException(DriveErrorMessages.InvalidDriveLocation);
        }

        string json = await _driveClient.DownloadFileAsync(fileId, cancellationToken).ConfigureAwait(false);
        return SpreadsheetSerializer.FromJson(json);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DriveFileInfo>> ListFilesAsync(CancellationToken cancellationToken = default)
    {
        return _driveClient.ListJsonFilesAsync(cancellationToken);
    }

    private static void ValidateFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new StorageException(DriveErrorMessages.InvalidFileName);
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0 || name.Contains('/') || name.Contains('\\') || name.Contains(':'))
        {
            throw new StorageException(DriveErrorMessages.InvalidFileName);
        }
    }
}
