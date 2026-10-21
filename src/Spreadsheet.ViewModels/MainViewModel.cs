using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Spreadsheet.Core.Model;
using Spreadsheet.Storage;
using Spreadsheet.ViewModels.Services;
using Spreadsheet.ViewModels.Text;

namespace Spreadsheet.ViewModels;

/// <summary>Головна модель подання: сітка клітинок, виділення, рядок формул, команди.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    /// <summary>Кількість рядків нової таблиці.</summary>
    public const int DefaultRows = 20;

    /// <summary>Кількість стовпців нової таблиці.</summary>
    public const int DefaultColumns = 10;

    private readonly IDialogService _dialogs;
    private readonly IStorage _storage;
    private Worksheet _worksheet;

    /// <summary>Рядки сітки.</summary>
    [ObservableProperty]
    private IReadOnlyList<RowViewModel> _rows = [];

    /// <summary>Заголовки стовпців: A, B, …, ZZ.</summary>
    [ObservableProperty]
    private IReadOnlyList<string> _columnHeaders = [];

    /// <summary>Кількість рядків.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InsertRowAboveCommand))]
    [NotifyCanExecuteChangedFor(nameof(InsertRowBelowCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteRowCommand))]
    private int _rowCount;

    /// <summary>Кількість стовпців.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InsertColumnLeftCommand))]
    [NotifyCanExecuteChangedFor(nameof(InsertColumnRightCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteColumnCommand))]
    private int _columnCount;

    /// <summary>Режим відображення.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValueMode))]
    private DisplayMode _mode = DisplayMode.Expression;

    /// <summary>Виділена клітинка (може бути <c>null</c>).</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitFormulaCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelFormulaEditCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearCellCommand))]
    [NotifyCanExecuteChangedFor(nameof(InsertRowAboveCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteRowCommand))]
    [NotifyCanExecuteChangedFor(nameof(InsertColumnLeftCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteColumnCommand))]
    private CellViewModel? _selectedCell;

    /// <summary>Адреса виділеної клітинки (порожньо, якщо виділення немає).</summary>
    [ObservableProperty]
    private string _selectedAddressText = string.Empty;

    /// <summary>Текст у рядку формул.</summary>
    [ObservableProperty]
    private string _formulaText = string.Empty;

    /// <summary>Повідомлення про помилку виділеної клітинки.</summary>
    [ObservableProperty]
    private string _formulaErrorText = string.Empty;

    /// <summary>Повідомлення в рядку стану.</summary>
    [ObservableProperty]
    private string _statusMessage = UiText.StatusReady;

    private long _documentVersion;

    /// <summary>Поточне розташування документа у сховищі (локальний шлях або Google Drive).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentFilePath))]
    [NotifyPropertyChangedFor(nameof(DocumentName))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private StorageLocation? _currentLocation;

    /// <summary>Шлях до поточного збереженого файлу (null для нового файлу). Тільки для читання.</summary>
    public string? CurrentFilePath => CurrentLocation?.Location;

    /// <summary>Чи є незбережені зміни в таблиці.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private bool _isDirty;

    /// <summary>Назва поточного документа або «Без назви».</summary>
    public string DocumentName =>
        CurrentLocation?.DisplayName ?? UiText.DefaultDocumentName;

    /// <summary>Заголовок вікна програми.</summary>
    public string WindowTitle =>
        $"{DocumentName}{(IsDirty ? "*" : "")} — {UiText.AppTitle}";

    /// <summary>Створює модель для заданої таблиці зі сховищем.</summary>
    public MainViewModel(Worksheet worksheet, IDialogService dialogs, IStorage storage)
    {
        ArgumentNullException.ThrowIfNull(worksheet);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(storage);
        _worksheet = worksheet;
        _dialogs = dialogs;
        _storage = storage;
        Rebuild();
    }

    /// <summary>Створює модель з новою таблицею 20×10 зі сховищем.</summary>
    public MainViewModel(IDialogService dialogs, IStorage storage)
        : this(new Worksheet(DefaultRows, DefaultColumns), dialogs, storage)
    {
    }

    /// <summary>Режим ЗНАЧЕННЯ (для перемикача на панелі інструментів).</summary>
    public bool IsValueMode
    {
        get => Mode == DisplayMode.Value;
        set => Mode = value ? DisplayMode.Value : DisplayMode.Expression;
    }

    /// <summary>Підставляє нову таблицю: виділення скидається, сітка перебудовується.</summary>
    public void LoadWorksheet(Worksheet worksheet)
    {
        ArgumentNullException.ThrowIfNull(worksheet);
        _worksheet = worksheet;
        _documentVersion = 0;
        SelectedCell = null;
        Rebuild();
        StatusMessage = UiText.StatusReady;
    }

    /// <summary>
    /// Зберігає текст клітинки; оновлює лише ті клітинки, які Worksheet перерахував,
    /// а також рядок формул, якщо виділена клітинка серед них.
    /// Фіксує IsDirty при фактичній зміні тексту.
    /// </summary>
    public void CommitCell(CellViewModel cell, string? text)
    {
        ArgumentNullException.ThrowIfNull(cell);
        if (!_worksheet.Contains(cell.Address))
        {
            StatusMessage = UiText.StatusCellOutOfTable;
            return;
        }

        string textBefore = _worksheet.GetSnapshot(cell.Address).RawText;
        IReadOnlyList<CellAddress> updated = _worksheet.SetCellText(cell.Address, text);
        string textAfter = _worksheet.GetSnapshot(cell.Address).RawText;

        if (textBefore != textAfter)
        {
            _documentVersion++;
            IsDirty = true;
        }

        foreach (CellAddress address in updated)
        {
            CellAt(address).Update(_worksheet.GetSnapshot(address), Mode);
        }

        if (SelectedCell is not null && updated.Contains(SelectedCell.Address))
        {
            RefreshFormulaBar();
        }

        StatusMessage = string.Format(
            CultureInfo.InvariantCulture,
            string.IsNullOrWhiteSpace(text) ? UiText.StatusCellCleared : UiText.StatusCellUpdated,
            cell.Address);
    }

    partial void OnSelectedCellChanged(CellViewModel? value) => RefreshFormulaBar();

    partial void OnModeChanged(DisplayMode value)
    {
        foreach (RowViewModel row in Rows)
        {
            foreach (CellViewModel cell in row.Cells)
            {
                cell.SetMode(value);
            }
        }

        StatusMessage = value == DisplayMode.Value ? UiText.StatusModeValue : UiText.StatusModeExpression;
    }

    // ---------- Рядок формул, редагування клітинки ----------

    private bool HasSelection() => SelectedCell is not null;

    [RelayCommand]
    private void CommitCellEdit(CellEdit? edit)
    {
        if (edit is not null)
        {
            CommitCell(edit.Cell, edit.Text);
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void CommitFormula()
    {
        if (SelectedCell is null)
        {
            StatusMessage = UiText.StatusSelectCell;
            return;
        }

        CommitCell(SelectedCell, FormulaText);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void CancelFormulaEdit() => RefreshFormulaBar();

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ClearCell()
    {
        if (SelectedCell is null)
        {
            StatusMessage = UiText.StatusSelectCell;
            return;
        }

        CommitCell(SelectedCell, null);
    }

    [RelayCommand]
    private void ToggleMode() => IsValueMode = !IsValueMode;

    // ---------- Файл: Створити, Відкрити, Зберегти, Зберегти як ----------

    [RelayCommand]
    private async Task NewAsync()
    {
        if (IsDirty)
        {
            SaveChangesChoice choice = await _dialogs.AskSaveChangesAsync(DocumentName);
            if (choice == SaveChangesChoice.Cancel)
            {
                return;
            }

            if (choice == SaveChangesChoice.Save)
            {
                bool saved = await SaveInternalAsync();
                if (!saved)
                {
                    return;
                }
            }
        }

        LoadWorksheet(new Worksheet(DefaultRows, DefaultColumns));
        CurrentLocation = null;
        IsDirty = false;
        StatusMessage = UiText.StatusNewCreated;
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        if (IsDirty)
        {
            SaveChangesChoice choice = await _dialogs.AskSaveChangesAsync(DocumentName);
            if (choice == SaveChangesChoice.Cancel)
            {
                return;
            }

            if (choice == SaveChangesChoice.Save)
            {
                bool saved = await SaveInternalAsync();
                if (!saved)
                {
                    return;
                }
            }
        }

        string? path = await _dialogs.PickOpenFileAsync();
        if (string.IsNullOrEmpty(path))
        {
            StatusMessage = UiText.StatusOpenCancelled;
            return;
        }

        try
        {
            Worksheet sheet = await _storage.LoadAsync(path);
            LoadWorksheet(sheet);
            CurrentLocation = new StorageLocation(path, Path.GetFileName(path));
            IsDirty = false;
            StatusMessage = string.Format(CultureInfo.InvariantCulture, UiText.StatusFileLoaded, DocumentName);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync(ex.Message);
        }
    }

    [RelayCommand]
    private async Task OpenFromDriveAsync()
    {
        if (IsDirty)
        {
            SaveChangesChoice choice = await _dialogs.AskSaveChangesAsync(DocumentName);
            if (choice == SaveChangesChoice.Cancel)
            {
                return;
            }

            if (choice == SaveChangesChoice.Save)
            {
                bool saved = await SaveInternalAsync();
                if (!saved)
                {
                    return;
                }
            }
        }

        StorageLocation? picked = await _dialogs.PickDriveFileAsync();
        if (picked is null)
        {
            StatusMessage = UiText.StatusOpenCancelled;
            return;
        }

        try
        {
            Worksheet sheet = await _storage.LoadAsync(picked.Location);
            LoadWorksheet(sheet);
            CurrentLocation = picked;
            IsDirty = false;
            StatusMessage = string.Format(CultureInfo.InvariantCulture, UiText.StatusFileLoaded, DocumentName);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync(ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await SaveInternalAsync();
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        await SaveAsInternalAsync();
    }

    [RelayCommand]
    private async Task SaveToDriveAsync()
    {
        string? fileName = await _dialogs.AskDriveFileNameAsync(DocumentName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            StatusMessage = UiText.StatusSaveCancelled;
            return;
        }

        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".json";
        }

        long versionBeforeSave = _documentVersion;
        try
        {
            string createdLocation = await _storage.SaveAsync(
                _worksheet,
                $"{GoogleDriveStorage.NewFilePrefix}{fileName}");

            CurrentLocation = new StorageLocation(createdLocation, fileName);
            if (_documentVersion == versionBeforeSave)
            {
                IsDirty = false;
            }

            StatusMessage = string.Format(CultureInfo.InvariantCulture, UiText.StatusFileSaved, DocumentName);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync(ex.Message);
        }
    }

    /// <summary>
    /// Перевіряє, чи можна закрити таблицю/вікно (запитує про незбережені зміни).
    /// Повертає <c>true</c>, якщо дію дозволено, або <c>false</c>, якщо скасовано.
    /// </summary>
    public async Task<bool> TryCloseAsync()
    {
        if (!IsDirty)
        {
            return true;
        }

        SaveChangesChoice choice = await _dialogs.AskSaveChangesAsync(DocumentName);
        switch (choice)
        {
            case SaveChangesChoice.Save:
                return await SaveInternalAsync();
            case SaveChangesChoice.Discard:
                return true;
            case SaveChangesChoice.Cancel:
            default:
                return false;
        }
    }

    private async Task<bool> SaveInternalAsync()
    {
        if (CurrentLocation is null)
        {
            return await SaveAsInternalAsync();
        }

        long versionBeforeSave = _documentVersion;
        try
        {
            string finalLoc = await _storage.SaveAsync(_worksheet, CurrentLocation.Location);
            if (!string.Equals(finalLoc, CurrentLocation.Location, StringComparison.Ordinal))
            {
                CurrentLocation = new StorageLocation(finalLoc, CurrentLocation.DisplayName);
            }

            if (_documentVersion == versionBeforeSave)
            {
                IsDirty = false;
            }

            StatusMessage = string.Format(CultureInfo.InvariantCulture, UiText.StatusFileSaved, DocumentName);
            return true;
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync(ex.Message);
            return false;
        }
    }

    private async Task<bool> SaveAsInternalAsync()
    {
        string? path = await _dialogs.PickSaveFileAsync(DocumentName);
        if (string.IsNullOrEmpty(path))
        {
            StatusMessage = UiText.StatusSaveCancelled;
            return false;
        }

        long versionBeforeSave = _documentVersion;
        try
        {
            string finalLoc = await _storage.SaveAsync(_worksheet, path);
            CurrentLocation = new StorageLocation(finalLoc, Path.GetFileName(finalLoc));
            if (_documentVersion == versionBeforeSave)
            {
                IsDirty = false;
            }

            StatusMessage = string.Format(CultureInfo.InvariantCulture, UiText.StatusFileSaved, DocumentName);
            return true;
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync(ex.Message);
            return false;
        }
    }

    // ---------- Таблиця ----------

    private bool CanInsertRowAbove() => SelectedCell is not null && RowCount < Worksheet.MaxRows;

    private bool CanInsertRowBelow() => RowCount < Worksheet.MaxRows;

    private bool CanDeleteRow() => SelectedCell is not null && RowCount > 1;

    private bool CanInsertColumnLeft() => SelectedCell is not null && ColumnCount < Worksheet.MaxColumns;

    private bool CanInsertColumnRight() => ColumnCount < Worksheet.MaxColumns;

    private bool CanDeleteColumn() => SelectedCell is not null && ColumnCount > 1;

    [RelayCommand(CanExecute = nameof(CanInsertRowAbove))]
    private void InsertRowAbove()
    {
        if (SelectedCell is null)
        {
            StatusMessage = UiText.StatusSelectCell;
            return;
        }

        InsertRow(SelectedCell.Address.Row);
    }

    [RelayCommand(CanExecute = nameof(CanInsertRowBelow))]
    private void InsertRowBelow() => InsertRow(SelectedCell is null ? RowCount + 1 : SelectedCell.Address.Row + 1);

    [RelayCommand(CanExecute = nameof(CanDeleteRow))]
    private async Task DeleteRowAsync()
    {
        if (SelectedCell is null)
        {
            StatusMessage = UiText.StatusSelectCell;
            return;
        }

        int index = SelectedCell.Address.Row;
        if (RowCount <= 1)
        {
            StatusMessage = UiText.StatusLastRow;
            return;
        }

        bool hasData = _worksheet.GetNonEmptyAddresses().Any(a => a.Row == index);
        string question = string.Format(CultureInfo.InvariantCulture, UiText.ConfirmDeleteRow, index);
        if (hasData && !await _dialogs.ConfirmAsync(question))
        {
            return;
        }

        ApplyStructuralChange(
            () => _worksheet.RemoveRow(index),
            string.Format(CultureInfo.InvariantCulture, UiText.StatusRowDeleted, index),
            UiText.StatusRowFailed);
    }

    [RelayCommand(CanExecute = nameof(CanInsertColumnLeft))]
    private void InsertColumnLeft()
    {
        if (SelectedCell is null)
        {
            StatusMessage = UiText.StatusSelectCell;
            return;
        }

        InsertColumn(SelectedCell.Address.Column);
    }

    [RelayCommand(CanExecute = nameof(CanInsertColumnRight))]
    private void InsertColumnRight() =>
        InsertColumn(SelectedCell is null ? ColumnCount + 1 : SelectedCell.Address.Column + 1);

    [RelayCommand(CanExecute = nameof(CanDeleteColumn))]
    private async Task DeleteColumnAsync()
    {
        if (SelectedCell is null)
        {
            StatusMessage = UiText.StatusSelectCell;
            return;
        }

        int index = SelectedCell.Address.Column;
        if (ColumnCount <= 1)
        {
            StatusMessage = UiText.StatusLastColumn;
            return;
        }

        string name = ColumnHeaders[index - 1];
        bool hasData = _worksheet.GetNonEmptyAddresses().Any(a => a.Column == index);
        string question = string.Format(CultureInfo.InvariantCulture, UiText.ConfirmDeleteColumn, name);
        if (hasData && !await _dialogs.ConfirmAsync(question))
        {
            return;
        }

        ApplyStructuralChange(
            () => _worksheet.RemoveColumn(index),
            string.Format(CultureInfo.InvariantCulture, UiText.StatusColumnDeleted, name),
            UiText.StatusColumnFailed);
    }

    // ---------- Діалоги ----------

    [RelayCommand]
    private Task ShowHelpAsync() => _dialogs.ShowHelpAsync();

    [RelayCommand]
    private Task ShowAboutAsync() => _dialogs.ShowAboutAsync();

    // ---------- Допоміжні методи ----------

    private void InsertRow(int index)
    {
        string failure = RowCount >= Worksheet.MaxRows
            ? string.Format(CultureInfo.InvariantCulture, UiText.StatusMaxRows, Worksheet.MaxRows)
            : UiText.StatusRowFailed;
        ApplyStructuralChange(
            () => _worksheet.AddRow(index),
            string.Format(CultureInfo.InvariantCulture, UiText.StatusRowInserted, index),
            failure);
    }

    private void InsertColumn(int index)
    {
        string failure = ColumnCount >= Worksheet.MaxColumns
            ? string.Format(CultureInfo.InvariantCulture, UiText.StatusMaxColumns, Worksheet.MaxColumns)
            : UiText.StatusColumnFailed;
        ApplyStructuralChange(
            () => _worksheet.AddColumn(index),
            string.Format(CultureInfo.InvariantCulture, UiText.StatusColumnInserted, ColumnName(index)),
            failure);
    }

    /// <summary>Виконує зміну розміру; за успіху перебудовує сітку й зберігає виділення за адресою.</summary>
    private void ApplyStructuralChange(Func<bool> change, string successMessage, string failureMessage)
    {
        CellAddress? selected = SelectedCell?.Address;
        if (!change())
        {
            StatusMessage = failureMessage;
            return;
        }

        _documentVersion++;
        IsDirty = true;
        Rebuild();
        if (selected is { } address)
        {
            var clipped = new CellAddress(Math.Min(address.Row, RowCount), Math.Min(address.Column, ColumnCount));
            SelectedCell = CellAt(clipped);
        }

        StatusMessage = successMessage;
    }

    /// <summary>Повністю перебудовує сітку, заголовки й розміри за поточною таблицею.</summary>
    private void Rebuild()
    {
        int rowCount = _worksheet.RowCount;
        int columnCount = _worksheet.ColumnCount;

        var rows = new RowViewModel[rowCount];
        for (int row = 1; row <= rowCount; row++)
        {
            var cells = new CellViewModel[columnCount];
            for (int column = 1; column <= columnCount; column++)
            {
                var address = new CellAddress(row, column);
                cells[column - 1] = new CellViewModel(address, _worksheet.GetSnapshot(address), Mode);
            }

            rows[row - 1] = new RowViewModel(row, cells);
        }

        var headers = new string[columnCount];
        for (int column = 1; column <= columnCount; column++)
        {
            headers[column - 1] = ColumnName(column);
        }

        Rows = rows;
        ColumnHeaders = headers;
        RowCount = rowCount;
        ColumnCount = columnCount;
    }

    private void RefreshFormulaBar()
    {
        CellViewModel? cell = SelectedCell;
        SelectedAddressText = cell?.Address.ToString() ?? string.Empty;
        FormulaText = cell?.RawText ?? string.Empty;
        FormulaErrorText = cell?.ErrorMessage ?? string.Empty;
    }

    private CellViewModel CellAt(CellAddress address) => Rows[address.Row - 1].Cells[address.Column - 1];

    /// <summary>Назва стовпця за номером: адреса в рядку 1 без кінцевої цифри.</summary>
    private static string ColumnName(int column)
    {
        string address = new CellAddress(1, column).ToString();
        return address[..^1];
    }
}
