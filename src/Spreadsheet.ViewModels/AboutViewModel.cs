using Spreadsheet.ViewModels.Text;

namespace Spreadsheet.ViewModels;

/// <summary>Дані вікна «Про програму».</summary>
public sealed class AboutViewModel
{
    /// <summary>Заголовок вікна.</summary>
    public string Title => UiText.AboutTitle;

    /// <summary>Назва програми.</summary>
    public string AppName => UiText.AppTitle;

    /// <summary>Версія збірки.</summary>
    public string Version { get; } =
        typeof(AboutViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    /// <summary>Версія з підписом, наприклад «Версія 1.0.0».</summary>
    public string VersionText => $"{UiText.AboutVersionLabel} {Version}";

    /// <summary>Назва роботи та варіант.</summary>
    public string Course => UiText.AboutCourse;

    /// <summary>Автор і група (плейсхолдери).</summary>
    public string Author => UiText.AboutAuthor;

    /// <summary>Короткий опис програми.</summary>
    public string Description => UiText.AboutDescription;
}
