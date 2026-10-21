namespace Spreadsheet.Storage;

/// <summary>
/// Представляє розташування файлу в сховищі та його зрозумілу назву для відображення в інтерфейсі.
/// </summary>
/// <param name="Location">Сире розташування файлу (локальний шлях або ідентифікатор Google Drive із префіксом gdrive:).</param>
/// <param name="DisplayName">Відображувана назва документа (наприклад, "budget.json").</param>
public record StorageLocation(string Location, string DisplayName);
