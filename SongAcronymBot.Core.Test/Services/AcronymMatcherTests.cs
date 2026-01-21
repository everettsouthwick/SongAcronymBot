using SongAcronymBot.Core.Services;
using Xunit;

namespace SongAcronymBot.Core.Test.Services
{
    /// <summary>
    /// Tests for AcronymMatcher.
    /// Note: Many methods require Reddit.Controllers.Comment which is difficult to mock.
    /// These tests focus on the core matching logic via the internal IsAcronymInText method.
    /// </summary>
    public class AcronymMatcherTests
    {
        #region IsAcronymInText Tests

        [Theory]
        [InlineData("NLMD", true, 0)]  // Acronym only
        [InlineData("I love NLMD", true, 7)]  // Acronym at end
        [InlineData("NLMD is great", true, 0)]  // Acronym at start
        [InlineData("I think NLMD is their best work", true, 8)]  // Acronym in middle
        public void IsAcronymInText_ShortComments_FindsAcronym(string text, bool expected, int expectedIndex)
        {
            // Act
            var result = AcronymMatcher.IsAcronymInText(text, "NLMD", out int index);

            // Assert
            Assert.Equal(expected, result);
            if (expected)
            {
                Assert.Equal(expectedIndex, index);
            }
        }

        [Theory]
        [InlineData("I've heard every album up to NLMD", true, 29)]  // Original failing case
        [InlineData("Their discography from INTMC to NLMD is fantastic", true, 32)]  // Multiple acronyms, find first occurrence of target
        [InlineData("The best albums are probably NLMD and also some others", true, 29)]
        [InlineData("Starting with NLMD then moving to The Outsiders was great", true, 14)]
        public void IsAcronymInText_MediumComments_FindsAcronym(string text, bool expected, int expectedIndex)
        {
            // Act
            var result = AcronymMatcher.IsAcronymInText(text, "NLMD", out int index);

            // Assert
            Assert.Equal(expected, result);
            if (expected)
            {
                Assert.Equal(expectedIndex, index);
            }
        }

        [Theory]
        [InlineData("I've been listening to Bowie for years now. Just finished going through all the albums chronologically and I'm up to NLMD. Any recommendations on what comes next? I heard Scary Monsters is really good but wanted other opinions first.", true)]
        [InlineData("So I started with Hunky Dory, then moved on to Ziggy Stardust, then Aladdin Sane. After a brief detour through Diamond Dogs, I've now settled on NLMD as my current favorite. The production quality is just incredible!", true)]
        [InlineData("What do you all think of the album NLMD? I'm curious to hear different perspectives on this one. Some say it's underrated while others consider it a masterpiece. I've been debating this with friends for weeks now.", true)]
        public void IsAcronymInText_LongComments_FindsAcronym(string text, bool expected)
        {
            // Act
            var result = AcronymMatcher.IsAcronymInText(text, "NLMD", out int index);

            // Assert
            Assert.Equal(expected, result);
            if (expected)
            {
                Assert.True(index >= 0, "Index should be valid when match is found");
            }
        }

        [Theory]
        [InlineData("nlmd", "NLMD", true)]  // All lowercase text, uppercase acronym
        [InlineData("NLMD", "nlmd", true)]  // All uppercase text, lowercase acronym
        [InlineData("Nlmd", "NLMD", true)]  // Mixed case text
        [InlineData("nLmD", "nlmd", true)]  // Weird casing
        [InlineData("I really love nlmd", "NLMD", true)]  // Lowercase in sentence
        [InlineData("what about NLMD eh?", "nlmd", true)]  // Uppercase in sentence
        public void IsAcronymInText_CaseInsensitive_Works(string text, string acronym, bool expected)
        {
            // Act
            var result = AcronymMatcher.IsAcronymInText(text, acronym, out int _);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("INTMC", "NLMD", false)]  // Different acronym
        [InlineData("Something completely different", "NLMD", false)]  // No acronym
        [InlineData("", "NLMD", false)]  // Empty text
        [InlineData("NLMD", "", false)]  // Empty acronym
        [InlineData(null, "NLMD", false)]  // Null text
        [InlineData("NLMD", null, false)]  // Null acronym
        public void IsAcronymInText_NoMatch_ReturnsFalse(string? text, string? acronym, bool expected)
        {
            // Act
            var result = AcronymMatcher.IsAcronymInText(text!, acronym!, out int index);

            // Assert
            Assert.Equal(expected, result);
            Assert.Equal(-1, index);
        }

        [Theory]
        [InlineData("NLMDA", "NLMD", false)]  // Acronym as part of larger word (suffix)
        [InlineData("XNLMD", "NLMD", false)]  // Acronym as part of larger word (prefix)
        [InlineData("XNLMDY", "NLMD", false)]  // Acronym as part of larger word (both)
        [InlineData("SomeNLMDWord", "NLMD", false)]  // Embedded in word
        public void IsAcronymInText_PartOfWord_ReturnsFalse(string text, string acronym, bool expected)
        {
            // Act
            var result = AcronymMatcher.IsAcronymInText(text, acronym, out int _);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("I love (NLMD)", "NLMD", true)]  // Parentheses
        [InlineData("NLMD!", "NLMD", true)]  // Exclamation at end
        [InlineData("NLMD?", "NLMD", true)]  // Question mark at end
        [InlineData("NLMD.", "NLMD", true)]  // Period at end
        [InlineData("NLMD,", "NLMD", true)]  // Comma at end
        [InlineData("\"NLMD\"", "NLMD", true)]  // Quotes around
        [InlineData("'NLMD'", "NLMD", true)]  // Single quotes
        [InlineData("NLMD's", "NLMD", true)]  // Possessive - apostrophe filtered out, match found
        public void IsAcronymInText_SpecialCharacters_HandledCorrectly(string text, string acronym, bool expected)
        {
            // Act
            var result = AcronymMatcher.IsAcronymInText(text, acronym, out int _);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("A", "A", true)]  // Single character acronym at start
        [InlineData("Just A", "A", true)]  // Single character at end
        [InlineData("AB", "AB", true)]  // Two character acronym
        [InlineData("ABCDEFGHIJKLMNOP", "ABCDEFGHIJKLMNOP", true)]  // Very long acronym
        public void IsAcronymInText_VariousAcronymLengths_Works(string text, string acronym, bool expected)
        {
            // Act
            var result = AcronymMatcher.IsAcronymInText(text, acronym, out int _);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsAcronymInText_OriginalBugCase_ShouldNotThrow()
        {
            // Arrange - The exact comment that caused the original bug
            var comment = "I've heard every album up to NLMD";
            var acronym = "NLMD";

            // Act - Should not throw ArgumentOutOfRangeException
            var exception = Record.Exception(() => 
                AcronymMatcher.IsAcronymInText(comment, acronym, out int _));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void IsAcronymInText_OriginalBugCase_ReturnsTrue()
        {
            // Arrange - The exact comment that caused the original bug
            var comment = "I've heard every album up to NLMD";
            var acronym = "NLMD";

            // Act
            var result = AcronymMatcher.IsAcronymInText(comment, acronym, out int index);

            // Assert
            Assert.True(result);
            Assert.Equal(29, index);
        }

        #endregion
    }
}
