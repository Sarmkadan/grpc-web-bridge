#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace GrpcWebBridge.Formatters;

/// <summary>
/// Extension methods for XmlFormatter providing additional XML processing capabilities.
/// </summary>
public static class XmlFormatterExtensions
{
    /// <summary>
    /// Creates a deep copy of the XmlFormatter with new options.
    /// </summary>
    /// <param name="formatter">The source formatter</param>
    /// <param name="options">New options to apply</param>
    /// <returns>A new XmlFormatter instance</returns>
    public static XmlFormatter WithOptions(this XmlFormatter formatter, XmlFormatterOptions options)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        if (options is null)
            throw new ArgumentNullException(nameof(options));

        return new XmlFormatter(options);
    }

    /// <summary>
    /// Creates a deep copy of the XmlFormatter with new options.
    /// </summary>
    /// <param name="formatter">The source formatter</param>
    /// <param name="indent">Whether to indent the XML output</param>
    /// <param name="indentChars">Characters to use for indentation</param>
    /// <param name="omitXmlDeclaration">Whether to omit XML declaration</param>
    /// <returns>A new XmlFormatter instance</returns>
    public static XmlFormatter WithIndent(this XmlFormatter formatter, bool indent, string indentChars = " ", bool omitXmlDeclaration = false)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        var options = new XmlFormatterOptions
        {
            Indent = indent,
            IndentChars = indentChars,
            OmitXmlDeclaration = omitXmlDeclaration,
            OmitNamespaces = false,
            Encoding = System.Text.Encoding.UTF8
        };

        return new XmlFormatter(options);
    }

    /// <summary>
    /// Creates a deep copy of the XmlFormatter with modified namespace handling.
    /// </summary>
    /// <param name="formatter">The source formatter</param>
    /// <param name="omitNamespaces">Whether to omit XML namespaces</param>
    /// <returns>A new XmlFormatter instance</returns>
    public static XmlFormatter WithOmittedNamespaces(this XmlFormatter formatter, bool omitNamespaces)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        // Create new formatter with default options and override namespace behavior
        var options = new XmlFormatterOptions
        {
            Indent = true,
            IndentChars = " ",
            OmitXmlDeclaration = false,
            OmitNamespaces = omitNamespaces,
            Encoding = System.Text.Encoding.UTF8
        };

        return new XmlFormatter(options);
    }

    /// <summary>
    /// Converts XML string to XDocument for LINQ-to-XML operations.
    /// </summary>
    /// <param name="formatter">The formatter instance</param>
    /// <param name="xml">The XML string to parse</param>
    /// <returns>XDocument instance or null if parsing fails</returns>
    public static XDocument? ToXDocument(this XmlFormatter formatter, string xml)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        if (string.IsNullOrEmpty(xml))
            return null;

        try
        {
            return XDocument.Parse(xml);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts XML string to XElement for LINQ-to-XML operations.
    /// </summary>
    /// <param name="formatter">The formatter instance</param>
    /// <param name="xml">The XML string to parse</param>
    /// <returns>XElement instance or null if parsing fails</returns>
    public static XElement? ToXElement(this XmlFormatter formatter, string xml)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        if (string.IsNullOrEmpty(xml))
            return null;

        try
        {
            return XElement.Parse(xml);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if XML string is valid XML without throwing exceptions.
    /// </summary>
    /// <param name="formatter">The formatter instance</param>
    /// <param name="xml">The XML string to validate</param>
    /// <returns>True if valid XML, false otherwise</returns>
    public static bool IsValidXml(this XmlFormatter formatter, string xml)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        if (string.IsNullOrEmpty(xml))
            return false;

        try
        {
            XDocument.Parse(xml);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the root element name from XML string.
    /// </summary>
    /// <param name="formatter">The formatter instance</param>
    /// <param name="xml">The XML string</param>
    /// <returns>Root element name or null if not found</returns>
    public static string? GetRootElementName(this XmlFormatter formatter, string xml)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        if (string.IsNullOrEmpty(xml))
            return null;

        try
        {
            var doc = XDocument.Parse(xml);
            return doc.Root?.Name.LocalName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Counts the number of elements matching the XPath expression.
    /// </summary>
    /// <param name="formatter">The formatter instance</param>
    /// <param name="xml">The XML string to search</param>
    /// <param name="xpathExpression">XPath expression to match elements</param>
    /// <returns>Number of matching elements</returns>
    public static int CountElementsByXPath(this XmlFormatter formatter, string xml, string xpathExpression)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(xpathExpression))
            return 0;

        try
        {
            var doc = XDocument.Parse(xml);
            var elements = doc.XPathSelectElements(xpathExpression);
            return elements.Count();
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets all element values matching the XPath expression as a list.
    /// </summary>
    /// <param name="formatter">The formatter instance</param>
    /// <param name="xml">The XML string to search</param>
    /// <param name="xpathExpression">XPath expression to match elements</param>
    /// <returns>List of matching element values</returns>
    public static List<string> GetElementValuesByXPath(this XmlFormatter formatter, string xml, string xpathExpression)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        var result = new List<string>();

        if (string.IsNullOrEmpty(xml) || string.IsNullOrEmpty(xpathExpression))
            return result;

        try
        {
            var doc = XDocument.Parse(xml);
            var elements = doc.XPathSelectElements(xpathExpression);
            result.AddRange(elements.Select(e => e.Value));
        }
        catch
        {
            // Return empty list on error
        }

        return result;
    }
}