using System.Threading;
using System.Threading.Tasks;
using Spreadsheet.Core.Model;

namespace Spreadsheet.Storage;

/// <summary>
/// Інтерфейс для збереження та завантаження таблиці.
/// </summary>
public interface IStorage
{
    /// <summary>
    /// Зберігає таблицю за вказаним розташуванням та повертає підсумкове розташування (наприклад, з новим id для Drive).
    /// </summary>
    Task<string> SaveAsync(Worksheet sheet, string location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Завантажує таблицю з вказаного розташування.
    /// </summary>
    Task<Worksheet> LoadAsync(string location, CancellationToken cancellationToken = default);
}
