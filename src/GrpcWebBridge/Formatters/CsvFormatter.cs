#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Formatters;

/// <summary>
/// CSV export and import utilities for tabular data.
/// Supports RFC 4180 CSV format with proper escaping and quoting.
/// </summary>
public sealed class CsvFormatter
{
    private readonly CsvFormatterOptions _options;

    public CsvFormatter(CsvFormatterOptions? options = null)
    {
        _options = options ?? new CsvFormatterOptions();
    }

    /// <summary>
    /// Converts a collection of objects to CSV format.
    /// </summary>
    public string ToCsv<T>(IEnumerable<T> items) where T : class
    {
        if (items is null || !items.Any())
            return string.Empty;

        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties();

        // Write header
        var headers = properties.Select(p => EscapeCsvField(p.Name));
        sb.AppendLine(string.Join(_options.Delimiter, headers));

        // Write data rows
        foreach (var item in items)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return EscapeCsvField(value?.ToString() ?? string.Empty);
            });

            sb.AppendLine(string.Join(_options.Delimiter, values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts CSV to a list of dictionaries.
    /// Useful for dynamic/weakly-typed data.
    /// </summary>
    public List<Dictionary<string, string>> FromCsv(string csv)
    {
        if (string.IsNullOrEmpty(csv))
            return new List<Dictionary<string, string>>();

        var lines = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (lines.Length == 0)
            return new List<Dictionary<string, string>>();

        var result = new List<Dictionary<string, string>>();
        var headers = ParseCsvLine(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var values = ParseCsvLine(lines[i]);
            var dict = new Dictionary<string, string>();

            for (int j = 0; j < headers.Count && j < values.Count; j++)
            {
                dict[headers[j]] = values[j];
            }

            result.Add(dict);
        }

        return result;
    }

    /// <summary>
    /// Converts CSV to strongly-typed objects.
    /// </summary>
    public List<T> FromCsv<T>(string csv) where T : class, new()
    {
        if (string.IsNullOrEmpty(csv))
            return new List<T>();

        var dictionaries = FromCsv(csv);
        var properties = typeof(T).GetProperties();
        var result = new List<T>();

        foreach (var dict in dictionaries)
        {
            var item = new T();

            foreach (var prop in properties)
            {
                if (dict.TryGetValue(prop.Name, out var value))
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                        prop.SetValue(item, convertedValue);
                    }
                    catch
                    {
                        // Skip properties that can't be converted
                    }
                }
            }

            result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Exports data to CSV file.
    /// </summary>
    public async Task ExportToFileAsync<T>(IEnumerable<T> items, string filePath) where T : class
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        var csv = ToCsv(items);
        await File.WriteAllTextAsync(filePath, csv, Encoding.UTF8);
    }

    /// <summary>
    /// Imports CSV data from file.
    /// </summary>
    public async Task<List<Dictionary<string, string>>> ImportFromFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var csv = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        return FromCsv(csv);
    }

    /// <summary>
    /// Imports strongly-typed data from CSV file.
    /// </summary>
    public async Task<List<T>> ImportFromFileAsync<T>(string filePath) where T : class, new()
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        var csv = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        return FromCsv<T>(csv);
    }

    /// <summary>
    /// Exports dictionary collection to CSV.
    /// </summary>
    public string DictsToCsv(IEnumerable<Dictionary<string, string>> data)
    {
        if (data is null || !data.Any())
            return string.Empty;

        var sb = new StringBuilder();
        var dictList = data.ToList();
        var headers = dictList.First().Keys.ToList();

        // Write header
        sb.AppendLine(string.Join(_options.Delimiter, headers.Select(EscapeCsvField)));

        // Write rows
        foreach (var dict in dictList)
        {
            var values = headers.Select(h =>
                EscapeCsvField(dict.TryGetValue(h, out var v) ? v : string.Empty));

            sb.AppendLine(string.Join(_options.Delimiter, values));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Validates CSV format.
    /// </summary>
    public (bool Valid, List<string> Errors) ValidateCsv(string csv)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(csv))
        {
            errors.Add("CSV cannot be null or empty");
            return (false, errors);
        }

        var lines = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (lines.Length < 2)
        {
            errors.Add("CSV must contain at least a header and one data row");
            return (false, errors);
        }

        var headerCount = ParseCsvLine(lines[0]).Count;

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var valueCount = ParseCsvLine(lines[i]).Count;
            if (valueCount != headerCount)
            {
                errors.Add($"Row {i + 1} has {valueCount} values but header has {headerCount}");
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Escapes a field value for safe CSV inclusion.
    /// </summary>
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        // Check if field needs quoting
        if (field.Contains(_options.Delimiter) || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
        {
            // Escape quotes by doubling them
            var escaped = field.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return field;
    }

    /// <summary>
    /// Parses a single CSV line respecting quotes.
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < line.Length)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    current.Append('"');
                    i += 2;
                }
                else
                {
                    // Toggle quote state
                    inQuotes = !inQuotes;
                    i++;
                }
            }
            else if (ch == _options.Delimiter[0] && !inQuotes)
            {
                // Field separator
                result.Add(current.ToString());
                current.Clear();
                i++;
            }
            else
            {
                current.Append(ch);
                i++;
            }
        }

        result.Add(current.ToString());
        return result;
    }
}

/// <summary>
/// Configuration options for CSV formatter.
/// </summary>
public sealed class CsvFormatterOptions
{
    public string Delimiter { get; set; } = ",";
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public bool IncludeHeaders { get; set; } = true;
    public bool TrimWhitespace { get; set; } = false;
}
