using System.Xml.Linq;
using BazaarVoice.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BazaarVoice.Common.Tests
{
    public class XmlExtensionsTests
    {
        [Fact]
        public void GetElementValue_WithExistingElement_ReturnsTrimmedValue()
        {
            var xml = XElement.Parse("<Root><Name>  John Doe  </Name></Root>");
            xml.GetElementValue("Name").Should().Be("John Doe");
        }

        [Fact]
        public void GetElementValue_WithMissingElement_ReturnsNull()
        {
            var xml = XElement.Parse("<Root><Name>John</Name></Root>");
            xml.GetElementValue("Missing").Should().BeNull();
        }

        [Fact]
        public void GetElementValue_WithEmptyElement_ReturnsNull()
        {
            var xml = XElement.Parse("<Root><Name>  </Name></Root>");
            xml.GetElementValue("Name").Should().BeNull();
        }

        [Fact]
        public void GetElementIntValue_WithValidInt_ReturnsIntValue()
        {
            var xml = XElement.Parse("<Root><Rating>5</Rating></Root>");
            xml.GetElementIntValue("Rating").Should().Be(5);
        }

        [Fact]
        public void GetElementIntValue_WithInvalidInt_ReturnsDefault()
        {
            var xml = XElement.Parse("<Root><Rating>abc</Rating></Root>");
            xml.GetElementIntValue("Rating", 0).Should().Be(0);
        }

        [Fact]
        public void GetElementIntValue_WithMissingElement_ReturnsDefault()
        {
            var xml = XElement.Parse("<Root></Root>");
            xml.GetElementIntValue("Rating", -1).Should().Be(-1);
        }

        [Fact]
        public void GetElementDateValue_WithValidDate_ReturnsDateTime()
        {
            var xml = XElement.Parse("<Root><Date>2024-01-15T10:00:00Z</Date></Root>");
            var result = xml.GetElementDateValue("Date");
            result.Should().NotBeNull();
            result!.Value.Year.Should().Be(2024);
            result.Value.Month.Should().Be(1);
            result.Value.Day.Should().Be(15);
        }

        [Fact]
        public void GetElementDateValue_WithInvalidDate_ReturnsNull()
        {
            var xml = XElement.Parse("<Root><Date>not-a-date</Date></Root>");
            xml.GetElementDateValue("Date").Should().BeNull();
        }

        [Fact]
        public void GetAttributeValue_WithExistingAttribute_ReturnsTrimmedValue()
        {
            var xml = XElement.Parse("<Root id=\"  123  \" />");
            xml.GetAttributeValue("id").Should().Be("123");
        }

        [Fact]
        public void GetAttributeValue_WithMissingAttribute_ReturnsNull()
        {
            var xml = XElement.Parse("<Root />");
            xml.GetAttributeValue("id").Should().BeNull();
        }
    }
}
