namespace Spreadsheet.ViewModels;

/// <summary>Подання рядка таблиці.</summary>
public sealed class RowViewModel(int rowNumber, IReadOnlyList<CellViewModel> cells)
{
    /// <summary>Номер рядка (з 1).</summary>
    public int RowNumber { get; } = rowNumber;

    /// <summary>Клітинки рядка зліва направо.</summary>
    public IReadOnlyList<CellViewModel> Cells { get; } = cells;
}
