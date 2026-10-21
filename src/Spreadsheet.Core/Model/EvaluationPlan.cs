namespace Spreadsheet.Core.Model;

/// <summary>Порядок обчислення клітинок і множина клітинок, що лежать на циклі.</summary>
/// <param name="Order">Адреси в порядку обчислення (залежності раніше залежних).</param>
/// <param name="Cyclic">Клітинки, що лежать на циклі (включно із самопосиланням).</param>
internal sealed record EvaluationPlan(IReadOnlyList<CellAddress> Order, IReadOnlySet<CellAddress> Cyclic);
