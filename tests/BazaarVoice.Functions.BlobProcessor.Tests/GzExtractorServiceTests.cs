using System.IO;
using System.IO.Compression;
using System.Text;
using BazaarVoice.Functions.BlobProcessor.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BazaarVoice.Functions.BlobProcessor.Tests
{
    public class GzExtractorServiceTests
    {
        private readonly GzExtractorService _sut;
        private readonly Mock<ILogger<GzExtractorService>> _loggerMock;

        public GzExtractorServiceTests()
        {
            _loggerMock = new Mock<ILogger<GzExtractorService>>();
            _sut = new GzExtractorService(_loggerMock.Object);
        }

        [Fact]
        public async Task ExtractGzToXmlAsync_WithValidGzStream_ReturnsXmlContent()
        {
            // Arrange
            var expectedXml = "<Reviews><Review><Id>123</Id></Review></Reviews>";
            var gzStream = CreateGzStream(expectedXml);

            // Act
            var result = await _sut.ExtractGzToXmlAsync(gzStream);

            // Assert
            result.Should().Be(expectedXml);
        }

        [Fact]
        public async Task ExtractGzToXmlAsync_WithNullStream_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.ExtractGzToXmlAsync(null!));
        }

        [Fact]
        public async Task ExtractGzToXmlAsync_WithEmptyStream_ThrowsArgumentException()
        {
            // Arrange
            var emptyStream = new MemoryStream();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _sut.ExtractGzToXmlAsync(emptyStream));
        }

        private static Stream CreateGzStream(string content)
        {
            var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, leaveOpen: true))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                gzipStream.Write(bytes, 0, bytes.Length);
            }
            outputStream.Position = 0;
            return outputStream;
        }
    }
}
