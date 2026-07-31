using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GrpcWebBridge.Formatters;
using Xunit;

namespace GrpcWebBridge.Tests
{
    public class XmlFormatterExtensionsTests
    {
        private readonly XmlFormatter _formatter = new XmlFormatter();

        [Fact]
        public void WithOptions_ReturnsNewFormatterWithSpecifiedOptions()
        {
            var options = new XmlFormatterOptions { Indent = false };
            var result = _formatter.WithOptions(options);
            Assert.NotNull(result);
        }

        [Fact]
        public void WithIndent_ReturnsNewFormatterWithIndentationApplied()
        {
            var result = _formatter.WithIndent(true, "  ");
            Assert.NotNull(result);
        }

        [Fact]
        public void WithOmittedNamespaces_ReturnsNewFormatter()
        {
            var result = _formatter.WithOmittedNamespaces(true);
            Assert.NotNull(result);
        }

        [Fact]
        public void ToXDocument_ValidXml_ReturnsXDocument()
        {
            var xml = "<root><child>value</child></root>";
            var result = _formatter.ToXDocument(xml);
            Assert.NotNull(result);
            Assert.Equal("root", result!.Root?.Name.LocalName);
        }

        [Fact]
        public void ToXElement_ValidXml_ReturnsXElement()
        {
            var xml = "<root><child>value</child></root>";
            var result = _formatter.ToXElement(xml);
            Assert.NotNull(result);
            Assert.Equal("root", result!.Name.LocalName);
        }

        [Theory]
        [InlineData("<root />", true)]
        [InlineData("invalid", false)]
        public void IsValidXml_ValidatesCorrectly(string xml, bool expected)
        {
            if (expected)
                Assert.True(_formatter.IsValidXml(xml));
            else
                Assert.False(_formatter.IsValidXml(xml));
        }

        [Fact]
        public void GetRootElementName_ValidXml_ReturnsRootName()
        {
            var xml = "<myRoot>content</myRoot>";
            var result = _formatter.GetRootElementName(xml);
            Assert.Equal("myRoot", result);
        }

        [Fact]
        public void CountElementsByXPath_ValidXPath_ReturnsCount()
        {
            var xml = "<root><item>1</item><item>2</item></root>";
            var count = _formatter.CountElementsByXPath(xml, "//item");
            Assert.Equal(2, count);
        }

        [Fact]
        public void GetElementValuesByXPath_ValidXPath_ReturnsValues()
        {
            var xml = "<root><item>1</item><item>2</item></root>";
            var values = _formatter.GetElementValuesByXPath(xml, "//item");
            Assert.Equal(new List<string> { "1", "2" }, values);
        }
    }
}
