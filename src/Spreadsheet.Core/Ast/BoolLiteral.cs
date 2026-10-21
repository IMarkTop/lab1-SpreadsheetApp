namespace Spreadsheet.Core.Ast;

/// <summary>Логічна константа <c>true</c>/<c>false</c>.</summary>
public sealed record BoolLiteral(bool Value, int Position, int Length) : Expr(Position, Length);
