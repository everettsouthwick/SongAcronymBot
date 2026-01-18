using SongAcronymBot.Core.Model;
using SongAcronymBot.Core.Services;
using Xunit;

namespace SongAcronymBot.Core.Test.Services
{
    public class ReplyFormatterTests
    {
        private readonly ReplyFormatter _formatter;

        public ReplyFormatterTests()
        {
            _formatter = new ReplyFormatter();
        }

        [Fact]
        public void FormatReplyBodyWithFooter_ShouldAppendFooterWithAuthor()
        {
            // Arrange
            var body = "- TEST could mean \"Test Song\", a track.";
            var author = "testuser";

            // Act
            var result = _formatter.FormatReplyBodyWithFooter(body, author);

            // Assert
            Assert.Contains(body, result);
            Assert.Contains("/u/testuser", result);
            Assert.Contains("can reply with \"delete\"", result);
            Assert.Contains("/r/songacronymbot", result);
        }

        [Fact]
        public void FormatReplyBodyWithFooter_ShouldIncludeDivider()
        {
            // Arrange
            var body = "Test body";
            var author = "user123";

            // Act
            var result = _formatter.FormatReplyBodyWithFooter(body, author);

            // Assert
            Assert.Contains("---", result);
        }

        [Fact]
        public void BuildReplyBody_ShouldCombineMultipleMatches()
        {
            // Arrange
            var matches = new List<AcronymMatch>
            {
                new("TEST1", 1) { CommentBody = "- TEST1 means something.\n" },
                new("TEST2", 2) { CommentBody = "- TEST2 means something else.\n" }
            };
            var author = "testuser";

            // Act
            var result = _formatter.BuildReplyBody(matches, author);

            // Assert
            Assert.Contains("TEST1 means something", result);
            Assert.Contains("TEST2 means something else", result);
            Assert.Contains("/u/testuser", result);
        }

        [Fact]
        public void BuildReplyBody_WithEmptyMatches_ShouldReturnFooterOnly()
        {
            // Arrange
            var matches = new List<AcronymMatch>();
            var author = "testuser";

            // Act
            var result = _formatter.BuildReplyBody(matches, author);

            // Assert
            Assert.Contains("/u/testuser", result);
            Assert.Contains("---", result);
        }
    }
}
