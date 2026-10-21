namespace Spreadsheet.Core.Errors;

/// <summary>Вид помилки виразу.</summary>
public enum ErrorKind
{
    /// <summary>Синтаксична помилка.</summary>
    Syntax,

    /// <summary>Порушення типів операндів.</summary>
    Type,

    /// <summary>Ділення на нуль.</summary>
    DivisionByZero,

    /// <summary>Неточне ділення для операції <c>/</c>.</summary>
    NonExactDivision,

    /// <summary>Посилання на порожню клітинку.</summary>
    EmptyCell,

    /// <summary>Посилання на клітинку, що містить помилку.</summary>
    ReferencedCellError,

    /// <summary>Циклічне посилання.</summary>
    CircularReference,

    /// <summary>Посилання на клітинку поза межами таблиці.</summary>
    InvalidReference,
}
