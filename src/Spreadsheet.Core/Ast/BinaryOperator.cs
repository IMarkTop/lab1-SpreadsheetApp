namespace Spreadsheet.Core.Ast;

/// <summary>Бінарна операція.</summary>
public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,

    /// <summary>Точне ділення <c>/</c>.</summary>
    Divide,

    /// <summary>Цілочисельне ділення <c>div</c>.</summary>
    IntegerDivide,

    /// <summary>Остача <c>mod</c>.</summary>
    Modulo,

    Equal,
    Less,
    Greater,
    And,
    Or,
}
