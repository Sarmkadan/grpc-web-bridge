# CsvFormatterExtensions

Provides extension methods for converting objects to CSV format and for manipulating CSV files asynchronously. The methods are intended to be used with the `CsvFormatter` type in the `grpc-web-bridge` project to simplify common CSV operations such as streaming data to memory, appending to files, merging multiple CSV files, and splitting large CSV files into smaller chunks.

## API

### AppendToFileAsync<T>
**Purpose**  
Asynchronously appends a sequence of objects of type `T` to a CSV file.

**Parameters**  
- `items`: An `IEnumerable<T>` containing the objects to serialize and append.  
- `filePath`: The path of the file to which the CSV data will be appended.  
- `cancellationToken` (optional): A `System.Threading.CancellationToken` to observe while waiting for the operation to complete.

**Return Value**  
A `Task` that completes when the append operation finishes.

**Exceptions**  
- `ArgumentNullException` if `items` or `filePath` is `null`.  
- `IOException` if an I/O error occurs while opening or writing to the file.  
- `UnauthorizedAccessException` if the caller lacks permission to access the file.  
- `OperationCanceledException` if the operation is canceled via `cancellationToken`.

### ToCsvStream
**Purpose**  
Converts a collection of objects to CSV format and returns the result as a memory‑resident stream.

**Parameters**  
- `data`: An `IEnumerable<object>` representing the rows to serialize. Each object's public properties are used as CSV columns.

**Return Value**  
A `MemoryStream` containing the UTF‑8 encoded CSV data. The stream's position is left at the beginning for reading.

**Exceptions**  
- `ArgumentNullException` if `data` is `null`.  
- `InvalidOperationException` if an object in `data` has a property that cannot be serialized to CSV.

### ToCsvStream<T>
**Purpose**  
Converts a strongly‑typed sequence of objects to CSV format and returns the result as a memory‑resident stream.

**Parameters**  
- `items`: An `IEnumerable<T>` containing the objects to serialize.

**Return Value**  
A `MemoryStream` containing the UTF‑8 encoded CSV data. The stream's position is left at the beginning for reading.

**Exceptions**  
- `ArgumentNullException` if `items` is `null`.  
- `InvalidOperationException` if a property of `T` cannot be serialized to CSV.

### MergeFilesAsync
**Purpose**  
Asynchronously merges multiple CSV files into a single CSV file.

**Parameters**  
- `sourceFiles`: An `IEnumerable<string>` of file paths to merge, in the order they should appear in the output.  
- `destinationFile`: The path of the file that will contain the merged CSV data.  
- `cancellationToken` (optional): A `System.Threading.CancellationToken` to observe while waiting for the operation to complete.

**Return Value**  
A `Task` that completes when the merge operation finishes.

**Exceptions**  
- `ArgumentNullException` if `sourceFiles` or `destinationFile` is `null`.  
- `ArgumentException` if `sourceFiles` contains a duplicate or an empty path.  
- `FileNotFoundException` if any file in `sourceFiles` does not exist.  
- `IOException` if an I/O error occurs while reading source files or writing the destination file.  
- `UnauthorizedAccessException` if the caller lacks permission to access any of the involved files.  
- `OperationCanceledException` if the operation is canceled via `cancellationToken`.

### SplitFileAsync<T>
**Purpose**  
Asynchronously splits a CSV file into multiple files, each containing up to `rowsPerFile` rows, mapping each CSV row to an instance of type `T`.

**Parameters**  
- `formatter`: The `CsvFormatter` instance on which the extension method is invoked.  
- `inputFilePath`: The path of the CSV file to split.  
- `outputDirectory`: The directory where the split files will be created.  
- `rowsPerFile`: The maximum number of data rows (excluding header) to place in each output file. Must be greater than zero.

**Return Value**  
A `Task` that completes when the split operation finishes.

**Exceptions**  
- `ArgumentNullException` if `formatter`, `inputFilePath`, or `outputDirectory` is `null`.  
- `ArgumentException` if `rowsPerFile` is less than or equal to zero.  
- `FileNotFoundException` if the file at `inputFilePath` does not exist.  
- `DirectoryNotFoundException` if `outputDirectory` does not exist and cannot be created.  
- `IOException` if an I/O error occurs while reading the input file or writing any of the output files.  
- `UnauthorizedAccessException` if the caller lacks permission to read the input file or write to the output directory.  
- `InvalidOperationException` if the CSV file cannot be mapped to type `T` (e.g., missing required properties).  

## Usage

```csharp
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GrpcWebBridge.Csv; // Hypothetical namespace

public async Task GenerateCsvReportAsync()
{
    var records = new List<SalesRecord>
    {
        new SalesRecord { Id = 1, Product = "Widget", Quantity = 10 },
        new SalesRecord { Id = 2, Product = "Gadget", Quantity = 5 }
    };

    // Convert a list of objects to a CSV stream in memory
    using MemoryStream csvStream = CsvFormatterExtensions.ToCsvStream<SalesRecord>(records);
    // csvStream can now be sent over HTTP, saved, etc.
}
```

```csharp
using System.Threading.Tasks;
using GrpcWebBridge.Csv; // Hypothetical namespace

public async Task SplitLargeLogFileAsync()
{
    var formatter = new CsvFormatter(); // Assume a parameter‑less constructor exists
    string inputPath = @"C:\Logs\combined.csv";
    string outputDir = @"C:\Logs\Split";
    int rowsPerFile = 5000;

    // Split the large CSV into smaller files, each with up to 5000 rows
    await formatter.SplitFileAsync<LogEntry>(inputPath, outputDir, rowsPerFile);
}
```

## Notes
- All static methods (`AppendToFileAsync<T>`, `ToCsvStream`, `ToCsvStream<T>`, `MergeFilesAsync`) do not rely on mutable shared state and are therefore thread‑safe when called concurrently with distinct arguments.  
- The extension method `SplitFileAsync<T>` operates on the supplied `CsvFormatter` instance; if that instance holds mutable state, concurrent calls on the same instance may not be thread‑safe.  
- Methods that accept file paths will create directories if they do not exist (except for `SplitFileAsync<T>`, which expects the output directory to exist or be creatable).  
- Passing an empty enumerable to the `ToCsv*` overloads results in a CSV stream containing only a header line (if the implementation writes headers) or an empty stream, depending on the internal logic.  
- Encoding used for the CSV streams is UTF‑8 without a BOM; consumers should interpret the stream accordingly.  
- Exception messages are intended to help diagnose issues such as missing files, insufficient permissions, or malformed data; applications should catch the specific exception types relevant to their error‑handling strategy.
