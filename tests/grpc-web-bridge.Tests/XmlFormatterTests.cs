#nullable enable

using FluentAssertions;
using GrpcWebBridge.Formatters;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace GrpcWebBridge.Tests;

public class TestModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public sealed class XmlFormatterTests
{
    private readonly XmlFormatter _formatter;

    public XmlFormatterTests()
    {
        _formatter = new XmlFormatter();
    }

    [Fact]
    public void ToXml_WithValidObject_ReturnsXmlString()
    {
        // Arrange
        var testObject = new TestModel { Id = 1, Name = "Test" };

        // Act
        var xml = _formatter.ToXml(testObject);

        // Assert
        xml.Should().NotBeNullOrEmpty();
        xml.Should().Contain("<Id>1</Id>");
        xml.Should().Contain("<Name>Test</Name>");
    }

    [Fact]
    public void ToXml_WithNullObject_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _formatter.ToXml<TestModel>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromXml_WithValidXml_ReturnsObject()
    {
        // Arrange
        var xml = "<TestModel><Id>1</Id><Name>Test</Name></TestModel>";

        // Act
        var result = _formatter.FromXml<TestModel>(xml);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public void FromXml_WithNullOrEmptyXml_ThrowsArgumentException()
    {
        // Act
        Action act = () => _formatter.FromXml<TestModel>(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FormatXml_WithValidXml_ReturnsFormattedXml()
    {
        // Arrange
        var xml = "<TestModel><Id>1</Id><Name>Test</Name></TestModel>";

        // Act
        var formattedXml = _formatter.FormatXml(xml);

        // Assert
        formattedXml.Should().NotBeNullOrEmpty();
        formattedXml.Should().Contain("<TestModel>");
        formattedXml.Should().Contain("<Id>1</Id>");
        formattedXml.Should().Contain("<Name>Test</Name>");
    }

    [Fact]
    public void FormatXml_WithNullOrEmptyXml_ReturnsSameValue()
    {
        // Act
        var resultNull = _formatter.FormatXml(null!);
        var resultEmpty = _formatter.FormatXml(string.Empty);

        // Assert
        resultNull.Should().BeNull();
        resultEmpty.Should().BeEmpty();
    }

    [Fact]
    public void MinifyXml_WithValidXml_ReturnsMinifiedXml()
    {
        // Arrange
        var xml = "<TestModel>\n  <Id>1</Id>\n  <Name>Test</Name>\n</TestModel>";

        // Act
        var minifiedXml = XmlFormatter.MinifyXml(xml);

        // Assert
        minifiedXml.Should().NotBeNullOrEmpty();
        minifiedXml.Should().NotContain("\n");
        minifiedXml.Should().NotContain("  ");
        minifiedXml.Should().Contain("<TestModel><Id>1</Id><Name>Test</Name></TestModel>");
    }

    [Fact]
    public void MinifyXml_WithNullOrEmptyXml_ReturnsSameValue()
    {
        // Act
        var resultNull = XmlFormatter.MinifyXml(null!);
        var resultEmpty = XmlFormatter.MinifyXml(string.Empty);

        // Assert
        resultNull.Should().BeNull();
        resultEmpty.Should().BeEmpty();
    }

    [Fact]
    public void ValidateXml_WithValidXml_ReturnsValid()
    {
        // Arrange
        var xml = "<TestModel><Id>1</Id><Name>Test</Name></TestModel>";

        // Act
        var (isValid, errors) = _formatter.ValidateXml(xml);

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateXml_WithInvalidXml_ReturnsInvalidWithErrors()
    {
        // Arrange
        var xml = "<TestModel><Id>1</Id><Name>Test</TestModel>";

        // Act
        var (isValid, errors) = _formatter.ValidateXml(xml);

        // Assert
        isValid.Should().BeFalse();
        errors.Should().NotBeEmpty();
        errors.Should().ContainSingle();
    }

    [Fact]
    public void ValidateXml_WithNullOrEmptyXml_ReturnsInvalidWithError()
    {
        // Act
        var (isValidNull, errorsNull) = _formatter.ValidateXml(null!);
        var (isValidEmpty, errorsEmpty) = _formatter.ValidateXml(string.Empty);

        // Assert
        isValidNull.Should().BeFalse();
        errorsNull.Should().ContainSingle();
        isValidEmpty.Should().BeFalse();
        errorsEmpty.Should().ContainSingle();
    }

    [Fact]
    public void GetElementValueByXPath_WithValidXmlAndXPath_ReturnsValue()
    {
        // Arrange
        var xml = "<TestModel><Id>1</Id><Name>Test</Name></TestModel>";
        var xpath = "/TestModel/Name";

        // Act
        var value = _formatter.GetElementValueByXPath(xml, xpath);

        // Assert
        value.Should().Be("Test");
    }

    [Fact]
    public void GetElementValueByXPath_WithInvalidXPath_ReturnsNull()
    {
        // Arrange
        var xml = "<TestModel><Id>1</Id><Name>Test</Name></TestModel>";
        var xpath = "/TestModel/Invalid";

        // Act
        var value = _formatter.GetElementValueByXPath(xml, xpath);

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void GetElementValueByXPath_WithNullOrEmptyParameters_ReturnsNull()
    {
        // Act
        var valueNullXml = _formatter.GetElementValueByXPath(null!, "/Test");
        var valueEmptyXml = _formatter.GetElementValueByXPath(string.Empty, "/Test");
        var valueNullXPath = _formatter.GetElementValueByXPath("<Test></Test>", null!);
        var valueEmptyXPath = _formatter.GetElementValueByXPath("<Test></Test>", string.Empty);

        // Assert
        valueNullXml.Should().BeNull();
        valueEmptyXml.Should().BeNull();
        valueNullXPath.Should().BeNull();
        valueEmptyXPath.Should().BeNull();
    }

    [Fact]
    public async Task ExportToFileAsync_WithValidObject_CreatesFile()
    {
        // Arrange
        var testObject = new TestModel { Id = 1, Name = "Test" };
        var filePath = Path.GetTempFileName();

        try
        {
            // Act
            await _formatter.ExportToFileAsync(testObject, filePath);

            // Assert
            File.Exists(filePath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().NotBeNullOrEmpty();
            content.Should().Contain("<Id>1</Id>");
            content.Should().Contain("<Name>Test</Name>");
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task ExportToFileAsync_WithNullFilePath_ThrowsArgumentException()
    {
        // Arrange
        var testObject = new TestModel { Id = 1, Name = "Test" };

        // Act
        Func<Task> act = async () => await _formatter.ExportToFileAsync(testObject, string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ImportFromFileAsync_WithExistingFile_ReturnsObject()
    {
        // Arrange
        var xml = "<TestModel><Id>1</Id><Name>Test</Name></TestModel>";
        var filePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(filePath, xml);

        try
        {
            // Act
            var result = await _formatter.ImportFromFileAsync<TestModel>(filePath);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Test");
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    [Fact]
    public async Task ImportFromFileAsync_WithNullFilePath_ThrowsArgumentException()
    {
        // Act
        Func<Task> act = async () => await _formatter.ImportFromFileAsync<TestModel>(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ImportFromFileAsync_WithNonExistingFile_ThrowsFileNotFoundException()
    {
        // Act
        Func<Task> act = async () => await _formatter.ImportFromFileAsync<TestModel>("nonexistent.xml");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void MergeXml_WithTwoValidXmlDocuments_ReturnsMergedXml()
    {
        // Arrange
        var xml1 = "<Root><Element1>Value1</Element1></Root>";
        var xml2 = "<Root><Element2>Value2</Element2></Root>";

        // Act
        var mergedXml = _formatter.MergeXml(xml1, xml2);

        // Assert
        mergedXml.Should().NotBeNullOrEmpty();
        mergedXml.Should().Contain("<Element1>Value1</Element1>");
        mergedXml.Should().Contain("<Element2>Value2</Element2>");
    }

    [Fact]
    public void MergeXml_WithNullOrEmptyParameters_ReturnsOtherValue()
    {
        // Act
        var result1 = _formatter.MergeXml(null!, "<Test>Value</Test>");
        var result2 = _formatter.MergeXml("<Test>Value</Test>", string.Empty);
        var result3 = _formatter.MergeXml(string.Empty, "<Test>Value</Test>");
        var result4 = _formatter.MergeXml(null!, string.Empty);

        // Assert
        result1.Should().Be("<Test>Value</Test>");
        result2.Should().Be("<Test>Value</Test>");
        result3.Should().Be("<Test>Value</Test>");
        result4.Should().BeEmpty();
    }
}