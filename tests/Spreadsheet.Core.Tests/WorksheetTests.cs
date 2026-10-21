using System.Collections.Generic;
using Spreadsheet.Core.Errors;
using Spreadsheet.Core.Evaluation;
using Spreadsheet.Core.Model;
using Xunit;

namespace Spreadsheet.Core.Tests;

/// <summary>
/// Юніт-тести функціональності ядра електронної таблиці (Варіант 31).
/// </summary>
public class WorksheetTests
{
    private static CellAddress Addr(string text)
    {
        Assert.True(CellAddress.TryParse(text, out CellAddress address));
        return address;
    }

    private static void Set(Worksheet sheet, string address, string? text) =>
        sheet.SetCellText(Addr(address), text);

    private static EvalResult GetResult(Worksheet sheet, string address)
    {
        EvalResult? result = sheet.GetSnapshot(Addr(address)).Result;
        Assert.NotNull(result);
        return result;
    }

    /// <summary>
    /// Тест 1: Обчислення логічного виразу операціями Варіанта 31 (+, -, *, /, mod, div, =, &lt;, &gt;, not, and, or).
    /// </summary>
    [Fact]
    public void Evaluate_Expression_WithVariant31Operations_ReturnsCorrectBooleanResult()
    {
        var sheet = new Worksheet(5, 5);

        // (10 + 5) mod 4 = 3 and not (2 * 3 > 10)
        // 15 mod 4 = 3 (true); 6 > 10 (false) -> not false (true); true and true -> true
        Set(sheet, "A1", "(10 + 5) mod 4 = 3 and not (2 * 3 > 10)");

        EvalResult result = GetResult(sheet, "A1");

        Assert.True(result.IsSuccess);
        Assert.Equal(ValueKind.Boolean, result.Value.Kind);
        Assert.True(result.Value.Boolean);
    }

    /// <summary>
    /// Тест 2: Посилання між клітинками та каскадний перерахунок залежностей.
    /// </summary>
    [Fact]
    public void CellReferences_RecalculatesDependentChainCorrectly()
    {
        var sheet = new Worksheet(5, 5);

        Set(sheet, "A1", "10");
        Set(sheet, "A2", "A1 * 2 + 5"); // 10 * 2 + 5 = 25
        Set(sheet, "A3", "A2 > 20");     // 25 > 20 -> true

        EvalResult resA2 = GetResult(sheet, "A2");
        EvalResult resA3 = GetResult(sheet, "A3");
        Assert.True(resA2.IsSuccess);
        Assert.Equal(25, resA2.Value.Number);
        Assert.True(resA3.IsSuccess);
        Assert.True(resA3.Value.Boolean);

        // Змінюємо значення A1: залежні клітинки A2 та A3 мають автоматично перерахуватися
        Set(sheet, "A1", "2");

        resA2 = GetResult(sheet, "A2");
        resA3 = GetResult(sheet, "A3");
        Assert.Equal(9, resA2.Value.Number); // 2 * 2 + 5 = 9
        Assert.False(resA3.Value.Boolean);   // 9 > 20 -> false
    }

    /// <summary>
    /// Тест 3: Локалізація синтаксичної помилки (перший етап обробки з визначенням позиції).
    /// </summary>
    [Fact]
    public void SyntaxError_IdentifiesErrorAndLocalizesPosition()
    {
        var sheet = new Worksheet(5, 5);

        // Некоректний вираз: оператор '+' на кінці рядка без другого операнда
        Set(sheet, "B1", "10 +");

        EvalResult result = GetResult(sheet, "B1");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.Syntax, result.Error.Kind);
        Assert.Equal(4, result.Error.Position); // Позиція помилки наприкінці рядка
    }

    /// <summary>
    /// Тест 4: Виявлення циклічного посилання (Circular Reference).
    /// </summary>
    [Fact]
    public void CircularReference_DetectsCycleAndMarksCellsWithError()
    {
        var sheet = new Worksheet(5, 5);

        // Взаємне посилання клітинок одна на одну: A1 -> B1 -> A1
        Set(sheet, "A1", "B1 + 1");
        Set(sheet, "B1", "A1 + 1");

        EvalResult resA1 = GetResult(sheet, "A1");
        EvalResult resB1 = GetResult(sheet, "B1");

        Assert.False(resA1.IsSuccess);
        Assert.Equal(ErrorKind.CircularReference, resA1.Error?.Kind);
        Assert.False(resB1.IsSuccess);
        Assert.Equal(ErrorKind.CircularReference, resB1.Error?.Kind);
    }

    /// <summary>
    /// Тест 5: Семантична помилка часу виконання (ділення на нуль div / mod).
    /// </summary>
    [Fact]
    public void EvaluationError_DivisionByZero_ReturnsAppropriateError()
    {
        var sheet = new Worksheet(5, 5);

        Set(sheet, "C1", "100 div 0");

        EvalResult result = GetResult(sheet, "C1");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(ErrorKind.DivisionByZero, result.Error.Kind);
    }
}
