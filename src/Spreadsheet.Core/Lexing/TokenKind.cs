namespace Spreadsheet.Core.Lexing;

/// <summary>Вид лексеми.</summary>
public enum TokenKind
{
    Number,
    CellRef,
    True,
    False,
    And,
    Or,
    Not,
    Div,
    Mod,
    Plus,
    Minus,
    Star,
    Slash,
    Equal,
    Less,
    Greater,
    LeftParen,
    RightParen,

    /// <summary>Кінець вводу (позиція дорівнює довжині рядка, довжина 0).</summary>
    End,
}
