using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;
using Spreadsheet.ViewModels;

namespace Spreadsheet.App.Behaviors;

/// <summary>
/// Динамічно створює та оновлює стовпці DataGrid на основі списку заголовків ColumnHeaders.
/// </summary>
public static class DataGridColumnsBehavior
{
    public static readonly AttachedProperty<IReadOnlyList<string>?> HeadersProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, IReadOnlyList<string>?>(
            "Headers",
            typeof(DataGridColumnsBehavior));

    public static readonly AttachedProperty<IDataTemplate?> CellTemplateProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, IDataTemplate?>(
            "CellTemplate",
            typeof(DataGridColumnsBehavior));

    public static readonly AttachedProperty<IDataTemplate?> CellEditingTemplateProperty =
        AvaloniaProperty.RegisterAttached<DataGrid, IDataTemplate?>(
            "CellEditingTemplate",
            typeof(DataGridColumnsBehavior));

    static DataGridColumnsBehavior()
    {
        HeadersProperty.Changed.AddClassHandler<DataGrid>(OnHeadersChanged);
    }

    public static IReadOnlyList<string>? GetHeaders(DataGrid element) => element.GetValue(HeadersProperty);

    public static void SetHeaders(DataGrid element, IReadOnlyList<string>? value) => element.SetValue(HeadersProperty, value);

    public static IDataTemplate? GetCellTemplate(DataGrid element) => element.GetValue(CellTemplateProperty);

    public static void SetCellTemplate(DataGrid element, IDataTemplate? value) => element.SetValue(CellTemplateProperty, value);

    public static IDataTemplate? GetCellEditingTemplate(DataGrid element) => element.GetValue(CellEditingTemplateProperty);

    public static void SetCellEditingTemplate(DataGrid element, IDataTemplate? value) => element.SetValue(CellEditingTemplateProperty, value);

    private static void OnHeadersChanged(DataGrid grid, AvaloniaPropertyChangedEventArgs args)
    {
        var headers = args.NewValue as IReadOnlyList<string>;
        grid.Columns.Clear();

        if (headers is null || headers.Count == 0)
        {
            return;
        }

        IDataTemplate? cellTemplate = GetCellTemplate(grid);
        IDataTemplate? editingTemplate = GetCellEditingTemplate(grid);

        for (int i = 0; i < headers.Count; i++)
        {
            int columnIndex = i;
            var column = new DataGridTemplateColumn
            {
                Header = headers[i],
                Tag = columnIndex,
                Width = new DataGridLength(80),
                CanUserSort = false,
                CanUserReorder = false,
                CellTemplate = new FuncDataTemplate<object?>((_, _) =>
                {
                    var presenter = new ContentControl();
                    presenter.Bind(ContentControl.ContentProperty, new Binding($"Cells[{columnIndex}]"));
                    presenter.ContentTemplate = cellTemplate;
                    return presenter;
                }),
                CellEditingTemplate = new FuncDataTemplate<object?>((_, _) =>
                {
                    var presenter = new ContentControl();
                    presenter.Bind(ContentControl.ContentProperty, new Binding($"Cells[{columnIndex}]"));
                    presenter.ContentTemplate = editingTemplate;
                    return presenter;
                }),
            };

            grid.Columns.Add(column);
        }
    }
}
