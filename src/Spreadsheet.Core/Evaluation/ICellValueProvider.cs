using Spreadsheet.Core.Model;

namespace Spreadsheet.Core.Evaluation;

/// <summary>Постачальник значень клітинок для <see cref="Evaluator"/>.</summary>
public interface ICellValueProvider
{
    /// <summary>Повертає стан клітинки за адресою.</summary>
    CellLookup GetCell(CellAddress address);
}
