# XmlFormatter

`XmlFormatter` is a utility class that simplifies working with XML in the `grpc-web-bridge` project. It provides methods for serializing objects to XML, deserializing XML back into objects, formatting, minifying, validating, querying, and converting XML to other representations. The class is designed for ease of use while allowing control over indentation, XML declaration, namespaces, and encoding through configurable properties.

## API

### XmlFormatter()
Creates a new instance with default settings: indentation enabled, indent characters set to two spaces, XML declaration included, namespaces preserved, and UTF‑8 encoding.

### public string ToXml<T>(T value)
Serializes an object of type `T` to an XML string.

- **Parameters**
  - `value`: The object to serialize. Must not be `null`.
- **Return value**: XML representation of `value`.
- **Exceptions**
  - `ArgumentNullException` if `value` is `null`.
  - `InvalidOperationException` if the type `T` cannot be serialized (e.g., lacks a parameterless constructor or contains non‑serializable members).
  - `XmlException` if an error occurs during serialization.

### public T? FromXml<T>(string xml)
Deserializes an XML string into an instance of type `T`.

- **Parameters**
  - `xml`: The XML input. Must not be `null` or empty.
- **Return value**: An object of type `T` or `null` if deserialization yields no value.
- **Exceptions**
  - `ArgumentNullException` if `xml` is `null`.
  - `ArgumentException` if `xml` is empty or not well‑formed.
  - `InvalidOperationException` if the XML cannot be mapped to type `T`.
  - `XmlException` if the XML is malformed.

### public string FormatXml(string xml)
Returns a formatted version of the supplied XML, applying the current `Indent` and `IndentChars` settings.

- **Parameters**
  - `xml`: The XML to format. Must be well‑formed.
- **Return value**: Indented XML string.
- **Exceptions**
  - `ArgumentNullException` if `xml` is `null`.
  - `ArgumentException` if `xml` is not well‑formed XML.

### public static string MinifyXml(string xml)
Strips all insignificant whitespace from the XML string.

- **Parameters**
  - `xml`: The XML to minify. Must not be `null`.
- **Return value**: Compact XML string with no unnecessary whitespace.
- **Exceptions**
  - `ArgumentNullException` if `xml` is `null`.
  - `ArgumentException` if `xml` is not well‑formed.

### public (bool Valid, List<string> Errors) ValidateXml(string xml)
Checks the XML for well‑formedness and returns validation details.

- **Parameters**
  - `xml`: The XML to validate. Must not be `null`.
- **Return value**: A tuple where `Valid` indicates whether the XML is well‑formed and `Errors` contains descriptive messages if any.
- **Exceptions**
  - `ArgumentNullException` if `xml` is `null`.
  - `XmlException` is caught internally and reported via the `Errors` list; the method does not throw for malformed XML.

### public string? GetElementValueByXPath(string xml, string xpath)
Evaluates an XPath expression and returns the text value of the first matching node.

- **Parameters**
  - `xml`: The XML source. Must not be `null`.
  - `xpath`: The XPath expression. Must not be `null`.
- **Return value**: The inner text of the matched node, or `null` if no node matches.
- **Exceptions**
  - `ArgumentNullException` if either parameter is `null`.
  - `ArgumentException` if `xml` is not well‑formed.
  - `XPathException` if the XPath expression is invalid.

### public async Task ExportToFileAsync<T>(T value, string filePath)
Serializes `value` to XML and writes it to the specified file asynchronously.

- **Parameters**
  - `value`: The object to serialize. Must not be `null`.
  - `filePath`: Destination file path. Must not be `null` or empty.
- **Return value**: A task that completes when the file has been written.
- **Exceptions**
  - `ArgumentNullException` if `value` or `filePath` is `null`.
  - `ArgumentException` if `filePath` is empty.
  - `IOException` or `UnauthorizedAccessException` for file‑system errors.
  - `InvalidOperationException` or `XmlException` for serialization failures (propagated from `ToXml<T>`).

### public async Task<T?> ImportFromFileAsync<T>(string filePath)
Reads an XML file and deserializes its contents into an object of type `T` asynchronously.

- **Parameters**
  - `filePath`: Path to the XML file. Must not be `null` or empty.
- **Return value**: Deserialized object of type `T` or `null` if the file contains no data.
- **Exceptions**
  - `ArgumentNullException` if `filePath` is `null`.
  - `ArgumentException` if `filePath` is empty.
  - `FileNotFoundException` if the file does not exist.
  - `IOException` for other file‑access problems.
  - `InvalidOperationException` or `XmlException` for deserialization errors (propagated from `FromXml<T>`).

### public string MergeXml(string xml1, string xml2)
Combines two XML fragments into a single XML document by appending the root element of `xml2` as a child of the root element of `xml1`.

