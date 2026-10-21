using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Spreadsheet.App.Views;
using Spreadsheet.Storage;
using Spreadsheet.ViewModels;
using Spreadsheet.ViewModels.Drive;
using Spreadsheet.ViewModels.Services;
using Spreadsheet.ViewModels.Text;

namespace Spreadsheet.App.Dialogs;

/// <summary>Реалізація IDialogService для Avalonia через модальні діалогові вікна та системні діалоги файлів.</summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private readonly Func<Window?> _ownerProvider;
    private readonly IDriveFileSource _driveFileSource;

    public AvaloniaDialogService(Func<Window?> ownerProvider, IDriveFileSource driveFileSource)
    {
        _ownerProvider = ownerProvider ?? throw new ArgumentNullException(nameof(ownerProvider));
        _driveFileSource = driveFileSource ?? throw new ArgumentNullException(nameof(driveFileSource));
    }

    public async Task ShowErrorAsync(string message)
    {
        var window = new MessageWindow();
        window.DataContext = new MessageDialogViewModel(
            UiText.ErrorDialogTitle,
            message,
            isConfirm: false,
            _ => window.Close());

        Window? owner = _ownerProvider();
        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }
    }

    public async Task<bool> ConfirmAsync(string message)
    {
        bool confirmed = false;
        var window = new MessageWindow();
        window.DataContext = new MessageDialogViewModel(
            UiText.ConfirmDialogTitle,
            message,
            isConfirm: true,
            res =>
            {
                confirmed = res;
                window.Close();
            });

        Window? owner = _ownerProvider();
        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }

        return confirmed;
    }

    public async Task ShowHelpAsync()
    {
        var window = new HelpWindow
        {
            DataContext = new HelpViewModel(),
        };

        Window? owner = _ownerProvider();
        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }
    }

    public async Task ShowAboutAsync()
    {
        var window = new AboutWindow
        {
            DataContext = new AboutViewModel(),
        };

        Window? owner = _ownerProvider();
        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }
    }

    public async Task<string?> PickOpenFileAsync()
    {
        Window? owner = _ownerProvider();
        if (owner is null)
        {
            return null;
        }

        IStorageProvider storageProvider = owner.StorageProvider;
        if (!storageProvider.CanOpen)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = UiText.OpenDialogTitle,
            AllowMultiple = false,
            FileTypeFilter = [
                new FilePickerFileType(UiText.FileTypeFilterJson)
                {
                    Patterns = ["*.json"]
                }
            ]
        };

        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0)
        {
            return null;
        }

        string? localPath = files[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(localPath))
        {
            await ShowErrorAsync(UiText.ErrorInvalidPath);
            return null;
        }

        return localPath;
    }

    public async Task<string?> PickSaveFileAsync(string suggestedName)
    {
        Window? owner = _ownerProvider();
        if (owner is null)
        {
            return null;
        }

        IStorageProvider storageProvider = owner.StorageProvider;
        if (!storageProvider.CanSave)
        {
            return null;
        }

        var options = new FilePickerSaveOptions
        {
            Title = UiText.SaveDialogTitle,
            SuggestedFileName = suggestedName,
            DefaultExtension = "json",
            ShowOverwritePrompt = true,
            FileTypeChoices = [
                new FilePickerFileType(UiText.FileTypeFilterJson)
                {
                    Patterns = ["*.json"]
                }
            ]
        };

        IStorageFile? file = await storageProvider.SaveFilePickerAsync(options);
        if (file is null)
        {
            return null;
        }

        string? localPath = file.TryGetLocalPath();
        if (string.IsNullOrEmpty(localPath))
        {
            await ShowErrorAsync(UiText.ErrorInvalidPath);
            return null;
        }

        return localPath;
    }

    public async Task<SaveChangesChoice> AskSaveChangesAsync(string documentName)
    {
        SaveChangesChoice result = SaveChangesChoice.Cancel;
        var window = new SaveChangesWindow();
        window.DataContext = new SaveChangesDialogViewModel(documentName, choice =>
        {
            result = choice;
            window.Close();
        });

        Window? owner = _ownerProvider();
        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }

        return result;
    }

    public async Task<StorageLocation?> PickDriveFileAsync()
    {
        StorageLocation? result = null;
        var window = new DriveFilePickerWindow();
        var vm = new DriveFilePickerViewModel(
            ct => _driveFileSource.ListFilesAsync(ct),
            item =>
            {
                result = new StorageLocation($"gdrive:{item.Id}", item.Name);
                window.Close();
            },
            () => window.Close());

        window.DataContext = vm;
        _ = vm.LoadFilesCommand.ExecuteAsync(null);

        Window? owner = _ownerProvider();
        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }

        return result;
    }

    public async Task<string?> AskDriveFileNameAsync(string suggestedName)
    {
        string? result = null;
        var window = new DriveSaveFileNameWindow();
        var vm = new DriveSaveFileNameViewModel(
            suggestedName,
            name =>
            {
                result = name;
                window.Close();
            },
            () => window.Close());

        window.DataContext = vm;

        Window? owner = _ownerProvider();
        if (owner is not null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }

        return result;
    }
}
