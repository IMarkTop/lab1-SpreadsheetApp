namespace Spreadsheet.Core.Ast;

/// <summary>Унарна операція; <see cref="Expr.Position"/> — позиція знака операції.</summary>
public sealed record UnaryExpr(UnaryOperator Operator, Expr Operand, int Position, int Length) : Expr(Position, Length);
