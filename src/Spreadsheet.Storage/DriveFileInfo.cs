using System;

namespace Spreadsheet.Storage;

/// <summary>
/// Інформація про файл таблиці на Google Drive.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор файлу на Google Drive.</param>
/// <param name="Name">Назва файлу (з розширенням).</param>
/// <param name="ModifiedTime">Час останньої зміни файлу (UTC).</param>
public record DriveFileInfo(string Id, string Name, DateTimeOffset? ModifiedTime);
