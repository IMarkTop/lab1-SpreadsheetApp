using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Spreadsheet.ViewModels;

namespace Spreadsheet.App.Behaviors;

/// <summary>
/// Синхронізує виділену клітинку між DataGrid та ViewModel (двостороннє зв'язування),
/// а також обробляє клавішу Delete для очищення клітинки поза режимом редагування.
/// </summary>
public static class DataGridSelectedCellBehavior
{
    private static readonly AttachedProperty<SelectionTracker?> TrackerProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, SelectionTracker?>(
            "Tracker",
            typeof(DataGridSelectedCellBehavior));

    public static readonly AttachedProperty<CellViewModel?> SelectedCellProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, CellViewModel?>(
            "SelectedCell",
            typeof(DataGridSelectedCellBehavior),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    static DataGridSelectedCellBehavior()
    {
        SelectedCellProperty.Changed.AddClassHandler<DataGrid>(OnSelectedCellPropertyChanged);
        Control.LoadedEvent.AddClassHandler<DataGrid>((grid, _) => EnsureTracker(grid));
        DataGrid.SelectionChangedEvent.AddClassHandler<DataGrid>((grid, _) => EnsureTracker(grid));
    }

    public static CellViewModel? GetSelectedCell(DataGrid element) => element.GetValue(SelectedCellProperty);

    public static void SetSelectedCell(DataGrid element, CellViewModel? value)
    {
        EnsureTracker(element);
        element.SetValue(SelectedCellProperty, value);
    }

    private static SelectionTracker EnsureTracker(DataGrid grid)
    {
        SelectionTracker? tracker = grid.GetValue(TrackerProperty);
        if (tracker is null)
        {
            tracker = new SelectionTracker(grid);
            grid.SetValue(TrackerProperty, tracker);
        }
        return tracker;
    }

    private static void OnSelectedCellPropertyChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs args)
    {
        SelectionTracker tracker = EnsureTracker(grid);
        tracker.OnViewModelCellChanged(args.NewValue as CellViewModel);
    }

    private sealed class SelectionTracker
    {
        private readonly DataGrid _grid;
        private bool _isSyncing;

        public SelectionTracker(DataGrid grid)
        {
            _grid = grid;
            _grid.CellPointerPressed += OnCellPointerPressed;
            _grid.CurrentCellChanged += OnCurrentCellChanged;
            _grid.SelectionChanged += OnSelectionChanged;
            _grid.KeyUp += OnGridKeyUp;
            _grid.AddHandler(InputElement.KeyDownEvent, OnGridKeyDown, RoutingStrategies.Bubble);
        }

        private void OnGridKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
            {
                return;
            }

            if (e.Handled)
            {
                return;
            }

            if (IsFromTextBox(e.Source))
            {
                // Delete надійшла з TextBox — не перешкоджаємо редагуванню тексту
                return;
            }

            // Поза режимом редагування: очищаємо виділену клітинку та позначаємо подію як оброблену
            var cell = _grid.GetValue(SelectedCellProperty);
            if (cell is not null && _grid.DataContext is MainViewModel vm && vm.ClearCellCommand.CanExecute(null))
            {
                vm.ClearCellCommand.Execute(null);
                e.Handled = true;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
        {
            var row = e.Row?.DataContext as RowViewModel;
            var col = e.Column;

            if (row is not null && col is not null)
            {
                int colIndex = col.Tag is int tag ? tag : _grid.Columns.IndexOf(col);
                if (colIndex >= 0 && colIndex < row.Cells.Count)
                {
                    CellViewModel cell = row.Cells[colIndex];
                    UpdateSelectedCell(cell);
                }
            }
        }

        private void OnCurrentCellChanged(object? sender, EventArgs e) => SyncGridToViewModel();

        private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => SyncGridToViewModel();

        private void OnGridKeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right or Key.Tab or Key.Enter)
            {
                if (IsFromTextBox(e.Source))
                {
                    return;
                }

                SyncGridToViewModel();
            }
        }

        private static bool IsFromTextBox(object? source)
        {
            if (source is null)
            {
                return false;
            }

            if (source is TextBox)
            {
                return true;
            }

            if (source is Visual visual)
            {
                return visual.FindAncestorOfType<TextBox>() is not null;
            }

            return false;
        }

        private void SyncGridToViewModel()
        {
            if (_isSyncing)
            {
                return;
            }

            var row = _grid.SelectedItem as RowViewModel;
            var col = _grid.CurrentColumn;

            if (row is not null && col is not null)
            {
                int colIndex = col.Tag is int tag ? tag : _grid.Columns.IndexOf(col);
                if (colIndex >= 0 && colIndex < row.Cells.Count)
                {
                    CellViewModel cell = row.Cells[colIndex];
                    UpdateSelectedCell(cell);
                }
            }
        }

        private void UpdateSelectedCell(CellViewModel? cell)
        {
            if (_isSyncing)
            {
                return;
            }

            CellViewModel? current = _grid.GetValue(SelectedCellProperty);
            if (ReferenceEquals(current, cell))
            {
                return;
            }

            _isSyncing = true;
            try
            {
                _grid.SetValue(SelectedCellProperty, cell);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        public void OnViewModelCellChanged(CellViewModel? cell)
        {
            if (_isSyncing)
            {
                return;
            }

            _isSyncing = true;
            try
            {
                if (cell is null)
                {
                    _grid.SelectedItem = null;
                    return;
                }

                if (_grid.ItemsSource is IEnumerable<RowViewModel> rows)
                {
                    RowViewModel? targetRow = null;
                    foreach (RowViewModel r in rows)
                    {
                        if (r.RowNumber == cell.Address.Row)
                        {
                            targetRow = r;
                            break;
                        }
                    }

                    DataGridColumn? targetCol = null;
                    int targetColIndex = cell.Address.Column - 1;
                    foreach (DataGridColumn c in _grid.Columns)
                    {
                        if (c.Tag is int idx && idx == targetColIndex)
                        {
                            targetCol = c;
                            break;
                        }
                    }

                    if (targetRow is not null)
                    {
                        _grid.SelectedItem = targetRow;
                    }

                    if (targetCol is not null)
                    {
                        _grid.CurrentColumn = targetCol;
                    }

                    if (targetRow is not null && targetCol is not null)
                    {
                        _grid.ScrollIntoView(targetRow, targetCol);
                    }
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}
