#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

using System.Diagnostics.CodeAnalysis;

namespace GrpcWebBridge.Formatters;

/// <summary>
/// Extension methods for <see cref="XmlFormatter"/> providing additional XML processing capabilities.
/// </summary>
public static class XmlFormatterExtensions
{
    /// <summary>
    /// Creates a deep copy of the <see cref="XmlFormatter"/> with new options.
    /// </summary>
    /// <param name="formatter">The source formatter.</param>
    /// <param name="options">New options to apply.</param>
    /// <returns>A new <see cref="XmlFormatter"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    public static XmlFormatter WithOptions(this XmlFormatter formatter, XmlFormatterOptions options)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentNullException.ThrowIfNull(options);

        return new XmlFormatter(options);
    }

    /// <summary>
    /// Creates a deep copy of the <see cref="XmlFormatter"/> with new options.
    /// </summary>
    /// <param name="formatter">The source formatter.</param>
    /// <param name="indent">Whether to indent the XML output.</param>
    /// <param name="indentChars">Characters to use for indentation. Defaults to a single space.</param>
    /// <param name="omitXmlDeclaration">Whether to omit XML declaration.</param>
    /// <returns>A new <see cref="XmlFormatter"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is <see langword="null"/>.</exception>
    public static XmlFormatter WithIndent(this XmlFormatter formatter, bool indent, string indentChars = " ", bool omitXmlDeclaration = false)
    {
        ArgumentNullException.ThrowIfNull(formatter);

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
    /// Creates a deep copy of the <see cref="XmlFormatter"/> with modified namespace handling.
    /// </summary>
    /// <param name="formatter">The source formatter.</param>
    /// <param name="omitNamespaces">Whether to omit XML namespaces.</param>
    /// <returns>A new <see cref="XmlFormatter"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is <see langword="null"/>.</exception>
    public static XmlFormatter WithOmittedNamespaces(this XmlFormatter formatter, bool omitNamespaces)
    {
        ArgumentNullException.ThrowIfNull(formatter);

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
    /// Converts XML string to <see cref="XDocument"/> for LINQ-to-XML operations.
    /// </summary>
    /// <param name="formatter">The formatter instance.</param>
    /// <param name="xml">The XML string to parse.</param>
    /// <returns><see cref="XDocument"/> instance or <see langword="null"/> if parsing fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="xml"/> is <see langword="null"/> or empty.</exception>
    public static XDocument? ToXDocument(this XmlFormatter formatter, string xml)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentException.ThrowIfNullOrEmpty(xml);

        try
        {
            return XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>
    /// Converts XML string to <see cref="XElement"/> for LINQ-to-XML operations.
    /// </summary>
    /// <param name="formatter">The formatter instance.</param>
    /// <param name="xml">The XML string to parse.</param>
    /// <returns><see cref="XElement"/> instance or <see langword="null"/> if parsing fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="xml"/> is <see langword="null"/> or empty.</exception>
    public static XElement? ToXElement(this XmlFormatter formatter, string xml)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentException.ThrowIfNullOrEmpty(xml);

        try
        {
            return XElement.Parse(xml, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if XML string is valid XML without throwing exceptions.
    /// </summary>
    /// <param name="formatter">The formatter instance.</param>
    /// <param name="xml">The XML string to validate.</param>
    /// <returns>True if valid XML, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="xml"/> is <see langword="null"/> or empty.</exception>
    public static bool IsValidXml(this XmlFormatter formatter, string xml)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentException.ThrowIfNullOrEmpty(xml);

        try
        {
            XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            return true;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the root element name from XML string.
    /// </summary>
    /// <param name="formatter">The formatter instance.</param>
    /// <param name="xml">The XML string.</param>
    /// <returns>Root element name or <see langword="null"/> if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="xml"/> is <see langword="null"/> or empty.</exception>
    public static string? GetRootElementName(this XmlFormatter formatter, string xml)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentException.ThrowIfNullOrEmpty(xml);

        try
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            return doc.Root?.Name.LocalName;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    /// <summary>
    /// Counts the number of elements matching the XPath expression.
    /// </summary>
    /// <param name="formatter">The formatter instance.</param>
    /// <param name="xml">The XML string to search.</param>
    /// <param name="xpathExpression">XPath expression to match elements.</param>
    /// <returns>Number of matching elements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="xml"/> or <paramref name="xpathExpression"/> is <see langword="null"/> or empty.</exception>
    public static int CountElementsByXPath(this XmlFormatter formatter, string xml, string xpathExpression)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentException.ThrowIfNullOrEmpty(xml);
        ArgumentException.ThrowIfNullOrEmpty(xpathExpression);

        try
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            var elements = doc.XPathSelectElements(xpathExpression);
            return elements.Count();
        }
        catch (XmlException)
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets all element values matching the XPath expression as a list.
    /// </summary>
    /// <param name="formatter">The formatter instance.</param>
    /// <param name="xml">The XML string to search.</param>
    /// <param name="xpathExpression">XPath expression to match elements.</param>
    /// <returns>List of matching element values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="xml"/> or <paramref name="xpathExpression"/> is <see langword="null"/> or empty.</exception>
    public static List<string> GetElementValuesByXPath(this XmlFormatter formatter, string xml, string xpathExpression)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        ArgumentException.ThrowIfNullOrEmpty(xml);
        ArgumentException.ThrowIfNullOrEmpty(xpathExpression);

        var result = new List<string>();

        try
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            var elements = doc.XPathSelectElements(xpathExpression);
            result.AddRange(elements.Select(e => e.Value));
        }
        catch (XmlException)
        {
            // Return empty list on error
        }

        return result;
    }
}