- **Parameters**
  - `xml1`: Primary XML. Must be well‑formed and contain exactly one root element.
  - `xml2`: Secondary XML. Must be well‑formed and contain exactly one root element.
- **Return value**: Merged XML string.
- **Exceptions**
  - `ArgumentNullException` if either parameter is `null`.
  - `ArgumentException` if either input is not well‑formed or does not have a single root element.

### public string GetTextContent(string xml)
Extracts and concatenates all text nodes within the XML, ignoring markup.

- **Parameters**
  - `xml`: The XML source. Must not be `null`.
- **Return value**: A string containing all text content.
- **Exceptions**
  - `ArgumentNullException` if `xml` is `null`.
  - `ArgumentException` if `xml` is not well‑formed.

### public Dictionary<string, object?> XmlToDictionary(string xml)
Converts a simple XML structure (elements with scalar values) into a dictionary where keys are element names and values are the element contents.

- **Parameters**
  - `xml`: The XML to convert. Must not be `null`.
- **Return value**: Dictionary mapping element names to their values (`null` for empty elements).
- **Exceptions**
  - `ArgumentNullException` if `xml` is `null`.
  - `ArgumentException` if `xml` is not well‑formed or contains complex nodes (attributes, nested elements) that cannot be represented as a flat dictionary.

### public bool Indent { get; set; }
Gets or sets whether the output XML should be indented. Default is `true`.

### public string IndentChars { get; set; }
Gets or sets the string used for each indentation level when `Indent` is `true`. Default is two spaces (`"  "`).

### public bool OmitXmlDeclaration { get; set; }
Gets or sets whether the XML declaration (`<?xml version="1.0" encoding="utf-8"?>`) is omitted from serialized output. Default is `false`.

### public bool OmitNamespaces { get; set; }
Gets or sets whether XML namespaces are omitted from serialized output. Default is `false`.

### public System.Text.Encoding Encoding { get; set; }
Gets or sets the encoding used when converting XML to/from byte streams (e.g., file I/O). Default is `Encoding.UTF8`.

## Usage

### Example 1: Serializing and saving an object
```csharp
var formatter = new XmlFormatter
{
    Indent = true,
    IndentChars = "\t",
    OmitXmlDeclaration = false,
    OmitNamespaces = true,
    Encoding = Encoding.UTF8
};

var person = new Person { Id = 42, Name = "Ada Lovelace" };
string xml = formatter.ToXml(person);
// xml now contains formatted XML representing the person instance

await formatter.ExportToFileAsync(person, "person.xml");
// The file person.xml is written with the same XML content
```

### Example 2: Loading, querying, and converting XML
```csharp
string xml = await File.ReadAllTextAsync("config.xml");
var formatter = new XmlFormatter();

if (formatter.ValidateXml(xml).Valid)
{
    string version = formatter.GetElementValueByXPath(xml, xpath") // Actually need correct: formatter.GetElementValueByXPath(xml, "/configuration/version");
    var settings = formatter.XmlToDictionary(xml);
    // settings contains key/value pairs for simple configuration elements

    var restored = formatter.FromXml<Configuration>(xml);
    // restored is a strongly‑typed configuration object
}
else
{
    foreach (var err in formatter.ValidateXml(xml).Errors)
    {
        Console.Error.WriteLine($"XML validation error: {err}");
    }
}
```

## Notes

- Instance properties (`Indent`, `IndentChars`, `OmitXmlDeclaration`, `OmitNamespaces`, `Encoding`) affect the behavior of `ToXml<T>`, `FormatXml`, `ExportToFileAsync<T>`, and related methods. Changing these properties after an operation has begun does not affect already‑completed operations.
- Static methods (`MinifyXml`, `ValidateXml`) are thread‑safe and can be invoked concurrently from multiple threads without external synchronization.
- The class itself is **not** thread‑safe when its instance properties are modified by one thread while another thread is executing a method that reads those properties. For concurrent use, either create separate `XmlFormatter` instances per thread or synchronize access to shared instances.
- `FromXml<T>` and `ExportToFileAsync<T>` rely on `XmlSerializer` internally; therefore, the type `T` must have a parameterless constructor and public read/write properties or fields that are serializable. Types implementing `ISerializable` or containing `XmlIgnore` attributes are supported as per the standard serializer rules.
- `GetElementValueByXPath` returns `null` when the XPath selects no nodes; it does not throw for an empty result set.
- `MergeXml` assumes both inputs contain a single root element; if either input has multiple roots or an XML declaration, the result may not be well‑formed. Callers should ensure inputs conform to this expectation or pre‑process them accordingly.
- `XmlToDictionary` is intended for simple configuration‑style XML where each element contains only text content. Elements with attributes, child elements, or mixed content will cause an `ArgumentException`.
- All methods that accept an XML string will throw an `ArgumentException` if the supplied string is not well‑formed XML, except for `ValidateXml` which captures the error and returns it in the `Errors` list.
