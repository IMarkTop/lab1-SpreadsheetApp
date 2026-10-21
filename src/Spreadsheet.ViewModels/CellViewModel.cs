using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Spreadsheet.Core.Evaluation;
using Spreadsheet.Core.Model;
using Spreadsheet.ViewModels.Text;

namespace Spreadsheet.ViewModels;

/// <summary>Подання клітинки: адреса, вихідний текст, відображуваний текст і помилка.</summary>
public sealed class CellViewModel : ObservableObject
{
    private CellSnapshot _snapshot;
    private DisplayMode _mode;
    private string _rawText = string.Empty;
    private string _displayText = string.Empty;
    private bool _isEmpty = true;
    private bool _isError;
    private string _errorMessage = string.Empty;

    /// <summary>Створює подання клітинки за знімком і режимом.</summary>
    public CellViewModel(CellAddress address, CellSnapshot snapshot, DisplayMode mode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Address = address;
        _snapshot = snapshot;
        _mode = mode;
        Apply();
    }

    /// <summary>Адреса клітинки.</summary>
    public CellAddress Address { get; }

    /// <summary>Вихідний текст.</summary>
    public string RawText
    {
        get => _rawText;
        private set => SetProperty(ref _rawText, value);
    }

    /// <summary>Текст у клітинці таблиці з урахуванням режиму.</summary>
    public string DisplayText
    {
        get => _displayText;
        private set => SetProperty(ref _displayText, value);
    }

    /// <summary>Чи клітинка порожня.</summary>
    public bool IsEmpty
    {
        get => _isEmpty;
        private set => SetProperty(ref _isEmpty, value);
    }

    /// <summary>Чи клітинка містить помилку.</summary>
    public bool IsError
    {
        get => _isError;
        private set => SetProperty(ref _isError, value);
    }

    /// <summary>Повне повідомлення про помилку (порожнє, якщо помилки немає).</summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    internal void Update(CellSnapshot snapshot, DisplayMode mode)
    {
        _snapshot = snapshot;
        _mode = mode;
        Apply();
    }

    internal void SetMode(DisplayMode mode)
    {
        _mode = mode;
        Apply();
    }

    private void Apply()
    {
        EvalResult? result = _snapshot.Result;
        bool error = result is { IsSuccess: false };

        RawText = _snapshot.RawText;
        IsEmpty = _snapshot.IsEmpty;
        IsError = error;
        ErrorMessage = error ? ErrorTextFormatter.Format(result!.Error!, _snapshot.RawText) : string.Empty;
        DisplayText = FormatDisplay(_snapshot, _mode);
    }

    private static string FormatDisplay(CellSnapshot snapshot, DisplayMode mode)
    {
        if (snapshot.IsEmpty)
        {
            return string.Empty;
        }

        if (mode == DisplayMode.Expression || snapshot.Result is null)
        {
            return snapshot.RawText;
        }

        if (!snapshot.Result.IsSuccess)
        {
            return UiText.ErrorValue;
        }

        Value value = snapshot.Result.Value;
        return value.Kind == ValueKind.Number
            ? value.Number.ToString(CultureInfo.InvariantCulture)
            : value.Boolean ? UiText.TrueText : UiText.FalseText;
    }
}
