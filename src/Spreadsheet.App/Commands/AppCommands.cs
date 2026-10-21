using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace Spreadsheet.App.Commands;

/// <summary>Загальні команди програми (закриття вікна).</summary>
public static class AppCommands
{
    /// <summary>Команда закриття вікна (вікно передається як параметр).</summary>
    public static ICommand CloseWindowCommand { get; } = new RelayCommand<Window>(window =>
    {
        window?.Close();
    });
}
