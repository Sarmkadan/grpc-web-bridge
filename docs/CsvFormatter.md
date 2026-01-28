# CsvFormatter
The `CsvFormatter` class is designed to handle the conversion of data between CSV (Comma Separated Values) format and .NET objects. It provides methods for exporting data to CSV files, importing data from CSV files, and validating the structure of CSV data. This class is useful for applications that need to interact with CSV data, such as importing or exporting data from spreadsheets or other systems that use CSV format.

## API
### Constructors
* `public CsvFormatter`: Initializes a new instance of the `CsvFormatter` class.

### Methods
* `public string ToCsv<T>(...)`: Converts a collection of objects of type `T` to a CSV string. The method takes a generic type `T` and returns a string representing the CSV data. It throws an exception if the conversion fails.
* `public List<Dictionary<string, string>> FromCsv(string csv)`: Converts a CSV string to a list of dictionaries, where each dictionary represents a row in the CSV data. The method takes a CSV string as input and returns a list of dictionaries. It throws an exception if the conversion fails.
* `public List<T> FromCsv<T>(string csv) where T : class, new`: Converts a CSV string to a list of objects of type `T`. The method takes a CSV string as input and returns a list of objects of type `T`. It throws an exception if the conversion fails.
* `public async Task ExportToFileAsync<T>(...)`: Exports a collection of objects of type `T` to a CSV file asynchronously. The method takes a generic type `T` and returns a task that represents the export operation. It throws an exception if the export fails.
* `public async Task<List<Dictionary<string, string>>> ImportFromFileAsync(string filePath)`: Imports a CSV file to a list of dictionaries asynchronously. The method takes a file path as input and returns a task that represents the import operation. It throws an exception if the import fails.
* `public async Task<List<T>> ImportFromFileAsync<T>(string filePath) where T : class, new`: Imports a CSV file to a list of objects of type `T` asynchronously. The method takes a file path as input and returns a task that represents the import operation. It throws an exception if the import fails.
* `public string DictsToCsv(List<Dictionary<string, string>> dicts)`: Converts a list of dictionaries to a CSV string.
* `public (bool Valid, List<string> Errors) ValidateCsv(string csv)`: Validates the structure of a CSV string and returns a tuple containing a boolean indicating whether the CSV is valid and a list of error messages.

### Properties
* `public string Delimiter`: Gets or sets the delimiter character used in the CSV data.
* `public Encoding Encoding`: Gets or sets the encoding used when reading or writing CSV files.
* `public bool IncludeHeaders`: Gets or sets a value indicating whether to include headers in the CSV data.
* `public bool TrimWhitespace`: Gets or sets a value indicating whether to trim whitespace from the CSV data.

## Usage
The following example demonstrates how to use the `CsvFormatter` class to export a list of objects to a CSV file:
```csharp
var formatter = new CsvFormatter();
var data = new List<MyObject>
{
    new MyObject { Name = "John", Age = 30 },
    new MyObject { Name = "Jane", Age = 25 }
};

await formatter.ExportToFileAsync(data, "output.csv");
```
The following example demonstrates how to use the `CsvFormatter` class to import a CSV file to a list of objects:
```csharp
var formatter = new CsvFormatter();
var filePath = "input.csv";

var data = await formatter.ImportFromFileAsync<MyObject>(filePath);

foreach (var obj in data)
{
    Console.WriteLine($"Name: {obj.Name}, Age: {obj.Age}");
}
```

## Notes
* The `CsvFormatter` class is not thread-safe. If you need to use it in a multi-threaded environment, you should create a new instance of the class for each thread.
* The `FromCsv` and `ToCsv` methods assume that the CSV data is well-formed and does not contain any errors. If the CSV data is malformed, these methods may throw exceptions or produce incorrect results.
* The `ValidateCsv` method can be used to validate the structure of a CSV string before attempting to import or export it.
* The `Delimiter`, `Encoding`, `IncludeHeaders`, and `TrimWhitespace` properties can be used to customize the behavior of the `CsvFormatter` class. For example, you can change the delimiter character or encoding used when reading or writing CSV files.
