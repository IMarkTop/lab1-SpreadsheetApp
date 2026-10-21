using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spreadsheet.ViewModels.Text;

namespace Spreadsheet.ViewModels.Drive;

/// <summary>
/// Модель подання діалогу введення назви файлу для збереження на Google Drive.
/// </summary>
public sealed partial class DriveSaveFileNameViewModel : ObservableObject
{
    private readonly Action<string> _onConfirm;
    private readonly Action _onCancel;

    public string Title => UiText.DriveSaveTitle;

    public string Prompt => UiText.DriveSavePrompt;

    public string SaveButtonText => UiText.DriveSaveButton;

    public string CancelButtonText => UiText.DriveCancelButton;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public DriveSaveFileNameViewModel(
        string suggestedName,
        Action<string> onConfirm,
        Action onCancel)
    {
        _onConfirm = onConfirm ?? throw new ArgumentNullException(nameof(onConfirm));
        _onCancel = onCancel ?? throw new ArgumentNullException(nameof(onCancel));
        _fileName = string.IsNullOrWhiteSpace(suggestedName) || suggestedName == UiText.DefaultDocumentName
            ? "Table.json"
            : suggestedName;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(FileName);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        string name = FileName.Trim();
        char[] invalidChars = Path.GetInvalidFileNameChars();
        if (name.IndexOfAny(invalidChars) >= 0 || name.Contains('/') || name.Contains('\\') || name.Contains(':'))
        {
            ErrorMessage = "Назва файлу містить неприпустимі символи";
            return;
        }

        if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            name += ".json";
        }

        _onConfirm(name);
    }

    [RelayCommand]
    private void Cancel()
    {
        _onCancel();
    }
}
