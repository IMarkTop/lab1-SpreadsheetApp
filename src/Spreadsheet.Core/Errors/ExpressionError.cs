namespace Spreadsheet.Core.Errors;

/// <summary>Помилка виразу з локалізацією у вихідному рядку.</summary>
/// <param name="Kind">Вид помилки.</param>
/// <param name="Message">Повідомлення українською.</param>
/// <param name="Position">Індекс першого символу помилки (з 0) у вихідному рядку.</param>
/// <param name="Length">Довжина фрагмента з помилкою.</param>
public sealed record ExpressionError(ErrorKind Kind, string Message, int Position, int Length);
