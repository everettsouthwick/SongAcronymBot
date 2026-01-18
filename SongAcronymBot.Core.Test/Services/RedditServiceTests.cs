using Moq;
using Reddit;
using SongAcronymBot.Core.Services;
using SongAcronymBot.Domain.Models;
using SongAcronymBot.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SongAcronymBot.Core.Test.Services
{
    public class RedditServiceTests
    {
        private MockRepository mockRepository;

        private Mock<SongAcronymBot.Domain.Repositories.IAcronymRepository> mockAcronymRepository;
        private Mock<SongAcronymBot.Domain.Supabase.Repositories.IOptedOutRedditorRepository> mockOptedOutRedditorRepository;

        public RedditServiceTests()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);

            mockAcronymRepository = mockRepository.Create<SongAcronymBot.Domain.Repositories.IAcronymRepository>();
            mockOptedOutRedditorRepository = mockRepository.Create<SongAcronymBot.Domain.Supabase.Repositories.IOptedOutRedditorRepository>();
        }

        private RedditService CreateService()
        {
            return new RedditService(
                mockAcronymRepository.Object,
                mockOptedOutRedditorRepository.Object);
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
            mockAcronymRepository.Setup(x => x.GetAllGlobalAcronyms()).ReturnsAsync([]);
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

        private static Acronym CreateFakeAcronym(string acronym)
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
