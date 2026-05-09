using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JobFillHelper.Models;

public sealed class FillField : INotifyPropertyChanged
{
    private string _label = "";
    private string _value = "";

    public string Label
    {
        get => _label;
        set => SetField(ref _label, value);
    }

    public string Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
