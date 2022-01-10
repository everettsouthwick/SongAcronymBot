using Moq;
using Reddit;
using SongAcronymBot.Core.Services;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Repositories;
using SongAcronymBot.Domain.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SongAcronymBot.Core.Test.Services
{
    public class RedditServiceTests
    {
        private MockRepository mockRepository;

        private Mock<IAcronymRepository> mockAcronymRepository;
        private Mock<IRedditorRepository> mockRedditorRepository;
        private Mock<ISubredditRepository> mockSubredditRepository;
        private Mock<ISpotifyService> mockSpotifyService;

        public RedditServiceTests()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);

            mockAcronymRepository = mockRepository.Create<IAcronymRepository>();
            mockRedditorRepository = mockRepository.Create<IRedditorRepository>();
            mockSubredditRepository = mockRepository.Create<ISubredditRepository>();
            mockSpotifyService = mockRepository.Create<ISpotifyService>();
        }

        private RedditService CreateService()
        {
            return new RedditService(
                mockAcronymRepository.Object,
                mockRedditorRepository.Object,
                mockSubredditRepository.Object,
                mockSpotifyService.Object);
        }

        [Fact]
        public async Task ()
        {
            // Arrange
            var service = CreateService();
            var reddit = new RedditClient
                ("VknIzN8a-iphsQ",
                "658227845723--OoSarFXt7F2NewWn5xssHg48ePDDw",
                "ITmTWqKAKZJfjTm8UDJ4GVeBeEU",
                "658227845723-ozL1JJm9PgImhB7ryd1h8DcPF5uMmg",
                "script:songacronymbot:v1.0");
            var comment = reddit.Comment("t1_hrs3d2f").About();
            MockGlobalAcronyms();
            MockSubredditAcronyms(comment.Subreddit.ToLower());

            // Act
            var acronyms = await service.FindAcronymsAsync(comment);

            // Assert
            Assert.NotNull(acronyms);
        }

        private void MockGlobalAcronyms()
        {
            mockAcronymRepository.Setup(x => x.GetAllGlobalAcronyms()).ReturnsAsync(new List<Acronym>());
        }

        private void MockSubredditAcronyms(string subredditName)
        {
            var acronyms = new List<Acronym>
            {
                new Acronym
                {
                    AcronymName = "V&V",
                    AcronymType = Domain.Enum.AcronymType.Album,
                    AlbumName = "Vices & Virtues",
                    ArtistName = "Panic! At The Disco",
                    YearReleased = "2011",
                    Enabled = true,
                }
            };

            mockAcronymRepository.Setup(x => x.GetAllBySubredditNameAsync(subredditName)).ReturnsAsync(acronyms);
        }

        [Fact]
        public async Task StartAsync_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var service = CreateService();
            RedditClient reddit = null;
            bool debug = false;

            // Act
            await service.StartAsync(
                reddit,
                debug);

            // Assert
            Assert.True(false);
            mockRepository.VerifyAll();
        }
    }
}
