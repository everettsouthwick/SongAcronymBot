using SongAcronymBot.Core.Model;
using SongAcronymBot.Domain.Supabase.Models;
using Xunit;

namespace SongAcronymBot.Core.Test.Model
{
    public class AcronymMatchTests
    {
        [Fact]
        public void Constructor_WithEnrichedAcronym_Album_ShouldFormatCorrectly()
        {
            // Arrange
            var acronym = new EnrichedAcronym
            {
                Id = Guid.NewGuid(),
                AcronymText = "TPAB",
                AcronymType = AcronymType.Album,
                ArtistName = "Kendrick Lamar",
                ArtistSlug = "kendrick-lamar",
                AlbumName = "To Pimp a Butterfly",
                AlbumSlug = "to-pimp-a-butterfly",
                YearReleased = 2015
            };

            // Act
            var match = new AcronymMatch(acronym, 1);

            // Assert
            Assert.Equal("TPAB", match.Acronym);
            Assert.Equal(1, match.Position);
            Assert.Contains("TPAB", match.CommentBody);
            Assert.Contains("To Pimp a Butterfly", match.CommentBody);
            Assert.Contains("2015", match.CommentBody);
            Assert.Contains("Kendrick Lamar", match.CommentBody);
            Assert.Contains("album", match.CommentBody!.ToLower());
        }

        [Fact]
        public void Constructor_WithEnrichedAcronym_Track_ShouldFormatCorrectly()
        {
            // Arrange
            var acronym = new EnrichedAcronym
            {
                Id = Guid.NewGuid(),
                AcronymText = "DNA",
                AcronymType = AcronymType.Track,
                ArtistName = "Kendrick Lamar",
                ArtistSlug = "kendrick-lamar",
                AlbumName = "DAMN.",
                AlbumSlug = "damn",
                TrackName = "DNA.",
                YearReleased = 2017
            };

            // Act
            var match = new AcronymMatch(acronym, 2);

            // Assert
            Assert.Equal("DNA", match.Acronym);
            Assert.Equal(2, match.Position);
            Assert.Contains("DNA", match.CommentBody);
            Assert.Contains("DNA.", match.CommentBody); // Track name
            Assert.Contains("track", match.CommentBody!.ToLower());
        }

        [Fact]
        public void Constructor_WithEnrichedAcronym_Single_ShouldFormatCorrectly()
        {
            // Arrange
            var acronym = new EnrichedAcronym
            {
                Id = Guid.NewGuid(),
                AcronymText = "TEST",
                AcronymType = AcronymType.Single,
                ArtistName = "Test Artist",
                ArtistSlug = "test-artist",
                TrackName = "Test Single",
                IsSingle = true
            };

            // Act
            var match = new AcronymMatch(acronym, 3);

            // Assert
            Assert.Equal("TEST", match.Acronym);
            Assert.Contains("single", match.CommentBody!.ToLower());
            Assert.Contains("Test Single", match.CommentBody);
        }

        [Fact]
        public void Constructor_WithEnrichedAcronym_Artist_ShouldFormatCorrectly()
        {
            // Arrange
            var acronym = new EnrichedAcronym
            {
                Id = Guid.NewGuid(),
                AcronymText = "TDE",
                AcronymType = AcronymType.Artist,
                ArtistName = "Top Dawg Entertainment",
                ArtistSlug = "top-dawg-entertainment"
            };

            // Act
            var match = new AcronymMatch(acronym, 1);

            // Assert
            Assert.Equal("TDE", match.Acronym);
            Assert.Contains("Top Dawg Entertainment", match.CommentBody);
        }

        [Fact]
        public void Constructor_WithUnknownAcronym_ShouldFormatSuggestionLink()
        {
            // Arrange
            var acronymName = "UNKNOWNACRONYM";
            var index = 1;

            // Act
            var match = new AcronymMatch(acronymName, index);

            // Assert
            Assert.Equal("UNKNOWNACRONYM", match.Acronym);
            Assert.Equal(1, match.Position);
            Assert.Contains("not recognized", match.CommentBody);
            Assert.Contains("Click here", match.CommentBody);
            Assert.Contains("suggest", match.CommentBody!.ToLower());
        }

        [Fact]
        public void Constructor_WithArtistSlug_ShouldCreateLinks()
        {
            // Arrange
            var acronym = new EnrichedAcronym
            {
                Id = Guid.NewGuid(),
                AcronymText = "TEST",
                AcronymType = AcronymType.Album,
                ArtistName = "Test Artist",
                ArtistSlug = "test-artist",
                AlbumName = "Test Album",
                AlbumSlug = "test-album",
                YearReleased = 2023
            };

            // Act
            var match = new AcronymMatch(acronym, 1);

            // Assert
            Assert.Contains("myartistradar.com/artists/test-artist", match.CommentBody);
            Assert.Contains("myartistradar.com/artists/test-artist/test-album", match.CommentBody);
        }

        [Fact]
        public void Constructor_WithoutSlugs_ShouldNotCreateLinks()
        {
            // Arrange
            var acronym = new EnrichedAcronym
            {
                Id = Guid.NewGuid(),
                AcronymText = "TEST",
                AcronymType = AcronymType.Album,
                ArtistName = "Test Artist",
                ArtistSlug = null,
                AlbumName = "Test Album",
                AlbumSlug = null,
                YearReleased = 2023
            };

            // Act
            var match = new AcronymMatch(acronym, 1);

            // Assert
            Assert.DoesNotContain("myartistradar.com", match.CommentBody);
            Assert.Contains("Test Artist", match.CommentBody);
            Assert.Contains("Test Album", match.CommentBody);
        }
    }
}
