using BazaarVoice.Common.Extensions;
using FluentAssertions;
using Xunit;

namespace BazaarVoice.Common.Tests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("path/to/file.gz", "path_to_file")]
        [InlineData("incoming\\fromBazaarVoice\\test.gz", "incoming_fromBazaarVoice_test")]
        [InlineData("simple.gz", "simple")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void SanitizeForCheckpoint_ReturnsExpectedResult(string? input, string expected)
        {
            // Act
            var result = input.SanitizeForCheckpoint();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("test@example.com", true)]
        [InlineData("user.name+tag@domain.co.uk", true)]
        [InlineData("user@sub.domain.com", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("not-an-email", false)]
        [InlineData("@missing-local.com", false)]
        [InlineData("missing-domain@", false)]
        public void IsValidEmail_ReturnsExpectedResult(string? input, bool expected)
        {
            // Act
            var result = input.IsValidEmail();

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("short", 10, "short")]
        [InlineData("this is a longer string", 10, "this is a ...")]
        [InlineData("", 10, "")]
        [InlineData(null, 10, "")]
        public void Truncate_ReturnsExpectedResult(string? input, int maxLength, string expected)
        {
            // Act
            var result = input.Truncate(maxLength);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("john.doe@example.com", "j***@example.com")]
        [InlineData("a@example.com", "a@example.com")]
        [InlineData("", "[empty]")]
        [InlineData(null, "[empty]")]
        [InlineData("invalid", "[invalid]")]
        public void MaskEmail_ReturnsExpectedResult(string? input, string expected)
        {
            // Act
            var result = input.MaskEmail();

            // Assert
            result.Should().Be(expected);
        }
    }
}
