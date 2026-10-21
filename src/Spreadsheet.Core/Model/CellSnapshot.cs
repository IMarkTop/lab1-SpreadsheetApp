using Spreadsheet.Core.Evaluation;

namespace Spreadsheet.Core.Model;

/// <summary>Знімок клітинки: вихідний текст і результат обчислення.</summary>
/// <param name="RawText">Вихідний текст (порожній рядок для порожньої клітинки).</param>
/// <param name="IsEmpty">Чи клітинка порожня.</param>
/// <param name="Result">Значення або помилка; <c>null</c> для порожньої клітинки.</param>
public sealed record CellSnapshot(string RawText, bool IsEmpty, EvalResult? Result);
