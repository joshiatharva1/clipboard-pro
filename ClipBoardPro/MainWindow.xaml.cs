using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using JobFillHelper.Models;
using JobFillHelper.Services;

namespace JobFillHelper;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly FieldStore _fieldStore = new();
    private readonly WindowPasteService _pasteService = new();
    private bool _isAlwaysOnTop = true;
    private HwndSource? _hwndSource;
    private FillField? _pressedField;
    private System.Windows.Point _dragStartPoint;
    private bool _suppressClickPaste;
    private bool _canDropOnCurrentTarget;
    private string _statusText = "Click a saved value to paste it.";

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

    public bool IsPasteMode => Fields.All(field => !field.IsEditing);

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

    private void FieldCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<System.Windows.Controls.Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        if (GetField(sender) is not { } field || field.IsEditing)
        {
            return;
        }

        _pressedField = field;
        _dragStartPoint = e.GetPosition(this);
    }

    private async void FieldCard_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_suppressClickPaste)
        {
            _suppressClickPaste = false;
            _pressedField = null;
            return;
        }

        if (FindAncestor<System.Windows.Controls.Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        if (GetField(sender) is not { } field || field.IsEditing || _pressedField != field)
        {
            return;
        }

        _pressedField = null;
        e.Handled = true;
        var result = await _pasteService.PasteTextAsync(field.Value);
        StatusText = result;
    }

    private void FieldCard_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _pressedField is null || _pressedField.IsEditing)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        var movedFarEnough = Math.Abs(currentPosition.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                             Math.Abs(currentPosition.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance;

        if (!movedFarEnough)
        {
            return;
        }

        _suppressClickPaste = true;
        _canDropOnCurrentTarget = false;
        DragDrop.DoDragDrop((DependencyObject)sender, _pressedField, System.Windows.DragDropEffects.Move);
        _canDropOnCurrentTarget = false;
        _pressedField = null;
    }

    private void FieldCard_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        _canDropOnCurrentTarget = CanDropOnField(sender, e);
        e.Effects = _canDropOnCurrentTarget ? System.Windows.DragDropEffects.Move : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void FieldCard_GiveFeedback(object sender, System.Windows.GiveFeedbackEventArgs e)
    {
        Mouse.SetCursor(_canDropOnCurrentTarget ? System.Windows.Input.Cursors.SizeAll : System.Windows.Input.Cursors.No);
        e.UseDefaultCursors = false;
        e.Handled = true;
    }

    private bool CanDropOnField(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(FillField)) || GetField(sender) is not { } targetField)
        {
            return false;
        }

        var sourceField = (FillField)e.Data.GetData(typeof(FillField))!;
        return sourceField != targetField && !sourceField.IsEditing && !targetField.IsEditing;
    }

    private void FieldCard_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(FillField)) || GetField(sender) is not { } targetField)
        {
            return;
        }

        var sourceField = (FillField)e.Data.GetData(typeof(FillField))!;
        if (sourceField == targetField || sourceField.IsEditing || targetField.IsEditing)
        {
            return;
        }

        var oldIndex = Fields.IndexOf(sourceField);
        var newIndex = Fields.IndexOf(targetField);
        if (oldIndex < 0 || newIndex < 0 || oldIndex == newIndex)
        {
            return;
        }

        Fields.Move(oldIndex, newIndex);
        _fieldStore.Save(Fields);
        StatusText = "Order updated.";
        e.Handled = true;
    }

    private void AddField_Click(object sender, RoutedEventArgs e)
    {
        RemoveBlankFields();
        StopEditingAllFields();
        Fields.Add(new FillField { Value = "", IsEditing = true });
        OnPropertyChanged(nameof(IsPasteMode));
        StatusText = "New field ready to edit.";
    }

    private void ToggleEditField_Click(object sender, RoutedEventArgs e)
    {
        if (GetField(sender) is not { } field)
        {
            return;
        }

        if (field.IsEditing)
        {
            if (string.IsNullOrWhiteSpace(field.Value))
            {
                StatusText = "Enter a value before saving.";
                FocusFieldTextBox(sender as DependencyObject);
                return;
            }

            field.Value = field.Value.Trim();
            field.IsEditing = false;
            _fieldStore.Save(Fields);
            StatusText = "Saved.";
        }
        else
        {
            StopEditingAllFields();
            field.IsEditing = true;
            StatusText = "Editing field.";
            FocusFieldTextBox(sender as DependencyObject);
        }

        OnPropertyChanged(nameof(IsPasteMode));
    }

    private void DeleteField_Click(object sender, RoutedEventArgs e)
    {
        if (GetField(sender) is not { } field)
        {
            return;
        }

        Fields.Remove(field);
        _fieldStore.Save(Fields);
        OnPropertyChanged(nameof(IsPasteMode));
        StatusText = "Field deleted.";
    }

    private void FocusFieldTextBox(DependencyObject? source)
    {
        Dispatcher.BeginInvoke(() =>
        {
            Activate();

            var row = FindAncestor<Border>(source);
            var textBox = FindVisualChild<System.Windows.Controls.TextBox>(row);
            if (textBox is null)
            {
                return;
            }

            textBox.Focus();
            Keyboard.Focus(textBox);
            textBox.CaretIndex = textBox.Text.Length;
        });
    }

    private void StopEditingAllFields()
    {
        foreach (var field in Fields)
        {
            field.IsEditing = false;
        }
    }

    private void RemoveBlankFields()
    {
        for (var index = Fields.Count - 1; index >= 0; index--)
        {
            if (string.IsNullOrWhiteSpace(Fields[index].Value))
            {
                Fields.RemoveAt(index);
            }
        }
    }

    private static FillField? GetField(object sender)
    {
        return (sender as FrameworkElement)?.DataContext as FillField;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject? current) where T : DependencyObject
    {
        if (current is null)
        {
            return null;
        }

        for (var index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(current); index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(current, index);
            if (child is T match)
            {
                return match;
            }

            var nestedMatch = FindVisualChild<T>(child);
            if (nestedMatch is not null)
            {
                return nestedMatch;
            }
        }

        return null;
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