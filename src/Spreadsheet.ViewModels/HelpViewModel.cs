using Spreadsheet.ViewModels.Text;

namespace Spreadsheet.ViewModels;

/// <summary>Текст довідки.</summary>
public sealed class HelpViewModel
{
    /// <summary>Заголовок вікна довідки.</summary>
    public string Title => UiText.HelpTitle;

    /// <summary>Повний текст довідки.</summary>
    public string Text => UiText.HelpText;
}
