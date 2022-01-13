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
        public async Task CommentTest()
        {
            // Arrange
            var service = CreateService();
            var reddit = new RedditClient
                ("VknIzN8a-iphsQ",
                "658227845723--OoSarFXt7F2NewWn5xssHg48ePDDw",
                "ITmTWqKAKZJfjTm8UDJ4GVeBeEU",
                "658227845723-ozL1JJm9PgImhB7ryd1h8DcPF5uMmg",
                "script:songacronymbot:v1.0");
            var comment = reddit.Comment("t1_hrzsgw2").About();
            MockGlobalAcronyms();
            MockSubredditAcronyms(comment.Subreddit.ToLower());

            // Act
            var acronyms = await service.FindAcronymsAsync(comment);

            // Assert
            Assert.True(acronyms.Count == 2);
        }

        private void MockGlobalAcronyms()
        {
            mockAcronymRepository.Setup(x => x.GetAllGlobalAcronyms()).ReturnsAsync(new List<Acronym>());
        }

        private void MockSubredditAcronyms(string subredditName)
        {
            var acronyms = new List<Acronym>
            {
                CreateFakeAcronym("TMB"),
                CreateFakeAcronym("HDIMYLM")
            };

            mockAcronymRepository.Setup(x => x.GetAllBySubredditNameAsync(subredditName)).ReturnsAsync(acronyms);
        }

        private Acronym CreateFakeAcronym(string acronym)
        {
            return new Acronym
            {
                AcronymName = acronym,
                AcronymType = Domain.Enum.AcronymType.Track,
                AlbumName = "Fake Album",
                ArtistName = "Fake Artist",
                Enabled = true,
                TrackName = "Fake Track",
                YearReleased = "2022"
            };
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
