namespace Spreadsheet.ViewModels.Services;

/// <summary>
/// Вибір користувача при запиті збереження незбережених змін.
/// </summary>
public enum SaveChangesChoice
{
    /// <summary>Зберегти зміни.</summary>
    Save,

    /// <summary>Не зберігати зміни (відкинути).</summary>
    Discard,

    /// <summary>Скасувати дію (залишитися в поточній таблиці).</summary>
    Cancel,
}
