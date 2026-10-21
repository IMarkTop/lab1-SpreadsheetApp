using Spreadsheet.Core.Ast;

namespace Spreadsheet.Core.Model;

/// <summary>Збирає унікальні посилання на клітинки з дерева виразу (ітеративно).</summary>
internal static class DependencyCollector
{
    /// <summary>Повертає унікальні адреси, на які посилається вираз (у порядку появи).</summary>
    public static IReadOnlyList<CellAddress> Collect(Expr root)
    {
        var result = new List<CellAddress>();
        var seen = new HashSet<CellAddress>();
        var stack = new Stack<Expr>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            switch (stack.Pop())
            {
                case CellRefExpr cell:
                    if (seen.Add(cell.Address))
                    {
                        result.Add(cell.Address);
                    }

                    break;
                case UnaryExpr unary:
                    stack.Push(unary.Operand);
                    break;
                case BinaryExpr binary:
                    // Правий піде в стек першим, щоб лівий обробився раніше.
                    stack.Push(binary.Right);
                    stack.Push(binary.Left);
                    break;
            }
        }

        return result;
    }
}
