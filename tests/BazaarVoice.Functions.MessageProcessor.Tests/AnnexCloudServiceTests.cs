using BazaarVoice.Common.Models;
using BazaarVoice.Functions.MessageProcessor.Models;
using BazaarVoice.Functions.MessageProcessor.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Xunit;

namespace BazaarVoice.Functions.MessageProcessor.Tests
{
    public class AnnexCloudServiceTests
    {
        private readonly Mock<ILogger<AnnexCloudService>> _loggerMock;
        private readonly IMemoryCache _memoryCache;
        private readonly Configuration.MessageProcessorSettings _settings;

        public AnnexCloudServiceTests()
        {
            _loggerMock = new Mock<ILogger<AnnexCloudService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _settings = new Configuration.MessageProcessorSettings
            {
                AnnexCloudApiBaseUrl = "https://s15.socialannex.net"
            };
        }

        [Fact]
        public async Task GetReviewAndRatingActionAsync_WhenCached_ReturnsCachedAction()
        {
            // Arrange
            var cachedAction = new ReviewRatingAction
            {
                ActionCode = "REVIEW_RATING",
                ActionName = "Review & Rating",
                PointsValue = 100
            };

            _memoryCache.Set("ReviewAndRatingAction", cachedAction);

            var httpClientFactory = CreateMockHttpClientFactory(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(new AnnexCloudActionResponse()));

            var sut = new AnnexCloudService(httpClientFactory, _memoryCache, _settings, _loggerMock.Object);

            // Act
            var result = await sut.GetReviewAndRatingActionAsync();

            // Assert
            result.Should().NotBeNull();
            result!.ActionCode.Should().Be("REVIEW_RATING");
            result.PointsValue.Should().Be(100);
        }

        [Fact]
        public async Task GetReviewAndRatingActionAsync_WhenApiReturnsAction_CachesAndReturnsIt()
        {
            // Arrange
            var apiResponse = new AnnexCloudActionResponse
            {
                Success = true,
                Actions = new List<ReviewRatingAction>
                {
                    new ReviewRatingAction
                    {
                        ActionCode = "RR_CODE",
                        ActionName = "Review & Rating",
                        PointsValue = 50,
                        IsActive = true
                    }
                }
            };

            var httpClientFactory = CreateMockHttpClientFactory(
                HttpStatusCode.OK,
                JsonSerializer.Serialize(apiResponse));

            var sut = new AnnexCloudService(httpClientFactory, _memoryCache, _settings, _loggerMock.Object);

            // Act
            var result = await sut.GetReviewAndRatingActionAsync();

            // Assert
            result.Should().NotBeNull();
            result!.ActionCode.Should().Be("RR_CODE");
            result.PointsValue.Should().Be(50);

            // Verify it was cached
            _memoryCache.TryGetValue("ReviewAndRatingAction", out ReviewRatingAction? cached).Should().BeTrue();
            cached!.ActionCode.Should().Be("RR_CODE");
        }

        private IHttpClientFactory CreateMockHttpClientFactory(
            HttpStatusCode statusCode, string responseContent)
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent,
                        System.Text.Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri(_settings.AnnexCloudApiBaseUrl!)
            };

            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock
                .Setup(f => f.CreateClient("AnnexCloud"))
                .Returns(httpClient);

            return factoryMock.Object;
        }
    }
}
