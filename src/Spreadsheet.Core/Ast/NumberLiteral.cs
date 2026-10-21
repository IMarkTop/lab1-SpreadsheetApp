using System.Numerics;

namespace Spreadsheet.Core.Ast;

/// <summary>Цілочисельна константа довільної довжини.</summary>
public sealed record NumberLiteral(BigInteger Value, int Position, int Length) : Expr(Position, Length);
