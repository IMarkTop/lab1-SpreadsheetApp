using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Spreadsheet.ViewModels;

namespace Spreadsheet.App.Behaviors;

/// <summary>
/// Перехоплює завершення редагування клітинки DataGrid і викликає команду зі значенням CellEdit.
/// </summary>
public static class DataGridCellEditBehavior
{
    private static readonly AttachedProperty<EditTracker?> TrackerProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, EditTracker?>(
            "Tracker",
            typeof(DataGridCellEditBehavior));

    public static readonly AttachedProperty<ICommand?> CommitCommandProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, ICommand?>(
            "CommitCommand",
            typeof(DataGridCellEditBehavior));

    static DataGridCellEditBehavior()
    {
        CommitCommandProperty.Changed.AddClassHandler<DataGrid>(OnCommitCommandChanged);
    }

    public static ICommand? GetCommitCommand(DataGrid element) => element.GetValue(CommitCommandProperty);

    public static void SetCommitCommand(DataGrid element, ICommand? value) => element.SetValue(CommitCommandProperty, value);

    private static void OnCommitCommandChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs args)
    {
        EditTracker? tracker = grid.GetValue(TrackerProperty);
        if (tracker is null)
        {
            tracker = new EditTracker(grid);
            grid.SetValue(TrackerProperty, tracker);
        }
    }

    private sealed class EditTracker
    {
        private readonly DataGrid _grid;

        public EditTracker(DataGrid grid)
        {
            _grid = grid;
            _grid.CellEditEnding += OnCellEditEnding;
            _grid.PreparingCellForEdit += OnPreparingCellForEdit;
        }

        private void OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
        {
            // Ставимо фокус у текстове поле редагування
            TextBox? tb = e.EditingElement.FindDescendantOfType<TextBox>() ?? (e.EditingElement as TextBox);
            if (tb is not null)
            {
                tb.Focus();
                tb.CaretIndex = tb.Text?.Length ?? 0;
            }
        }

        private void OnCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            ICommand? command = _grid.GetValue(CommitCommandProperty);
            if (command is null)
            {
                return;
            }

            if (e.Row.DataContext is not RowViewModel row)
            {
                return;
            }

            if (e.Column.Tag is not int columnIndex || columnIndex < 0 || columnIndex >= row.Cells.Count)
            {
                return;
            }

            CellViewModel cell = row.Cells[columnIndex];
            TextBox? tb = e.EditingElement.FindDescendantOfType<TextBox>() ?? (e.EditingElement as TextBox);
            string? text = tb?.Text;

            var edit = new CellEdit(cell, text);
            if (command.CanExecute(edit))
            {
                command.Execute(edit);
            }
        }
    }
}
