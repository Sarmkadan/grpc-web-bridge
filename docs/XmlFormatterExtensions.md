# XmlFormatterExtensions

Provides extension methods for formatting and querying XML content using `XmlFormatter` and LINQ to XML (`XDocument`, `XElement`). Designed to simplify XML serialization, validation, and XPath-based traversal in .NET applications.

## API

### `public static XmlFormatter WithOptions(XmlWriterSettings settings)`

Configures an `XmlFormatter` with custom `XmlWriterSettings`. This allows control over XML output behavior such as encoding, standalone declaration, and output method.

- **Parameters**
  - `settings`: An `XmlWriterSettings` instance defining formatting rules (e.g., `Indent`, `OmitXmlDeclaration`).
- **Returns**
  - A new `XmlFormatter` instance configured with the provided settings.
- **Throws**
  - `ArgumentNullException`: If `settings` is `null`.

---

### `public static XmlFormatter WithIndent(XmlFormatter formatter, bool indent = true)`

Applies or removes indentation to an existing `XmlFormatter`.

- **Parameters**
  - `formatter`: The `XmlFormatter` to modify.
  - `indent`: If `true`, enables indentation; if `false`, disables it.
- **Returns**
  - A new `XmlFormatter` with the specified indentation behavior.
- **Throws**
  - `ArgumentNullException`: If `formatter` is `null`.

---

### `public static XmlFormatter WithOmittedNamespaces(XmlFormatter formatter, bool omit = true)`

Configures whether XML namespaces are omitted in the output of an `XmlFormatter`.

- **Parameters**
  - `formatter`: The `XmlFormatter` to modify.
  - `omit`: If `true`, omits namespace declarations; if `false`, includes them.
- **Returns**
  - A new `XmlFormatter` with the specified namespace behavior.
- **Throws**
  - `ArgumentNullException`: If `formatter` is `null`.

---
### `public static XDocument? ToXDocument(object? value)`

Serializes an object into an `XDocument` using the configured `XmlFormatter`. Returns `null` if serialization fails.

- **Parameters**
  - `value`: The object to serialize.
- **Returns**
  - An `XDocument` representing the serialized XML, or `null` if serialization fails (e.g., invalid input or formatting error).
- **Throws**
  - No exceptions are thrown; returns `null` on failure.

---
### `public static XElement? ToXElement(object? value)`

Serializes an object into an `XElement` using the configured `XmlFormatter`. Returns `null` if serialization fails.

- **Parameters**
  - `value`: The object to serialize.
- **Returns**
  - An `XElement` representing the serialized XML, or `null` if serialization fails.
- **Throws**
  - No exceptions are thrown; returns `null` on failure.

---
### `public static bool IsValidXml(string? xml)`

Determines whether a string is valid XML.

- **Parameters**
  - `xml`: The XML string to validate.
- **Returns**
  - `true` if the string is valid XML; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `xml` is `null`.

---
### `public static string? GetRootElementName(string? xml)`

Extracts the name of the root element from a valid XML string.

- **Parameters**
  - `xml`: The XML string to parse.
- **Returns**
  - The name of the root element, or `null` if the XML is invalid or has no root.
- **Throws**
  - `ArgumentNullException`: If `xml` is `null`.

---
### `public static int CountElementsByXPath(string? xml, string xpath)`

Counts the number of XML elements matching a given XPath expression.

- **Parameters**
  - `xml`: The XML string to query.
  - `xpath`: The XPath expression to evaluate.
- **Returns**
  - The number of matching elements, or `-1` if the XML is invalid or evaluation fails.
- **Throws**
  - `ArgumentNullException`: If either `xml` or `xpath` is `null`.

---
### `public static List<string> GetElementValuesByXPath(string? xml, string xpath)`

Retrieves the text content of all XML elements matching a given XPath expression.

- **Parameters**
  - `xml`: The XML string to query.
  - `xpath`: The XPath expression to evaluate.
- **Returns**
  - A list of string values from matching elements. Returns an empty list if no matches are found or if the XML is invalid.
- **Throws**
  - `ArgumentNullException`: If either `xml` or `xpath` is `null`.

## Usage

### Example 1: Serializing an object to XML and validating it
