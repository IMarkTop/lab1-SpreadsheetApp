using System.Globalization;
using Spreadsheet.Core.Errors;

namespace Spreadsheet.ViewModels.Text;

/// <summary>Форматує повідомлення про помилку виразу для інтерфейсу.</summary>
public static class ErrorTextFormatter
{
    /// <summary>
    /// <c>Помилка (позиція N, довжина M): повідомлення.</c> і, якщо діапазон лежить у тексті,
    /// <c>Фрагмент: «…»</c>. Позиція показується з 1 (у Core вона з 0).
    /// </summary>
    public static string Format(ExpressionError error, string rawText)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(rawText);

        string text = string.Format(
            CultureInfo.InvariantCulture, UiText.ErrorTemplate, error.Position + 1, error.Length, error.Message);

        bool hasFragment = error.Length > 0
            && error.Position >= 0
            && error.Position + error.Length <= rawText.Length;
        if (!hasFragment)
        {
            return text;
        }

        string fragment = rawText.Substring(error.Position, error.Length);
        string separator = text.EndsWith('.') ? " " : ". ";
        return text + separator + string.Format(CultureInfo.InvariantCulture, UiText.FragmentTemplate, fragment);
    }
}
