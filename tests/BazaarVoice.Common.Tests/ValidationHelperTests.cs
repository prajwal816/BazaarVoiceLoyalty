using BazaarVoice.Common.Helpers;
using BazaarVoice.Common.Models;
using FluentAssertions;
using Xunit;

namespace BazaarVoice.Common.Tests
{
    public class ValidationHelperTests
    {
        [Theory]
        [InlineData("file.gz", true)]
        [InlineData("FILE.GZ", true)]
        [InlineData("archive.tar.gz", true)]
        [InlineData("file.xml", false)]
        [InlineData("file.zip", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidGzFile_ReturnsExpectedResult(string? fileName, bool expected)
        {
            ValidationHelper.IsValidGzFile(fileName).Should().Be(expected);
        }

        [Fact]
        public void ValidateRecord_WithValidRecord_ReturnsTrue()
        {
            var record = new BazaarVoiceXmlRecord
            {
                Email = "test@example.com",
                ReviewId = "REV001"
            };

            var result = ValidationHelper.ValidateRecord(record, out var errors);

            result.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateRecord_WithNullRecord_ReturnsFalse()
        {
            var result = ValidationHelper.ValidateRecord(null, out var errors);

            result.Should().BeFalse();
            errors.Should().Contain("Record is null.");
        }

        [Fact]
        public void ValidateRecord_WithMissingEmail_ReturnsFalse()
        {
            var record = new BazaarVoiceXmlRecord
            {
                ReviewId = "REV001"
            };

            var result = ValidationHelper.ValidateRecord(record, out var errors);

            result.Should().BeFalse();
            errors.Should().Contain("Email is required.");
        }

        [Fact]
        public void ValidateRecord_WithInvalidEmail_ReturnsFalse()
        {
            var record = new BazaarVoiceXmlRecord
            {
                Email = "not-an-email",
                ReviewId = "REV001"
            };

            var result = ValidationHelper.ValidateRecord(record, out var errors);

            result.Should().BeFalse();
            errors.Should().Contain(e => e.Contains("not a valid email"));
        }

        [Fact]
        public void ValidateRecord_WithMissingReviewId_ReturnsFalse()
        {
            var record = new BazaarVoiceXmlRecord
            {
                Email = "test@example.com"
            };

            var result = ValidationHelper.ValidateRecord(record, out var errors);

            result.Should().BeFalse();
            errors.Should().Contain("ReviewId is required.");
        }

        [Theory]
        [InlineData("ANNEX123", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("  ", false)]
        public void IsValidMemberAnnexId_ReturnsExpectedResult(string? id, bool expected)
        {
            ValidationHelper.IsValidMemberAnnexId(id).Should().Be(expected);
        }

        [Theory]
        [InlineData("RR_ACTION", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsValidActionCode_ReturnsExpectedResult(string? code, bool expected)
        {
            ValidationHelper.IsValidActionCode(code).Should().Be(expected);
        }
    }
}
