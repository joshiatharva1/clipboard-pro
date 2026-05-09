using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using JobFillHelper.Models;

namespace JobFillHelper.Services;

public sealed class FieldStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _storagePath;

    public FieldStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "JobFillHelper");
        Directory.CreateDirectory(directory);
        _storagePath = Path.Combine(directory, "fields.json");
    }

    public ObservableCollection<FillField> Load()
    {
        if (!File.Exists(_storagePath))
        {
            return DefaultFields();
        }

        try
        {
            var json = File.ReadAllText(_storagePath);
            var fields = JsonSerializer.Deserialize<List<FillField>>(json, JsonOptions);

            return fields is { Count: > 0 }
                ? new ObservableCollection<FillField>(fields)
                : DefaultFields();
        }
        catch
        {
            return DefaultFields();
        }
    }

    public void Save(IEnumerable<FillField> fields)
    {
        var cleaned = fields
            .Where(field => !string.IsNullOrWhiteSpace(field.Label) || !string.IsNullOrWhiteSpace(field.Value))
            .Select(field => new FillField
            {
                Label = field.Label.Trim(),
                Value = field.Value
            })
            .ToList();

        var json = JsonSerializer.Serialize(cleaned, JsonOptions);
        File.WriteAllText(_storagePath, json);
    }

    private static ObservableCollection<FillField> DefaultFields()
    {
        return new ObservableCollection<FillField>
        {
            new FillField { Label = "Full name", Value = "" },
            new FillField { Label = "Email", Value = "" },
            new FillField { Label = "Phone", Value = "" },
            new FillField { Label = "Address", Value = "" },
            new FillField { Label = "LinkedIn", Value = "" },
            new FillField { Label = "GitHub", Value = "" },
            new FillField { Label = "Work authorization", Value = "" }
        };
    }
}
