using System;
using Avalonia;
using Avalonia.Controls;
using Spreadsheet.ViewModels;

namespace Spreadsheet.App.Behaviors;

/// <summary>
/// Перехоплює подію закриття вікна (хрестик або команда закриття) та асинхронно запитує підтвердження незбережених змін.
/// </summary>
public static class WindowClosingBehavior
{
    private static readonly AttachedProperty<WindowClosingTracker?> TrackerProperty =
        AvaloniaProperty.RegisterAttached<Window, WindowClosingTracker?>(
            "Tracker",
            typeof(WindowClosingBehavior));

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>(
            "IsEnabled",
            typeof(WindowClosingBehavior));

    static WindowClosingBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<Window>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(Window element) => element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Window element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(Window window, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is true)
        {
            WindowClosingTracker? tracker = window.GetValue(TrackerProperty);
            if (tracker is null)
            {
                tracker = new WindowClosingTracker(window);
                window.SetValue(TrackerProperty, tracker);
            }
        }
        else
        {
            WindowClosingTracker? tracker = window.GetValue(TrackerProperty);
            if (tracker is not null)
            {
                tracker.Dispose();
                window.SetValue(TrackerProperty, null);
            }
        }
    }

    private sealed class WindowClosingTracker : IDisposable
    {
        private readonly Window _window;
        private bool _isClosingAllowed;
        private bool _isChecking;

        public WindowClosingTracker(Window window)
        {
            _window = window;
            _window.Closing += OnWindowClosing;
        }

        public void Dispose()
        {
            _window.Closing -= OnWindowClosing;
        }

        private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            if (_isClosingAllowed)
            {
                return;
            }

            // Якщо DataContext не є MainViewModel, не блокуємо закриття
            if (_window.DataContext is not MainViewModel vm)
            {
                return;
            }

            e.Cancel = true;

            // Захист від повторного входу під час відображення модального діалогу
            if (_isChecking)
            {
                return;
            }

            _isChecking = true;
            try
            {
                bool canClose = await vm.TryCloseAsync();
                if (canClose)
                {
                    _isClosingAllowed = true;
                    _window.Close();
                }
            }
            catch
            {
                // При помилці залишаємо вікно відкритим
            }
            finally
            {
                _isChecking = false;
            }
        }
    }
}
