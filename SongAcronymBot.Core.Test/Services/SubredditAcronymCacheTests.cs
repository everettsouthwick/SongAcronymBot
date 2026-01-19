using Microsoft.Extensions.Logging;
using Moq;
using SongAcronymBot.Core.Services;
using SongAcronymBot.Core.Services.Interfaces;
using SongAcronymBot.Domain.Repositories.Interfaces;
using SongAcronymBot.Domain.Supabase.Models;
using System.Threading.Tasks;
using Xunit;

namespace SongAcronymBot.Core.Test.Services
{
    public class SubredditAcronymCacheTests
    {
        private readonly Mock<IAcronymRepository> _mockRepository;
        private readonly Mock<ILogger<SubredditAcronymCache>> _mockLogger;
        private readonly SubredditAcronymCache _cache;

        public SubredditAcronymCacheTests()
        {
            _mockRepository = new Mock<IAcronymRepository>();
            _mockLogger = new Mock<ILogger<SubredditAcronymCache>>();
            _cache = new SubredditAcronymCache(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task GetAcronymsAsync_FirstCall_ShouldFetchFromRepository()
        {
            // Arrange
            var subredditName = "testsubreddit";
            var acronyms = new List<EnrichedAcronym>
            {
                new() { Id = Guid.NewGuid(), AcronymText = "TEST", ArtistName = "Test Artist" }
            };
            _mockRepository.Setup(r => r.GetEnrichedAcronymsBySubredditNameAsync(subredditName))
                .ReturnsAsync(acronyms);

            // Act
            var result = await _cache.GetAcronymsAsync(subredditName);

            // Assert
            Assert.Single(result);
            Assert.Equal("TEST", result[0].AcronymText);
            _mockRepository.Verify(r => r.GetEnrichedAcronymsBySubredditNameAsync(subredditName), Times.Once);
        }

        [Fact]
        public async Task GetAcronymsAsync_SecondCall_ShouldUseCachedValue()
        {
            // Arrange
            var subredditName = "testsubreddit";
            var acronyms = new List<EnrichedAcronym>
            {
                new() { Id = Guid.NewGuid(), AcronymText = "TEST", ArtistName = "Test Artist" }
            };
            _mockRepository.Setup(r => r.GetEnrichedAcronymsBySubredditNameAsync(subredditName))
                .ReturnsAsync(acronyms);

            // Act
            await _cache.GetAcronymsAsync(subredditName);
            var result = await _cache.GetAcronymsAsync(subredditName);

            // Assert
            Assert.Single(result);
            _mockRepository.Verify(r => r.GetEnrichedAcronymsBySubredditNameAsync(subredditName), Times.Once);
        }

        [Fact]
        public async Task GetAcronymsAsync_DifferentSubreddits_ShouldFetchEach()
        {
            // Arrange
            var acronyms1 = new List<EnrichedAcronym>
            {
                new() { Id = Guid.NewGuid(), AcronymText = "SUB1", ArtistName = "Artist 1" }
            };
            var acronyms2 = new List<EnrichedAcronym>
            {
                new() { Id = Guid.NewGuid(), AcronymText = "SUB2", ArtistName = "Artist 2" }
            };
            _mockRepository.Setup(r => r.GetEnrichedAcronymsBySubredditNameAsync("subreddit1"))
                .ReturnsAsync(acronyms1);
            _mockRepository.Setup(r => r.GetEnrichedAcronymsBySubredditNameAsync("subreddit2"))
                .ReturnsAsync(acronyms2);

            // Act
            var result1 = await _cache.GetAcronymsAsync("subreddit1");
            var result2 = await _cache.GetAcronymsAsync("subreddit2");

            // Assert
            Assert.Equal("SUB1", result1[0].AcronymText);
            Assert.Equal("SUB2", result2[0].AcronymText);
            _mockRepository.Verify(r => r.GetEnrichedAcronymsBySubredditNameAsync("subreddit1"), Times.Once);
            _mockRepository.Verify(r => r.GetEnrichedAcronymsBySubredditNameAsync("subreddit2"), Times.Once);
        }

        [Fact]
        public async Task GetAcronymsAsync_WhenRepositoryThrows_ShouldReturnEmptyList()
        {
            // Arrange
            var subredditName = "failingsubreddit";
            _mockRepository.Setup(r => r.GetEnrichedAcronymsBySubredditNameAsync(subredditName))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _cache.GetAcronymsAsync(subredditName);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAcronymsAsync_WhenRepositoryReturnsEmpty_ShouldLogWarning()
        {
            // Arrange
            var subredditName = "emptysubreddit";
            _mockRepository.Setup(r => r.GetEnrichedAcronymsBySubredditNameAsync(subredditName))
                .ReturnsAsync(new List<EnrichedAcronym>());

            // Act
            var result = await _cache.GetAcronymsAsync(subredditName);

            // Assert
            Assert.Empty(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("0 acronyms")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
