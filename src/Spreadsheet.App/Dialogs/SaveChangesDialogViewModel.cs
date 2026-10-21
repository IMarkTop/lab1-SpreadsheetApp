using System;
using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spreadsheet.ViewModels.Services;
using Spreadsheet.ViewModels.Text;

namespace Spreadsheet.App.Dialogs;

/// <summary>
/// Модель діалогу збереження змін із трьома варіантами дій.
/// </summary>
public sealed partial class SaveChangesDialogViewModel : ObservableObject
{
    public string Title => UiText.SaveChangesTitle;

    public string Message { get; }

    public string SaveText => UiText.SaveButton;

    public string DiscardText => UiText.DiscardButton;

    public string CancelText => UiText.CancelButton;

    public ICommand SaveCommand { get; }

    public ICommand DiscardCommand { get; }

    public ICommand CancelCommand { get; }

    public SaveChangesDialogViewModel(string documentName, Action<SaveChangesChoice> closeAction)
    {
        ArgumentNullException.ThrowIfNull(closeAction);
        Message = string.Format(CultureInfo.InvariantCulture, UiText.ConfirmSaveChanges, documentName);

        SaveCommand = new RelayCommand(() => closeAction(SaveChangesChoice.Save));
        DiscardCommand = new RelayCommand(() => closeAction(SaveChangesChoice.Discard));
        CancelCommand = new RelayCommand(() => closeAction(SaveChangesChoice.Cancel));
    }
}
