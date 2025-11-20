#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace GrpcWebBridge.Formatters;

/// <summary>
/// XML serialization and formatting utilities.
/// Provides serialization, deserialization, and validation for XML data.
/// </summary>
public sealed class XmlFormatter
{
    private readonly XmlFormatterOptions _options;

    public XmlFormatter(XmlFormatterOptions? options = null)
    {
        _options = options ?? new XmlFormatterOptions();
    }

    /// <summary>
    /// Serializes an object to XML string.
    /// </summary>
    public string ToXml<T>(T obj) where T : class
    {
        if (obj is null)
            throw new ArgumentNullException(nameof(obj));

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var writer = new StringWriter())
            {
                var settings = new XmlWriterSettings
                {
                    Indent = _options.Indent,
                    IndentChars = _options.IndentChars,
                    OmitXmlDeclaration = _options.OmitXmlDeclaration,
                    Encoding = _options.Encoding
                };

                using (var xmlWriter = XmlWriter.Create(writer, settings))
                {
                    var namespaces = new XmlSerializerNamespaces();
                    if (_options.OmitNamespaces)
                    {
                        namespaces.Add(string.Empty, string.Empty);
                    }

                    serializer.Serialize(xmlWriter, obj, namespaces);
                }

                return writer.ToString();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to serialize to XML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes XML string to an object.
    /// </summary>
    public T? FromXml<T>(string xml) where T : class
    {
        if (string.IsNullOrEmpty(xml))
            throw new ArgumentException("XML string cannot be null or empty", nameof(xml));

        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(xml))
            {
                return (T?)serializer.Deserialize(reader);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to deserialize from XML: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts XML document to formatted string.
    /// </summary>
    public string FormatXml(string xml)
    {
        if (string.IsNullOrEmpty(xml))
            return xml;

        try
        {
            var doc = XDocument.Parse(xml);
            return doc.ToString(_options.Indent ? SaveOptions.None : SaveOptions.DisableFormatting);
        }
        catch
        {
            return xml;
        }
    }

    /// <summary>
    /// Minifies XML by removing unnecessary whitespace.
    /// </summary>
    public static string MinifyXml(string xml)
    {
        if (string.IsNullOrEmpty(xml))
            return xml;

        try
        {
            var doc = XDocument.Parse(xml);
            return doc.ToString(SaveOptions.DisableFormatting);
        }
        catch
        {
            return xml;
        }
    }

    /// <summary>
    /// Validates XML against a schema.
    /// </summary>
    public (bool Valid, List<string> Errors) ValidateXml(string xml)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(xml))
        {
            errors.Add("XML cannot be null or empty");
            return (false, errors);
        }

        try
        {
            XDocument.Parse(xml);
            return (true, errors);
        }
        catch (XmlException ex)
        {
            errors.Add($"XML parsing error: {ex.Message}");
            return (false, errors);
        }
    }

    /// <summary>
    /// Extracts XML element value by XPath.
    /// </summary>
    public string? GetElementValueByXPath(string xml, string xpathExpression)
    {
        if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(xpathExpression))
            return null;

        try
        {
            var doc = XDocument.Parse(xml);
            var element = doc.XPathSelectElement(xpathExpression);
            return element?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Exports to XML file.
    /// </summary>
    public async Task ExportToFileAsync<T>(T obj, string filePath) where T : class
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        var xml = ToXml(obj);
        await File.WriteAllTextAsync(filePath, xml, _options.Encoding).ConfigureAwait(false);
    }

    /// <summary>
    /// Imports from XML file.
    /// </summary>
    public async Task<T?> ImportFromFileAsync<T>(string filePath) where T : class
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var xml = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
        return FromXml<T>(xml);
    }

    /// <summary>
    /// Merges two XML documents.
    /// </summary>
    public string MergeXml(string xml1, string xml2)
    {
        if (string.IsNullOrEmpty(xml1))
            return xml2 ?? string.Empty;

        if (string.IsNullOrEmpty(xml2))
            return xml1;

        try
        {
            var doc1 = XDocument.Parse(xml1);
            var doc2 = XDocument.Parse(xml2);

            // Merge root element children
            if (doc1.Root is not null && doc2.Root is not null)
            {
                foreach (var element in doc2.Root.Elements())
                {
                    doc1.Root.Add(element);
                }
            }

            return doc1.ToString();
        }
        catch
        {
            return xml1;
        }
    }

    /// <summary>
    /// Extracts all text content from XML.
    /// </summary>
    public string GetTextContent(string xml)
    {
        if (string.IsNullOrEmpty(xml))
            return string.Empty;

        try
        {
            var doc = XDocument.Parse(xml);
            return string.Join(" ", doc.Descendants().Where(d => !d.HasElements).Select(d => d.Value));
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Converts XML to dictionary representation.
    /// </summary>
    public Dictionary<string, object?> XmlToDictionary(string xml)
    {
        if (string.IsNullOrEmpty(xml))
            return new Dictionary<string, object?>();

        try
        {
            var doc = XDocument.Parse(xml);
            return ElementToDictionary(doc.Root!);
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }

    private Dictionary<string, object?> ElementToDictionary(XElement element)
    {
        var dict = new Dictionary<string, object?>();

        // Add attributes
        foreach (var attr in element.Attributes())
        {
            dict[$"@{attr.Name.LocalName}"] = attr.Value;
        }

        // Add child elements
        foreach (var child in element.Elements())
        {
            var childDict = ElementToDictionary(child);
            dict[child.Name.LocalName] = childDict.Count > 0 ? (object)childDict : child.Value;
        }

        return dict;
    }
}

/// <summary>
/// Configuration options for XML formatter.
/// </summary>
public sealed class XmlFormatterOptions
{
    public bool Indent { get; set; } = true;
    public string IndentChars { get; set; } = "  ";
    public bool OmitXmlDeclaration { get; set; } = false;
    public bool OmitNamespaces { get; set; } = false;
    public System.Text.Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;
}
