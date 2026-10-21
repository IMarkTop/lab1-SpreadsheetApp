using System;

namespace Spreadsheet.Storage;

/// <summary>
/// Помилка збереження, завантаження або валідації формату таблиці.
/// </summary>
public class StorageException : Exception
{
    public StorageException(string message) : base(message)
    {
    }

    public StorageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
