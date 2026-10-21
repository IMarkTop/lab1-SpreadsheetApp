namespace Spreadsheet.ViewModels;

/// <summary>Режим відображення клітинок таблиці.</summary>
public enum DisplayMode
{
    /// <summary>ВИРАЗ: показується вихідний текст клітинки.</summary>
    Expression,

    /// <summary>ЗНАЧЕННЯ: показується обчислене значення.</summary>
    Value,
}
