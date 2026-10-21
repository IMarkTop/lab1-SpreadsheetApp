using Spreadsheet.Core.Errors;

namespace Spreadsheet.Core.Evaluation;

/// <summary>Результат обчислення: значення або помилка.</summary>
/// <param name="Value">Значення; за наявності помилки не використовується.</param>
/// <param name="Error">Помилка або <c>null</c>.</param>
public sealed record EvalResult(Value Value, ExpressionError? Error)
{
    /// <summary>Чи обчислення успішне.</summary>
    public bool IsSuccess => Error is null;

    /// <summary>Успішний результат.</summary>
    public static EvalResult Success(Value value) => new(value, null);

    /// <summary>Результат з помилкою.</summary>
    public static EvalResult Failure(ExpressionError error) => new(default, error);
}
