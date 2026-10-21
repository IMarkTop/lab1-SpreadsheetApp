using Spreadsheet.Core.Errors;

namespace Spreadsheet.Core.Evaluation;

/// <summary>Відповідь постачальника значень про клітинку: порожня, значення або помилка.</summary>
public sealed record CellLookup(CellLookupKind Kind, Value Value, ExpressionError? Error)
{
    /// <summary>Порожня клітинка.</summary>
    public static CellLookup Empty { get; } = new(CellLookupKind.Empty, default, null);

    /// <summary>Клітинка зі значенням.</summary>
    public static CellLookup FromValue(Value value) => new(CellLookupKind.Value, value, null);

    /// <summary>Клітинка з помилкою.</summary>
    public static CellLookup FromError(ExpressionError error) => new(CellLookupKind.Error, default, error);
}
