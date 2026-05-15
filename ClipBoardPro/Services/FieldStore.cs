using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using ClipBoardPro.Models;

namespace ClipBoardPro.Services;

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
        var directory = Path.Combine(appData, "ClipBoard Pro");
        Directory.CreateDirectory(directory);
        _storagePath = Path.Combine(directory, "fields.json");

        var oldStoragePath = Path.Combine(appData, "JobFillHelper", "fields.json");
        if (!File.Exists(_storagePath) && File.Exists(oldStoragePath))
        {
            File.Copy(oldStoragePath, _storagePath);
        }
    }

    public ObservableCollection<FillField> Load()
    {
        if (!File.Exists(_storagePath))
        {
            return new ObservableCollection<FillField>();
        }

        try
        {
            var json = File.ReadAllText(_storagePath);
            var fields = JsonSerializer.Deserialize<List<FillField>>(json, JsonOptions) ?? [];
            var validFields = fields
                .Where(field => !string.IsNullOrWhiteSpace(field.Value))
                .Select(field => new FillField
                {
                    Label = field.Label.Trim(),
                    Value = field.Value.Trim()
                });

            return new ObservableCollection<FillField>(validFields);
        }
        catch
        {
            return new ObservableCollection<FillField>();
        }
    }

    public void Save(IEnumerable<FillField> fields)
    {
        var cleaned = fields
            .Where(field => !string.IsNullOrWhiteSpace(field.Value))
            .Select(field => new FillField
            {
                Label = field.Label.Trim(),
                Value = field.Value.Trim()
            })
            .ToList();

        var json = JsonSerializer.Serialize(cleaned, JsonOptions);
        File.WriteAllText(_storagePath, json);
    }
}

