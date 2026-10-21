using System.Numerics;

namespace Spreadsheet.Core.Evaluation;

/// <summary>Значення виразу: ціле число довільної довжини або логічне значення.</summary>
public readonly record struct Value
{
    private Value(ValueKind kind, BigInteger number, bool boolean)
    {
        Kind = kind;
        Number = number;
        Boolean = boolean;
    }

    /// <summary>Тип значення.</summary>
    public ValueKind Kind { get; }

    /// <summary>Число (має сенс, якщо <see cref="Kind"/> — <see cref="ValueKind.Number"/>).</summary>
    public BigInteger Number { get; }

    /// <summary>Логічне значення (має сенс, якщо <see cref="Kind"/> — <see cref="ValueKind.Boolean"/>).</summary>
    public bool Boolean { get; }

    /// <summary>Створює числове значення.</summary>
    public static Value FromNumber(BigInteger number) => new(ValueKind.Number, number, false);

    /// <summary>Створює логічне значення.</summary>
    public static Value FromBoolean(bool boolean) => new(ValueKind.Boolean, BigInteger.Zero, boolean);

    /// <summary>Текстове подання: число або <c>true</c>/<c>false</c>.</summary>
    public override string ToString() =>
        Kind == ValueKind.Number ? Number.ToString() : (Boolean ? "true" : "false");
}
