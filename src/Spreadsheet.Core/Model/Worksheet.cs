using Spreadsheet.Core.Errors;
using Spreadsheet.Core.Evaluation;
using Spreadsheet.Core.Parsing;

namespace Spreadsheet.Core.Model;

/// <summary>Електронна таблиця: тексти клітинок, залежності, перерахунок і зміна розміру.</summary>
public sealed class Worksheet : ICellValueProvider
{
    /// <summary>Максимальна кількість рядків.</summary>
    public const int MaxRows = 1000;

    /// <summary>Максимальна кількість стовпців (A…ZZ).</summary>
    public const int MaxColumns = 702;

    private readonly Dictionary<CellAddress, Cell> _cells = new();
    private readonly Parser _parser = new();
    private readonly Evaluator _evaluator;
    private DependencyGraph _graph = new();

    /// <summary>Створює порожню таблицю заданого розміру (1…<see cref="MaxRows"/> × 1…<see cref="MaxColumns"/>).</summary>
    public Worksheet(int rowCount, int columnCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rowCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rowCount, MaxRows);
        ArgumentOutOfRangeException.ThrowIfLessThan(columnCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columnCount, MaxColumns);

        RowCount = rowCount;
        ColumnCount = columnCount;
        _evaluator = new Evaluator(this);
    }

    /// <summary>Кількість рядків.</summary>
    public int RowCount { get; private set; }

    /// <summary>Кількість стовпців.</summary>
    public int ColumnCount { get; private set; }

    /// <summary>Чи адреса лежить у межах таблиці.</summary>
    public bool Contains(CellAddress address) =>
        address.Row >= 1 && address.Row <= RowCount && address.Column >= 1 && address.Column <= ColumnCount;

    /// <summary>Повертає знімок клітинки.</summary>
    public CellSnapshot GetSnapshot(CellAddress address)
    {
        EnsureContains(address);
        return _cells.TryGetValue(address, out Cell? cell)
            ? new CellSnapshot(cell.RawText, false, cell.Result)
            : new CellSnapshot(string.Empty, true, null);
    }

    /// <summary>
    /// Задає текст клітинки й перераховує її та всі залежні. Повертає адреси перерахованих
    /// клітинок у порядку обчислення.
    /// </summary>
    public IReadOnlyList<CellAddress> SetCellText(CellAddress address, string? text)
    {
        EnsureContains(address);
        Store(address, text);
        return Recalculate([address]);
    }

    /// <summary>Адреси непорожніх клітинок (за рядком, потім за стовпцем).</summary>
    public IReadOnlyList<CellAddress> GetNonEmptyAddresses() => SortedKeys();

    /// <summary>Повний перерахунок: повторний розбір усіх текстів і перебудова графа залежностей.</summary>
    public void RecalculateAll()
    {
        _graph = new DependencyGraph();
        List<CellAddress> addresses = SortedKeys();
        foreach (CellAddress address in addresses)
        {
            Store(address, _cells[address].RawText);
        }

        Recalculate(addresses);
    }

    /// <summary>Вставляє порожній рядок перед <paramref name="index"/> (1…RowCount+1).</summary>
    public bool AddRow(int index)
    {
        if (index < 1 || index > RowCount + 1 || RowCount >= MaxRows)
        {
            return false;
        }

        Remap(a => a.Row >= index ? a with { Row = a.Row + 1 } : a);
        RowCount++;
        RecalculateAll();
        return true;
    }

    /// <summary>Видаляє рядок <paramref name="index"/> (1…RowCount); останній рядок видалити не можна.</summary>
    public bool RemoveRow(int index)
    {
        if (index < 1 || index > RowCount || RowCount <= 1)
        {
            return false;
        }

        Remap(a => a.Row == index ? null : a.Row > index ? a with { Row = a.Row - 1 } : a);
        RowCount--;
        RecalculateAll();
        return true;
    }

    /// <summary>Вставляє порожній стовпець перед <paramref name="index"/> (1…ColumnCount+1).</summary>
    public bool AddColumn(int index)
    {
        if (index < 1 || index > ColumnCount + 1 || ColumnCount >= MaxColumns)
        {
            return false;
        }

        Remap(a => a.Column >= index ? a with { Column = a.Column + 1 } : a);
        ColumnCount++;
        RecalculateAll();
        return true;
    }

    /// <summary>Видаляє стовпець <paramref name="index"/> (1…ColumnCount); останній стовпець видалити не можна.</summary>
    public bool RemoveColumn(int index)
    {
        if (index < 1 || index > ColumnCount || ColumnCount <= 1)
        {
            return false;
        }

        Remap(a => a.Column == index ? null : a.Column > index ? a with { Column = a.Column - 1 } : a);
        ColumnCount--;
        RecalculateAll();
        return true;
    }

    /// <inheritdoc />
    CellLookup ICellValueProvider.GetCell(CellAddress address)
    {
        if (!Contains(address))
        {
            return CellLookup.FromError(new ExpressionError(
                ErrorKind.InvalidReference, $"Клітинка {address} лежить поза межами таблиці", 0, 0));
        }

        if (!_cells.TryGetValue(address, out Cell? cell) || cell.Result is null)
        {
            return CellLookup.Empty;
        }

        return cell.Result.IsSuccess
            ? CellLookup.FromValue(cell.Result.Value)
            : CellLookup.FromError(cell.Result.Error!);
    }

    private void EnsureContains(CellAddress address)
    {
        if (!Contains(address))
        {
            throw new ArgumentOutOfRangeException(nameof(address), address, "Адреса лежить поза межами таблиці");
        }
    }

    private List<CellAddress> SortedKeys()
    {
        List<CellAddress> keys = _cells.Keys.ToList();
        keys.Sort((a, b) => a.Row != b.Row ? a.Row.CompareTo(b.Row) : a.Column.CompareTo(b.Column));
        return keys;
    }

    /// <summary>Зберігає текст, розбирає його й оновлює залежності (без обчислення).</summary>
    private void Store(CellAddress address, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _cells.Remove(address);
            _graph.ClearDependencies(address);
            return;
        }

        var cell = new Cell(text);
        ParseResult parsed = _parser.Parse(text);
        if (parsed.IsSuccess)
        {
            cell.Ast = parsed.Ast;
            _graph.SetDependencies(address, DependencyCollector.Collect(parsed.Ast!));
        }
        else
        {
            cell.Result = EvalResult.Failure(parsed.Error!);
            _graph.ClearDependencies(address);
        }

        _cells[address] = cell;
    }

    private IReadOnlyList<CellAddress> Recalculate(IEnumerable<CellAddress> seeds)
    {
        EvaluationPlan plan = _graph.Plan(seeds);
        foreach (CellAddress address in plan.Order)
        {
            if (!_cells.TryGetValue(address, out Cell? cell))
            {
                continue;
            }

            if (plan.Cyclic.Contains(address))
            {
                cell.Result = EvalResult.Failure(new ExpressionError(
                    ErrorKind.CircularReference, "Циклічне посилання", 0, cell.RawText.Length));
            }
            else if (cell.Ast is not null)
            {
                cell.Result = _evaluator.Evaluate(cell.Ast);
            }
        }

        return plan.Order;
    }

    /// <summary>Переносить тексти клітинок за відображенням адрес; <c>null</c> — клітинка видалена.</summary>
    private void Remap(Func<CellAddress, CellAddress?> map)
    {
        var texts = _cells.ToDictionary(pair => pair.Key, pair => pair.Value.RawText);
        _cells.Clear();
        foreach ((CellAddress address, string text) in texts)
        {
            CellAddress? moved = map(address);
            if (moved is not null)
            {
                _cells[moved.Value] = new Cell(text);
            }
        }
    }
}
