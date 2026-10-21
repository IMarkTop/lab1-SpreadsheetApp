using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Spreadsheet.Storage;

namespace Spreadsheet.ViewModels.Drive;

/// <summary>
/// Елемент списку файлів Google Drive.
/// </summary>
public sealed class DriveFileItemViewModel : ObservableObject
{
    public string Id { get; }

    public string Name { get; }

    public DateTimeOffset? ModifiedTime { get; }

    public string ModifiedTimeText { get; }

    public DriveFileItemViewModel(DriveFileInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        Id = info.Id;
        Name = info.Name;
        ModifiedTime = info.ModifiedTime;
        ModifiedTimeText = info.ModifiedTime.HasValue
            ? info.ModifiedTime.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
            : "—";
    }

    public DriveFileItemViewModel(string id, string name, DateTimeOffset? modifiedTime)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ModifiedTime = modifiedTime;
        ModifiedTimeText = modifiedTime.HasValue
            ? modifiedTime.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)
            : "—";
    }
}
