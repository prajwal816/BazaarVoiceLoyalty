using BazaarVoice.Common.Exceptions;
using BazaarVoice.Functions.BlobProcessor.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BazaarVoice.Functions.BlobProcessor.Tests
{
    public class XmlParserServiceTests
    {
        private readonly XmlParserService _sut;
        private readonly Mock<ILogger<XmlParserService>> _loggerMock;

        public XmlParserServiceTests()
        {
            _loggerMock = new Mock<ILogger<XmlParserService>>();
            _sut = new XmlParserService(_loggerMock.Object);
        }

        [Fact]
        public void ParseRecords_WithValidXml_ReturnsRecords()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <Reviews>
                    <Review>
                        <Id>REV001</Id>
                        <EmailAddress>test@example.com</EmailAddress>
                        <UserNickname>TestUser</UserNickname>
                        <Rating>5</Rating>
                        <ReviewText>Great product!</ReviewText>
                        <SubmissionTime>2024-01-15T10:00:00Z</SubmissionTime>
                        <ProductId>PROD001</ProductId>
                    </Review>
                </Reviews>";

            // Act
            var records = _sut.ParseRecords(xml);

            // Assert
            records.Should().HaveCount(1);
            records[0].ReviewId.Should().Be("REV001");
            records[0].Email.Should().Be("test@example.com");
            records[0].UserNickname.Should().Be("TestUser");
            records[0].Rating.Should().Be(5);
            records[0].ReviewText.Should().Be("Great product!");
            records[0].ProductId.Should().Be("PROD001");
        }

        [Fact]
        public void ParseRecords_WithMultipleRecords_ReturnsAll()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <Reviews>
                    <Review>
                        <Id>REV001</Id>
                        <EmailAddress>user1@example.com</EmailAddress>
                        <Rating>5</Rating>
                    </Review>
                    <Review>
                        <Id>REV002</Id>
                        <EmailAddress>user2@example.com</EmailAddress>
                        <Rating>4</Rating>
                    </Review>
                    <Review>
                        <Id>REV003</Id>
                        <EmailAddress>user3@example.com</EmailAddress>
                        <Rating>3</Rating>
                    </Review>
                </Reviews>";

            // Act
            var records = _sut.ParseRecords(xml);

            // Assert
            records.Should().HaveCount(3);
            records.Select(r => r.Email).Should().Contain(new[]
            {
                "user1@example.com", "user2@example.com", "user3@example.com"
            });
        }

        [Fact]
        public void ParseRecords_WithMissingEmail_SkipsRecord()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <Reviews>
                    <Review>
                        <Id>REV001</Id>
                        <EmailAddress>valid@example.com</EmailAddress>
                        <Rating>5</Rating>
                    </Review>
                    <Review>
                        <Id>REV002</Id>
                        <Rating>4</Rating>
                    </Review>
                </Reviews>";

            // Act
            var records = _sut.ParseRecords(xml);

            // Assert
            records.Should().HaveCount(1);
            records[0].Email.Should().Be("valid@example.com");
        }

        [Fact]
        public void ParseRecords_WithEmptyXml_ReturnsEmptyList()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?><Reviews></Reviews>";

            // Act
            var records = _sut.ParseRecords(xml);

            // Assert
            records.Should().BeEmpty();
        }

        [Fact]
        public void ParseRecords_WithInvalidXml_ThrowsProcessingException()
        {
            // Arrange
            var invalidXml = "this is not valid xml <><>";

            // Act & Assert
            Assert.Throws<ProcessingException>(() => _sut.ParseRecords(invalidXml));
        }

        [Fact]
        public void ParseRecords_WithMissingRating_DefaultsToZero()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                <Reviews>
                    <Review>
                        <Id>REV001</Id>
                        <EmailAddress>test@example.com</EmailAddress>
                    </Review>
                </Reviews>";

            // Act
            var records = _sut.ParseRecords(xml);

            // Assert
            records.Should().HaveCount(1);
            records[0].Rating.Should().Be(0);
        }
    }
}
