using SongAcronymBot.Core.Services;
using Xunit;

namespace SongAcronymBot.Core.Test.Services
{
    /// <summary>
    /// Tests for PromotionalMessageFormatter.
    /// </summary>
    public class PromotionalMessageFormatterTests
    {
        private readonly PromotionalMessageFormatter _formatter;

        public PromotionalMessageFormatterTests()
        {
            _formatter = new PromotionalMessageFormatter();
        }

        #region FormatAsSmallText Tests

        [Fact]
        public void FormatAsSmallText_WithSingleWord_ShouldPrefixWithCaret()
        {
            var result = _formatter.FormatAsSmallText("Hello");
            
            Assert.Equal("^Hello ", result);
        }

        [Fact]
        public void FormatAsSmallText_WithMultipleWords_ShouldPrefixEachWordWithCaret()
        {
            var result = _formatter.FormatAsSmallText("Check out this site");
            
            Assert.Equal("^Check ^out ^this ^site ", result);
        }

        [Fact]
        public void FormatAsSmallText_WithEmptyString_ShouldReturnEmpty()
        {
            var result = _formatter.FormatAsSmallText("");
            
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void FormatAsSmallText_WithNull_ShouldReturnEmpty()
        {
            var result = _formatter.FormatAsSmallText(null!);
            
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void FormatAsSmallText_WithWhitespaceOnly_ShouldReturnEmpty()
        {
            var result = _formatter.FormatAsSmallText("   ");
            
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void FormatAsSmallText_WithExtraSpaces_ShouldNormalizeSpacing()
        {
            var result = _formatter.FormatAsSmallText("Hello   World");
            
            Assert.Equal("^Hello ^World ", result);
        }

        [Fact]
        public void FormatAsSmallText_ShouldAlwaysEndWithSpace()
        {
            var result = _formatter.FormatAsSmallText("Test");
            
            Assert.EndsWith(" ", result);
        }

        [Fact]
        public void FormatAsSmallText_WithSpecialCharacters_ShouldPrefixWithCaret()
        {
            var result = _formatter.FormatAsSmallText("Visit mysite.com!");
            
            Assert.Equal("^Visit ^mysite.com! ", result);
        }

        #endregion

        #region FormatPromotionalMessage Tests

        [Fact]
        public void FormatPromotionalMessage_WithValidInputs_ShouldFormatCorrectly()
        {
            var result = _formatter.FormatPromotionalMessage("Check this out", "https://example.com");
            
            Assert.Equal("\n\n[^Check ^this ^out ](https://example.com)", result);
        }

        [Fact]
        public void FormatPromotionalMessage_ShouldStartWithTwoNewlines()
        {
            var result = _formatter.FormatPromotionalMessage("Test", "https://example.com");
            
            Assert.StartsWith("\n\n", result);
        }

        [Fact]
        public void FormatPromotionalMessage_ShouldContainMarkdownLink()
        {
            var url = "https://example.com/promo";
            var result = _formatter.FormatPromotionalMessage("Test", url);
            
            Assert.Contains($"]({url})", result);
        }

        [Fact]
        public void FormatPromotionalMessage_WithNullMessageText_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => 
                _formatter.FormatPromotionalMessage(null!, "https://example.com"));
        }

        [Fact]
        public void FormatPromotionalMessage_WithNullUrl_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => 
                _formatter.FormatPromotionalMessage("Test", null!));
        }

        [Fact]
        public void FormatPromotionalMessage_ResultCanBeAppendedToComment()
        {
            var originalComment = "This is a great song!";
            var promo = _formatter.FormatPromotionalMessage("More info", "https://example.com");
            var combined = originalComment + promo;
            
            Assert.StartsWith(originalComment, combined);
            Assert.Contains("https://example.com", combined);
        }

        #endregion

        #region ContainsPromotionalContent Tests

        [Fact]
        public void ContainsPromotionalContent_WithUrlPresent_ShouldReturnTrue()
        {
            var commentBody = "Some text [^More ^info ](https://example.com)";
            var url = "https://example.com";
            
            var result = _formatter.ContainsPromotionalContent(commentBody, url);
            
            Assert.True(result);
        }

        [Fact]
        public void ContainsPromotionalContent_WithUrlNotPresent_ShouldReturnFalse()
        {
            var commentBody = "Some text without any promotional links";
            var url = "https://example.com";
            
            var result = _formatter.ContainsPromotionalContent(commentBody, url);
            
            Assert.False(result);
        }

        [Fact]
        public void ContainsPromotionalContent_WithDifferentUrl_ShouldReturnFalse()
        {
            var commentBody = "Some text [link](https://other.com)";
            var url = "https://example.com";
            
            var result = _formatter.ContainsPromotionalContent(commentBody, url);
            
            Assert.False(result);
        }

        [Fact]
        public void ContainsPromotionalContent_WithEmptyBody_ShouldReturnFalse()
        {
            var result = _formatter.ContainsPromotionalContent("", "https://example.com");
            
            Assert.False(result);
        }

        [Fact]
        public void ContainsPromotionalContent_WithNullBody_ShouldReturnFalse()
        {
            var result = _formatter.ContainsPromotionalContent(null!, "https://example.com");
            
            Assert.False(result);
        }

        [Fact]
        public void ContainsPromotionalContent_WithEmptyUrl_ShouldReturnFalse()
        {
            var result = _formatter.ContainsPromotionalContent("Some text", "");
            
            Assert.False(result);
        }

        [Fact]
        public void ContainsPromotionalContent_WithNullUrl_ShouldReturnFalse()
        {
            var result = _formatter.ContainsPromotionalContent("Some text", null!);
            
            Assert.False(result);
        }

        [Fact]
        public void ContainsPromotionalContent_IsCaseInsensitive()
        {
            var commentBody = "Some text [link](HTTPS://EXAMPLE.COM)";
            var url = "https://example.com";
            
            var result = _formatter.ContainsPromotionalContent(commentBody, url);
            
            Assert.True(result);
        }

        [Fact]
        public void ContainsPromotionalContent_WithPartialUrlMatch_ShouldReturnTrue()
        {
            var commentBody = "Check out https://example.com/specific/path for more";
            var url = "https://example.com";
            
            var result = _formatter.ContainsPromotionalContent(commentBody, url);
            
            Assert.True(result);
        }

        #endregion
    }
}
