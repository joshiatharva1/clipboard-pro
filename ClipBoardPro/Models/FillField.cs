using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace JobFillHelper.Models;

public sealed class FillField : INotifyPropertyChanged
{
    private string _label = "";
    private string _value = "";
    private bool _isEditing;

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

    [JsonIgnore]
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetField(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(IsReadOnly));
            }
        }
    }

    [JsonIgnore]
    public bool IsReadOnly => !IsEditing;

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}