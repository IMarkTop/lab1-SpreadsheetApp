using System.Numerics;
using Spreadsheet.Core.Ast;
using Spreadsheet.Core.Errors;

namespace Spreadsheet.Core.Evaluation;

/// <summary>Обчислює синтаксичне дерево; помилки повертає як результат, а не виключення.</summary>
public sealed class Evaluator(ICellValueProvider cells)
{
    /// <summary>Обчислює вираз.</summary>
    public EvalResult Evaluate(Expr expr)
    {
        ArgumentNullException.ThrowIfNull(expr);

        return expr switch
        {
            NumberLiteral n => EvalResult.Success(Value.FromNumber(n.Value)),
            BoolLiteral b => EvalResult.Success(Value.FromBoolean(b.Value)),
            CellRefExpr c => EvaluateCell(c),
            UnaryExpr u => EvaluateUnary(u),
            BinaryExpr b => EvaluateBinary(b),
            _ => throw new ArgumentException("Невідомий вид вузла виразу", nameof(expr)),
        };
    }

    private EvalResult EvaluateCell(CellRefExpr expr)
    {
        CellLookup lookup = cells.GetCell(expr.Address);
        return lookup.Kind switch
        {
            CellLookupKind.Value => EvalResult.Success(lookup.Value),
            CellLookupKind.Error when lookup.Error!.Kind == ErrorKind.InvalidReference => Fail(
                ErrorKind.InvalidReference, lookup.Error.Message, expr),
            CellLookupKind.Error => Fail(
                ErrorKind.ReferencedCellError, $"Клітинка {expr.Address} містить помилку", expr),
            _ => Fail(ErrorKind.EmptyCell, $"Клітинка {expr.Address} порожня", expr),
        };
    }

    private EvalResult EvaluateUnary(UnaryExpr expr)
    {
        EvalResult operand = Evaluate(expr.Operand);
        if (!operand.IsSuccess)
        {
            return operand;
        }

        if (operand.Value.Kind != ValueKind.Boolean)
        {
            return Fail(ErrorKind.Type, "Операція «not» потребує логічного операнда", expr);
        }

        return EvalResult.Success(Value.FromBoolean(!operand.Value.Boolean));
    }

    private EvalResult EvaluateBinary(BinaryExpr expr)
    {
        // Обидва операнди обчислюються завжди (без short-circuit).
        EvalResult left = Evaluate(expr.Left);
        EvalResult right = Evaluate(expr.Right);
        if (!left.IsSuccess)
        {
            return left;
        }

        if (!right.IsSuccess)
        {
            return right;
        }

        Value l = left.Value;
        Value r = right.Value;
        string symbol = Symbol(expr.Operator);

        switch (expr.Operator)
        {
            case BinaryOperator.And:
            case BinaryOperator.Or:
                if (l.Kind != ValueKind.Boolean || r.Kind != ValueKind.Boolean)
                {
                    return Fail(ErrorKind.Type, $"Операція «{symbol}» потребує логічних операндів", expr);
                }

                return EvalResult.Success(Value.FromBoolean(
                    expr.Operator == BinaryOperator.And ? l.Boolean && r.Boolean : l.Boolean || r.Boolean));

            case BinaryOperator.Equal:
                if (l.Kind != r.Kind)
                {
                    return Fail(ErrorKind.Type, "Операція «=» потребує операндів одного типу", expr);
                }

                return EvalResult.Success(Value.FromBoolean(
                    l.Kind == ValueKind.Number ? l.Number == r.Number : l.Boolean == r.Boolean));
        }

        if (l.Kind != ValueKind.Number || r.Kind != ValueKind.Number)
        {
            return Fail(ErrorKind.Type, $"Операція «{symbol}» потребує числових операндів", expr);
        }

        BigInteger a = l.Number;
        BigInteger b = r.Number;
        switch (expr.Operator)
        {
            case BinaryOperator.Add:
                return Number(a + b);
            case BinaryOperator.Subtract:
                return Number(a - b);
            case BinaryOperator.Multiply:
                return Number(a * b);
            case BinaryOperator.Less:
                return EvalResult.Success(Value.FromBoolean(a < b));
            case BinaryOperator.Greater:
                return EvalResult.Success(Value.FromBoolean(a > b));
        }

        if (b.IsZero)
        {
            return Fail(ErrorKind.DivisionByZero, "Ділення на нуль", expr);
        }

        switch (expr.Operator)
        {
            case BinaryOperator.IntegerDivide:
                return Number(BigInteger.Divide(a, b));
            case BinaryOperator.Modulo:
                return Number(BigInteger.Remainder(a, b));
            default:
                BigInteger quotient = BigInteger.DivRem(a, b, out BigInteger remainder);
                return remainder.IsZero
                    ? Number(quotient)
                    : Fail(ErrorKind.NonExactDivision, "Результат ділення «/» не є цілим числом", expr);
        }
    }

    private static EvalResult Number(BigInteger value) => EvalResult.Success(Value.FromNumber(value));

    private static EvalResult Fail(ErrorKind kind, string message, Expr at) =>
        EvalResult.Failure(new ExpressionError(kind, message, at.Position, at.Length));

    private static string Symbol(BinaryOperator op) => op switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        BinaryOperator.IntegerDivide => "div",
        BinaryOperator.Modulo => "mod",
        BinaryOperator.Equal => "=",
        BinaryOperator.Less => "<",
        BinaryOperator.Greater => ">",
        BinaryOperator.And => "and",
        _ => "or",
    };
}
