using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Spreadsheet.App.Dialogs;
using Spreadsheet.App.Views;
using Spreadsheet.Storage;
using Spreadsheet.ViewModels;

namespace Spreadsheet.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            var driveStorage = new GoogleDriveStorage();
            var localStorage = new LocalFileStorage();
            var compositeStorage = new CompositeStorage(localStorage, driveStorage);
            var dialogService = new AvaloniaDialogService(() => desktop.MainWindow ?? mainWindow, driveStorage);
            var mainViewModel = new MainViewModel(dialogService, compositeStorage);

            mainWindow.DataContext = mainViewModel;
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
