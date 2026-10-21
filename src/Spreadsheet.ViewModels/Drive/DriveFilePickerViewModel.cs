using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spreadsheet.Storage;
using Spreadsheet.ViewModels.Text;

namespace Spreadsheet.ViewModels.Drive;

/// <summary>
/// Модель подання діалогу вибору файлу з Google Drive.
/// </summary>
public sealed partial class DriveFilePickerViewModel : ObservableObject
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<DriveFileInfo>>> _fileLoader;
    private readonly Action<DriveFileItemViewModel> _onConfirm;
    private readonly Action _onCancel;

    public string Title => UiText.DrivePickerTitle;

    public string OpenButtonText => UiText.DriveOpenButton;

    public string CancelButtonText => UiText.DriveCancelButton;

    public string RetryButtonText => UiText.DriveRetryButton;

    public string LoadingText => UiText.DriveLoading;

    public string EmptyText => UiText.DriveNoFiles;

    public ObservableCollection<DriveFileItemViewModel> Files { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    private DriveFileItemViewModel? _selectedFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private bool _hasError;

    [ObservableProperty]
    private string? _errorMessage;

    public bool IsEmpty => !IsLoading && !HasError && Files.Count == 0;

    public DriveFilePickerViewModel(
        Func<CancellationToken, Task<IReadOnlyList<DriveFileInfo>>> fileLoader,
        Action<DriveFileItemViewModel> onConfirm,
        Action onCancel)
    {
        _fileLoader = fileLoader ?? throw new ArgumentNullException(nameof(fileLoader));
        _onConfirm = onConfirm ?? throw new ArgumentNullException(nameof(onConfirm));
        _onCancel = onCancel ?? throw new ArgumentNullException(nameof(onCancel));
    }

    [RelayCommand]
    public async Task LoadFilesAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = null;
        Files.Clear();

        try
        {
            IReadOnlyList<DriveFileInfo> result = await _fileLoader(cancellationToken).ConfigureAwait(false);
            foreach (DriveFileInfo info in result)
            {
                Files.Add(new DriveFileItemViewModel(info));
            }
        }
        catch (OperationCanceledException)
        {
            // Скасування завантаження користувачем
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(IsEmpty));
        }
    }

    private bool CanOpen() => SelectedFile is not null;

    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        if (SelectedFile is not null)
        {
            _onConfirm(SelectedFile);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _onCancel();
    }

    [RelayCommand]
    private Task RetryAsync() => LoadFilesAsync();
}
