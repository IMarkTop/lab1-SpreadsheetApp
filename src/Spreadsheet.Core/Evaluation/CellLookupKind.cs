namespace Spreadsheet.Core.Evaluation;

/// <summary>Стан клітинки, на яку є посилання.</summary>
public enum CellLookupKind
{
    /// <summary>Клітинка порожня.</summary>
    Empty,

    /// <summary>Клітинка має значення.</summary>
    Value,

    /// <summary>Клітинка містить помилку.</summary>
    Error,
}
