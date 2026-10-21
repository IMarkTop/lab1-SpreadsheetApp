using System.Text;

namespace Spreadsheet.Core.Model;

/// <summary>Адреса клітинки: рядок і стовпець нумеруються з 1 (A=1, Z=26, AA=27).</summary>
/// <param name="Row">Номер рядка (з 1).</param>
/// <param name="Column">Номер стовпця (з 1).</param>
public readonly record struct CellAddress(int Row, int Column)
{
    /// <summary>
    /// Розбирає адресу виду <c>A3</c>, <c>AB12</c> (регістр не важливий).
    /// Повертає <c>false</c>, якщо форма некоректна або номери не вміщуються в <see cref="int"/>.
    /// </summary>
    public static bool TryParse(string? text, out CellAddress address)
    {
        address = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        int index = 0;
        long column = 0;
        while (index < text.Length && char.IsAsciiLetter(text[index]))
        {
            column = column * 26 + (char.ToUpperInvariant(text[index]) - 'A' + 1);
            if (column > int.MaxValue)
            {
                return false;
            }

            index++;
        }

        if (index == 0 || index == text.Length)
        {
            return false;
        }

        long row = 0;
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            row = row * 10 + (text[index] - '0');
            if (row > int.MaxValue)
            {
                return false;
            }

            index++;
        }

        if (index != text.Length || row < 1)
        {
            return false;
        }

        address = new CellAddress((int)row, (int)column);
        return true;
    }

    /// <summary>Повертає адресу у вигляді <c>A3</c>.</summary>
    public override string ToString()
    {
        var letters = new StringBuilder();
        for (int column = Column; column > 0; column = (column - 1) / 26)
        {
            letters.Insert(0, (char)('A' + (column - 1) % 26));
        }

        return $"{letters}{Row}";
    }
}
