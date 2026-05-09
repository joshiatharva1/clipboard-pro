using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;
using JobFillHelper.Models;
using JobFillHelper.Services;

namespace JobFillHelper;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly FieldStore _fieldStore = new();
    private readonly WindowPasteService _pasteService = new();
    private bool _isAlwaysOnTop = true;
    private bool _isEditMode;
    private HwndSource? _hwndSource;
    private string _statusText = "Paste mode keeps the browser field active. Turn on Edit to change saved values.";

    public MainWindow()
    {
        InitializeComponent();
        Fields = _fieldStore.Load();
        DataContext = this;
        SourceInitialized += (_, _) =>
        {
            _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            _hwndSource?.AddHook(WindowProcedure);
        };
        Loaded += (_, _) => _pasteService.Start(this);
        Closing += (_, _) =>
        {
            _fieldStore.Save(Fields);
            _pasteService.Dispose();
            _hwndSource?.RemoveHook(WindowProcedure);
        };
    }

    public ObservableCollection<FillField> Fields { get; }

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set
        {
            if (_isAlwaysOnTop == value)
            {
                return;
            }

            _isAlwaysOnTop = value;
            Topmost = value;
            OnPropertyChanged();
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode == value)
            {
                return;
            }

            _isEditMode = value;
            StatusText = value
                ? "Edit mode is on. Update values, then save."
                : "Paste mode keeps the browser field active.";
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPasteMode));
        }
    }

    public bool IsPasteMode => !IsEditMode;

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private async void PasteField_Click(object sender, RoutedEventArgs e)
    {
        if (GetField(sender) is not { } field)
        {
            return;
        }

        var result = await _pasteService.PasteTextAsync(field.Value);
        StatusText = $"{field.Label}: {result}";
    }

    private void CopyField_Click(object sender, RoutedEventArgs e)
    {
        if (GetField(sender) is not { } field)
        {
            return;
        }

        _pasteService.CopyText(field.Value);
        StatusText = $"Copied {field.Label}.";
    }

    private void RemoveField_Click(object sender, RoutedEventArgs e)
    {
        if (GetField(sender) is { } field)
        {
            Fields.Remove(field);
            StatusText = "Field removed.";
        }
    }

    private void AddField_Click(object sender, RoutedEventArgs e)
    {
        Fields.Add(new FillField { Label = "New field", Value = "" });
        StatusText = "New field added.";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _fieldStore.Save(Fields);
        StatusText = "Saved locally.";
    }

    private void SaveAndClose_Click(object sender, RoutedEventArgs e)
    {
        _fieldStore.Save(Fields);
        Close();
    }

    private static FillField? GetField(object sender)
    {
        return (sender as FrameworkElement)?.DataContext as FillField;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == NativeMethods.WmMouseActivate && IsPasteMode)
        {
            handled = true;
            return new IntPtr(NativeMethods.MaNoActivate);
        }

        return IntPtr.Zero;
    }

    private static class NativeMethods
    {
        public const int WmMouseActivate = 0x0021;
        public const int MaNoActivate = 3;
    }
}
