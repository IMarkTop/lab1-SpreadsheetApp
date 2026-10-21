using System;
using System.Threading;
using System.Threading.Tasks;
using Spreadsheet.Core.Model;

namespace Spreadsheet.Storage;

/// <summary>
/// Композитне сховище: маршрутизує операції на GoogleDriveStorage для префіксу "gdrive:", або на LocalFileStorage.
/// </summary>
public sealed class CompositeStorage : IStorage, IDisposable
{
    private readonly IStorage _localStorage;
    private readonly IStorage _driveStorage;

    /// <summary>
    /// Створює композитне сховище із заданими сховищами.
    /// </summary>
    public CompositeStorage(IStorage localStorage, IStorage driveStorage)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _driveStorage = driveStorage ?? throw new ArgumentNullException(nameof(driveStorage));
    }

    /// <inheritdoc />
    public Task<string> SaveAsync(Worksheet sheet, string location, CancellationToken cancellationToken = default)
    {
        return SelectStorage(location).SaveAsync(sheet, location, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Worksheet> LoadAsync(string location, CancellationToken cancellationToken = default)
    {
        return SelectStorage(location).LoadAsync(location, cancellationToken);
    }

    public void Dispose()
    {
        if (_driveStorage is IDisposable driveDisposable)
        {
            driveDisposable.Dispose();
        }

        if (_localStorage is IDisposable localDisposable)
        {
            localDisposable.Dispose();
        }
    }

    private IStorage SelectStorage(string? location)
    {
        if (location is not null &&
            location.StartsWith(GoogleDriveStorage.DrivePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return _driveStorage;
        }

        return _localStorage;
    }
}
