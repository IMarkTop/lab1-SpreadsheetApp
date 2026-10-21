using System.Threading.Tasks;

namespace Spreadsheet.ViewModels.Services;

/// <summary>Діалоги, які ViewModel викликає через інтерфейс (реалізація — у шарі UI).</summary>
public interface IDialogService
{
    /// <summary>Показує повідомлення про помилку.</summary>
    Task ShowErrorAsync(string message);

    /// <summary>Запитує підтвердження; <c>true</c> — користувач погодився.</summary>
    Task<bool> ConfirmAsync(string message);

    /// <summary>Відкриває довідку.</summary>
    Task ShowHelpAsync();

    /// <summary>Відкриває вікно «Про програму».</summary>
    Task ShowAboutAsync();

    /// <summary>Відкриває діалог вибору файлу для читання; повертає шлях або <c>null</c> при скасуванні.</summary>
    Task<string?> PickOpenFileAsync();

    /// <summary>Відкриває діалог вибору файлу для збереження; повертає шлях або <c>null</c> при скасуванні.</summary>
    Task<string?> PickSaveFileAsync(string suggestedName);

    /// <summary>Запитує збереження незбережених змін перед відкриттям, створенням чи закриттям.</summary>
    Task<SaveChangesChoice> AskSaveChangesAsync(string documentName);

    /// <summary>Відкриває діалог вибору файлу з Google Drive; повертає розташування або <c>null</c> при скасуванні.</summary>
    Task<Storage.StorageLocation?> PickDriveFileAsync();

    /// <summary>Запитує назву нового файлу для збереження на Google Drive; повертає назву або <c>null</c> при скасуванні.</summary>
    Task<string?> AskDriveFileNameAsync(string suggestedName);
}
