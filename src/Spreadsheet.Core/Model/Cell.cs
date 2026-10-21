using Spreadsheet.Core.Ast;
using Spreadsheet.Core.Evaluation;

namespace Spreadsheet.Core.Model;

/// <summary>Внутрішній стан непорожньої клітинки.</summary>
internal sealed class Cell(string rawText)
{
    /// <summary>Вихідний текст.</summary>
    public string RawText { get; } = rawText;

    /// <summary>Синтаксичне дерево; <c>null</c>, якщо розбір не вдався.</summary>
    public Expr? Ast { get; set; }

    /// <summary>Результат обчислення.</summary>
    public EvalResult? Result { get; set; }
}
