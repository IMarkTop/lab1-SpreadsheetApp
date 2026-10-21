namespace Spreadsheet.Core.Lexing;

/// <summary>Лексема з позицією у вихідному рядку.</summary>
/// <param name="Kind">Вид лексеми.</param>
/// <param name="Text">Текст лексеми у вихідному рядку.</param>
/// <param name="Position">Індекс першого символу (з 0).</param>
public sealed record Token(TokenKind Kind, string Text, int Position)
{
    /// <summary>Довжина лексеми.</summary>
    public int Length => Text.Length;
}
