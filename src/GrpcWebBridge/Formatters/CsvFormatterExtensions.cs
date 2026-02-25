#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text;

namespace GrpcWebBridge.Formatters;

/// <summary>
/// Extension methods for <see cref="CsvFormatter"/> providing additional CSV processing capabilities.
/// </summary>
public static class CsvFormatterExtensions
{
    /// <summary>
    /// Appends CSV data to an existing file.
    /// </summary>
    /// <typeparam name="T">The type of objects to append.</typeparam>
    /// <param name="formatter">The CSV formatter instance.</param>
    /// <param name="items">The collection of items to append.</param>
    /// <param name="filePath">Path to the CSV file.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is <see langword="null"/>, empty, or whitespace.</exception>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
    /// <exception cref="DirectoryNotFoundException">The specified path is invalid (for example, it is on an unmapped drive).</exception>
    public static async Task AppendToFileAsync<T>(this CsvFormatter formatter, IEnumerable<T> items, string filePath) where T : class
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var csv = formatter.ToCsv(items);
        await File.AppendAllTextAsync(filePath, csv, Encoding.UTF8).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts a collection of dictionaries to CSV and returns as a stream.
    /// Useful for HTTP responses or streaming scenarios.
    /// </summary>
    /// <param name="formatter">The CSV formatter instance.</param>
    /// <param name="data">The dictionary collection to convert.</param>
    /// <returns>MemoryStream containing the CSV data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">An I/O error occurs during stream creation.</exception>
    public static MemoryStream ToCsvStream(this CsvFormatter formatter, IEnumerable<Dictionary<string, string>> data)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentNullException.ThrowIfNull(data);

        var csv = formatter.DictsToCsv(data);
        return new MemoryStream(Encoding.UTF8.GetBytes(csv));
    }

    /// <summary>
    /// Converts a collection of objects to CSV and returns as a stream.
    /// Useful for HTTP responses or streaming scenarios.
    /// </summary>
    /// <typeparam name="T">The type of objects to convert.</typeparam>
    /// <param name="formatter">The CSV formatter instance.</param>
    /// <param name="items">The collection of items to convert.</param>
    /// <returns>MemoryStream containing the CSV data.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">An I/O error occurs during stream creation.</exception>
    public static MemoryStream ToCsvStream<T>(this CsvFormatter formatter, IEnumerable<T> items) where T : class
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentNullException.ThrowIfNull(items);

        var csv = formatter.ToCsv(items);
        return new MemoryStream(Encoding.UTF8.GetBytes(csv));
    }

    /// <summary>
    /// Merges multiple CSV files into a single output file.
    /// </summary>
    /// <param name="formatter">The CSV formatter instance.</param>
    /// <param name="outputFilePath">Path to the output CSV file.</param>
    /// <param name="inputFilePaths">Paths to input CSV files to merge.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="outputFilePath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="outputFilePath"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputFilePaths"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputFilePaths"/> contains a null or empty string.</exception>
    /// <exception cref="FileNotFoundException">An input file does not exist.</exception>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
    public static async Task MergeFilesAsync(this CsvFormatter formatter, string outputFilePath, params string[] inputFilePaths)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (string.IsNullOrWhiteSpace(outputFilePath))
            throw new ArgumentException("Output file path cannot be null or empty.", nameof(outputFilePath));

        ArgumentNullException.ThrowIfNull(inputFilePaths);

        if (inputFilePaths.Length == 0)
            throw new ArgumentException("At least one input file path is required.", nameof(inputFilePaths));

        if (inputFilePaths.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Input file paths cannot be null or empty.", nameof(inputFilePaths));

        var allLines = new List<string>();

        // Process each input file
        foreach (var inputPath in inputFilePaths)
        {
            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"Input file not found: {inputPath}");

            var csv = await File.ReadAllTextAsync(inputPath, Encoding.UTF8).ConfigureAwait(false);
            var lines = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            if (allLines.Count == 0)
            {
                // First file - include all lines
                allLines.AddRange(lines);
            }
            else
            {
                // Subsequent files - skip header
                for (int i = 1; i < lines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                        allLines.Add(lines[i]);
                }
            }
        }

        // Write merged content
        await File.WriteAllLinesAsync(outputFilePath, allLines, Encoding.UTF8).ConfigureAwait(false);
    }

    /// <summary>
    /// Splits a large CSV file into multiple smaller files based on row count.
    /// </summary>
    /// <typeparam name="T">The type of objects in the CSV.</typeparam>
    /// <param name="formatter">The CSV formatter instance.</param>
    /// <param name="inputFilePath">Path to the input CSV file.</param>
    /// <param name="outputDirectory">Directory to save split files.</param>
    /// <param name="rowsPerFile">Number of rows per output file.</param>
    /// <returns>Task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="inputFilePath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="outputDirectory"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputFilePath"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="outputDirectory"/> is empty or whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="rowsPerFile"/> is not positive.</exception>
    /// <exception cref="FileNotFoundException">The input file does not exist.</exception>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
    public static async Task SplitFileAsync<T>(this CsvFormatter formatter, string inputFilePath, string outputDirectory, int rowsPerFile) where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (string.IsNullOrWhiteSpace(inputFilePath))
            throw new ArgumentException("Input file path cannot be null or empty.", nameof(inputFilePath));

        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        if (rowsPerFile <= 0)
            throw new ArgumentException("Rows per file must be positive.", nameof(rowsPerFile));

        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        // Ensure output directory exists
        Directory.CreateDirectory(outputDirectory);

        var csv = await File.ReadAllTextAsync(inputFilePath, Encoding.UTF8).ConfigureAwait(false);
        var lines = csv.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (lines.Length <= 1)
            return; // Only header or empty

        var headers = lines[0];
        var dataLines = lines.Skip(1).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();

        int fileCount = 0;
        for (int i = 0; i < dataLines.Count; i += rowsPerFile)
        {
            var batch = dataLines.Skip(i).Take(rowsPerFile);
            var outputPath = Path.Combine(outputDirectory, $"part_{fileCount}.csv");

            var outputLines = new List<string> { headers };
            outputLines.AddRange(batch);

            await File.WriteAllLinesAsync(outputPath, outputLines, Encoding.UTF8).ConfigureAwait(false);
            fileCount++;
        }
    }
}