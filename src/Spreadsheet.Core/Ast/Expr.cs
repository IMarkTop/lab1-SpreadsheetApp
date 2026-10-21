namespace Spreadsheet.Core.Ast;

/// <summary>
/// Базовий вузол синтаксичного дерева. <paramref name="Position"/> і <paramref name="Length"/>
/// описують ключовий фрагмент вузла: для літералів і посилань — сам фрагмент,
/// для операцій — знак операції (саме там показуються помилки обчислення).
/// </summary>
/// <param name="Position">Індекс першого символу (з 0).</param>
/// <param name="Length">Довжина фрагмента.</param>
public abstract record Expr(int Position, int Length);
