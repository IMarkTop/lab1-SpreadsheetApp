namespace Spreadsheet.ViewModels;

/// <summary>Дані завершення редагування клітинки для передачі в команду.</summary>
/// <param name="Cell">Подання клітинки, яку було відредаговано.</param>
/// <param name="Text">Новий введений текст.</param>
public sealed record CellEdit(CellViewModel Cell, string? Text);
