using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Spreadsheet.Core.Model;

namespace Spreadsheet.Storage;

/// <summary>
/// Серіалізація та десеріалізація таблиці у формат JSON (версія 1).
/// </summary>
public static class SpreadsheetSerializer
{
    public const string ExpectedFormat = "SpreadsheetApp";
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Серіалізує таблицю у рядок JSON.
    /// </summary>
    public static string ToJson(Worksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var cellsList = new List<object>();
        foreach (CellAddress address in sheet.GetNonEmptyAddresses())
        {
            CellSnapshot snapshot = sheet.GetSnapshot(address);
            if (!snapshot.IsEmpty)
            {
                cellsList.Add(new
                {
                    address = address.ToString(),
                    text = snapshot.RawText,
                });
            }
        }

        var document = new
        {
            format = ExpectedFormat,
            version = CurrentVersion,
            rows = sheet.RowCount,
            columns = sheet.ColumnCount,
            cells = cellsList,
        };

        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    /// <summary>
    /// Десеріалізує таблицю з рядка JSON із повною валідацією формату.
    /// </summary>
    public static Worksheet FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new StorageException("Файл порожній");
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл не є коректним JSON: {ex.Message}", ex);
        }

        if (root is not JsonObject rootObj)
        {
            throw new StorageException("Кореневий елемент JSON має бути об'єктом");
        }

        // 1. Формат
        if (!rootObj.TryGetPropertyValue("format", out JsonNode? formatNode) || formatNode is null)
        {
            if (rootObj.ContainsKey("kind") || rootObj.ContainsKey("mimeType") ||
                (!rootObj.ContainsKey("rows") && !rootObj.ContainsKey("columns") && !rootObj.ContainsKey("cells")))
            {
                throw new StorageException("Це не файл таблиці SpreadsheetApp: у файлі відсутнє обов'язкове поле 'format'");
            }

            throw new StorageException("У файлі відсутнє обов'язкове поле 'format'");
        }

        string? format = formatNode.GetValue<string?>();
        if (format != ExpectedFormat)
        {
            throw new StorageException($"Це не файл таблиці SpreadsheetApp: невідомий формат файлу '{format}' (очікується '{ExpectedFormat}')");
        }

        // 2. Версія
        if (!rootObj.TryGetPropertyValue("version", out JsonNode? versionNode) || versionNode is null)
        {
            throw new StorageException("У файлі відсутнє обов'язкове поле 'version'");
        }

        int version;
        try
        {
            version = versionNode.GetValue<int>();
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            throw new StorageException("Поле 'version' має бути цілим числом", ex);
        }

        if (version <= 0 || version > CurrentVersion)
        {
            throw new StorageException($"Невідома версія формату: {version} (підтримується лише {CurrentVersion})");
        }

        // 3. Розміри
        if (!rootObj.TryGetPropertyValue("rows", out JsonNode? rowsNode) || rowsNode is null)
        {
            throw new StorageException("У файлі відсутнє обов'язкове поле 'rows'");
        }

        int rows;
        try
        {
            rows = rowsNode.GetValue<int>();
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            throw new StorageException("Поле 'rows' має бути цілим числом", ex);
        }

        if (rows < 1 || rows > Worksheet.MaxRows)
        {
            throw new StorageException($"Кількість рядків ({rows}) має бути в межах від 1 до {Worksheet.MaxRows}");
        }

        if (!rootObj.TryGetPropertyValue("columns", out JsonNode? colsNode) || colsNode is null)
        {
            throw new StorageException("У файлі відсутнє обов'язкове поле 'columns'");
        }

        int columns;
        try
        {
            columns = colsNode.GetValue<int>();
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            throw new StorageException("Поле 'columns' має бути цілим числом", ex);
        }

        if (columns < 1 || columns > Worksheet.MaxColumns)
        {
            throw new StorageException($"Кількість стовпців ({columns}) має бути в межах від 1 до {Worksheet.MaxColumns}");
        }

        // 4. Клітинки
        if (!rootObj.TryGetPropertyValue("cells", out JsonNode? cellsNode) || cellsNode is null)
        {
            throw new StorageException("У файлі відсутнє обов'язкове поле 'cells'");
        }

        if (cellsNode is not JsonArray cellsArray)
        {
            throw new StorageException("Поле 'cells' має бути масивом");
        }

        var cellEntries = new List<(CellAddress Address, string Text)>();
        var seenAddresses = new HashSet<CellAddress>();

        foreach (JsonNode? item in cellsArray)
        {
            if (item is not JsonObject cellObj)
            {
                throw new StorageException("Елемент масиву 'cells' має бути об'єктом");
            }

            if (!cellObj.TryGetPropertyValue("address", out JsonNode? addrNode) || addrNode is null)
            {
                throw new StorageException("У клітинки відсутнє обов'язкове поле 'address'");
            }

            string? addrText = addrNode.GetValue<string?>();
            if (string.IsNullOrWhiteSpace(addrText) || !CellAddress.TryParse(addrText, out CellAddress address))
            {
                throw new StorageException($"Некоректна адреса клітинки: '{addrText}'");
            }

            if (address.Row < 1 || address.Row > rows || address.Column < 1 || address.Column > columns)
            {
                throw new StorageException($"Адреса {address} виходить за межі таблиці розміром {rows}×{columns}");
            }

            if (!seenAddresses.Add(address))
            {
                throw new StorageException($"Дубльована адреса клітинки: {address}");
            }

            if (!cellObj.TryGetPropertyValue("text", out JsonNode? textNode) || textNode is null)
            {
                throw new StorageException($"У клітинки {address} відсутнє обов'язкове поле 'text'");
            }

            string? text = textNode.GetValue<string?>();
            cellEntries.Add((address, text ?? string.Empty));
        }

        // 5. Побудова Worksheet
        var sheet = new Worksheet(rows, columns);
        foreach (var (address, text) in cellEntries)
        {
            sheet.SetCellText(address, text);
        }

        return sheet;
    }
}
