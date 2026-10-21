namespace Spreadsheet.Core.Ast;

/// <summary>Бінарна операція; <see cref="Expr.Position"/> — позиція знака операції.</summary>
public sealed record BinaryExpr(BinaryOperator Operator, Expr Left, Expr Right, int Position, int Length)
    : Expr(Position, Length);
