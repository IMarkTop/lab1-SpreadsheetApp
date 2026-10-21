using Spreadsheet.Core.Model;

namespace Spreadsheet.Core.Ast;

/// <summary>Посилання на клітинку.</summary>
public sealed record CellRefExpr(CellAddress Address, int Position, int Length) : Expr(Position, Length);
