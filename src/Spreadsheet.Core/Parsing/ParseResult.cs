using Spreadsheet.Core.Ast;
using Spreadsheet.Core.Errors;

namespace Spreadsheet.Core.Parsing;

/// <summary>Результат розбору: дерево або перша синтаксична помилка.</summary>
/// <param name="Ast">Корінь дерева; <c>null</c> у разі помилки.</param>
/// <param name="Error">Помилка; <c>null</c> у разі успіху.</param>
public sealed record ParseResult(Expr? Ast, ExpressionError? Error)
{
    /// <summary>Чи успішний розбір.</summary>
    public bool IsSuccess => Error is null;
}
