using SongAcronymBot.Core.Model;
using SongAcronymBot.Core.Services;
using System.Linq;
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

        [Fact]
        public void BuildReplyBody_WithDuplicateAcronyms_ShouldConsolidateWithOr()
        {
            // Arrange - Same acronym from different artists
            var matches = new List<AcronymMatch>
            {
                new("AIWFCIY", 1) 
                { 
                    CommentBody = "- AIWFCIY could mean \"All I Want For Christmas Is You\" by Mariah Carey.\n",
                    MatchDescription = "\"All I Want For Christmas Is You\" by Mariah Carey"
                },
                new("AIWFCIY", 2) 
                { 
                    CommentBody = "- AIWFCIY could mean \"All I Want For Color Is Yellow\" by Justin Bieber.\n",
                    MatchDescription = "\"All I Want For Color Is Yellow\" by Justin Bieber"
                }
            };
            var author = "testuser";

            // Act
            var result = _formatter.BuildReplyBody(matches, author);

            // Assert - Should be consolidated into one line
            Assert.Contains("AIWFCIY could mean \"All I Want For Christmas Is You\" by Mariah Carey or \"All I Want For Color Is Yellow\" by Justin Bieber", result);
            // Should NOT have two separate lines for AIWFCIY
            Assert.Single(result.Split('\n').Where(l => l.StartsWith("- AIWFCIY")));
        }

        [Fact]
        public void BuildReplyBody_WithMixedDuplicatesAndUniques_ShouldConsolidateCorrectly()
        {
            // Arrange
            var matches = new List<AcronymMatch>
            {
                new("AIWFCIY", 1) 
                { 
                    CommentBody = "- AIWFCIY could mean \"Song A\" by Artist A.\n",
                    MatchDescription = "\"Song A\" by Artist A"
                },
                new("WAP", 2) 
                { 
                    CommentBody = "- WAP could mean \"Wet Ass Pussy\" by Cardi B.\n",
                    MatchDescription = "\"Wet Ass Pussy\" by Cardi B"
                },
                new("AIWFCIY", 3) 
                { 
                    CommentBody = "- AIWFCIY could mean \"Song B\" by Artist B.\n",
                    MatchDescription = "\"Song B\" by Artist B"
                }
            };
            var author = "testuser";

            // Act
            var result = _formatter.BuildReplyBody(matches, author);

            // Assert
            // AIWFCIY should be consolidated (appears first based on min position)
            Assert.Contains("AIWFCIY could mean \"Song A\" by Artist A or \"Song B\" by Artist B", result);
            // WAP should remain as single line
            Assert.Contains("WAP could mean \"Wet Ass Pussy\" by Cardi B", result);
            // Should have exactly 2 bullet points
            Assert.Equal(2, result.Split('\n').Count(l => l.StartsWith("- ")));
        }
    }
}
