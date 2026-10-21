using Spreadsheet.Core.Errors;

namespace Spreadsheet.Core.Lexing;

/// <summary>Результат лексичного аналізу: лексеми (з кінцевою <see cref="TokenKind.End"/>) або помилка.</summary>
/// <param name="Tokens">Лексеми; порожній список у разі помилки.</param>
/// <param name="Error">Перша лексична помилка або <c>null</c>.</param>
public sealed record LexResult(IReadOnlyList<Token> Tokens, ExpressionError? Error)
{
    /// <summary>Чи завершився аналіз без помилки.</summary>
    public bool IsSuccess => Error is null;
}
