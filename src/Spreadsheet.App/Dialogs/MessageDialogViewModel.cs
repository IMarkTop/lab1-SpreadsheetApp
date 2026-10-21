using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spreadsheet.ViewModels.Text;

namespace Spreadsheet.App.Dialogs;

/// <summary>Модель подання для модального вікна повідомлень і підтверджень.</summary>
public sealed partial class MessageDialogViewModel : ObservableObject
{
    private readonly Action<bool> _closeAction;

    public MessageDialogViewModel(string title, string message, bool isConfirm, Action<bool> closeAction)
    {
        Title = title;
        Message = message;
        IsConfirm = isConfirm;
        _closeAction = closeAction;
    }

    public string Title { get; }

    public string Message { get; }

    public bool IsConfirm { get; }

    public string OkText => UiText.OkButton;

    public string YesText => UiText.YesButton;

    public string NoText => UiText.NoButton;

    [RelayCommand]
    private void Accept() => _closeAction(true);

    [RelayCommand]
    private void Reject() => _closeAction(false);
}
