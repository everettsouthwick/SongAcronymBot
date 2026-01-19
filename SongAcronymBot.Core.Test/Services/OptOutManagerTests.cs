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
    public class OptOutManagerTests
    {
        private readonly Mock<IOptedOutRedditorRepository> _mockRepository;
        private readonly Mock<ILogger<OptOutManager>> _mockLogger;
        private readonly OptOutManager _manager;

        public OptOutManagerTests()
        {
            _mockRepository = new Mock<IOptedOutRedditorRepository>();
            _mockLogger = new Mock<ILogger<OptOutManager>>();
            _manager = new OptOutManager(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task RefreshOptedOutUsersAsync_ShouldLoadUsernamesFromRepository()
        {
            // Arrange
            var usernames = new HashSet<string> { "user1", "user2", "user3" };
            _mockRepository.Setup(r => r.GetAllUsernamesAsync())
                .ReturnsAsync(usernames);

            // Act
            await _manager.RefreshOptedOutUsersAsync();

            // Assert
            Assert.Equal(3, _manager.OptedOutCount);
            Assert.True(_manager.IsOptedOut("user1"));
            Assert.True(_manager.IsOptedOut("user2"));
            Assert.True(_manager.IsOptedOut("user3"));
        }

        [Fact]
        public async Task IsOptedOut_BeforeRefresh_ShouldReturnFalse()
        {
            // Act - no refresh called
            var result = _manager.IsOptedOut("anyuser");

            // Assert
            Assert.False(result);
            await Task.CompletedTask;
        }

        [Fact]
        public async Task IsOptedOut_AfterRefresh_ShouldReturnTrueForOptedOutUser()
        {
            // Arrange
            var usernames = new HashSet<string> { "optedoutuser" };
            _mockRepository.Setup(r => r.GetAllUsernamesAsync())
                .ReturnsAsync(usernames);

            await _manager.RefreshOptedOutUsersAsync();

            // Act
            var result = _manager.IsOptedOut("optedoutuser");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsOptedOut_AfterRefresh_ShouldReturnFalseForNonOptedOutUser()
        {
            // Arrange
            var usernames = new HashSet<string> { "optedoutuser" };
            _mockRepository.Setup(r => r.GetAllUsernamesAsync())
                .ReturnsAsync(usernames);

            await _manager.RefreshOptedOutUsersAsync();

            // Act
            var result = _manager.IsOptedOut("normaluser");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOptedOutRedditorAsync_WhenNotExisting_ShouldCreateAndRefresh()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByUsernameAsync("newuser"))
                .ReturnsAsync((OptedOutRedditor?)null);
            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<OptedOutRedditor>()))
                .ReturnsAsync((OptedOutRedditor r) => r);
            _mockRepository.Setup(r => r.GetAllUsernamesAsync())
                .ReturnsAsync(new HashSet<string> { "newuser" });

            // Act
            await _manager.AddOptedOutRedditorAsync("newuser");

            // Assert
            _mockRepository.Verify(r => r.CreateAsync(It.Is<OptedOutRedditor>(o => o.Username == "newuser")), Times.Once);
            _mockRepository.Verify(r => r.GetAllUsernamesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddOptedOutRedditorAsync_WhenAlreadyExists_ShouldNotCreate()
        {
            // Arrange
            var existingUser = new OptedOutRedditor { Id = Guid.NewGuid(), Username = "existinguser" };
            _mockRepository.Setup(r => r.GetByUsernameAsync("existinguser"))
                .ReturnsAsync(existingUser);

            // Act
            await _manager.AddOptedOutRedditorAsync("existinguser");

            // Assert
            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<OptedOutRedditor>()), Times.Never);
        }

        [Fact]
        public async Task RemoveOptedOutRedditorAsync_WhenExists_ShouldDeleteAndRefresh()
        {
            // Arrange
            var existingUser = new OptedOutRedditor { Id = Guid.NewGuid(), Username = "existinguser" };
            _mockRepository.Setup(r => r.GetByUsernameAsync("existinguser"))
                .ReturnsAsync(existingUser);
            _mockRepository.Setup(r => r.DeleteAsync(existingUser.Id))
                .ReturnsAsync(true);
            _mockRepository.Setup(r => r.GetAllUsernamesAsync())
                .ReturnsAsync(new HashSet<string>());

            // Act
            await _manager.RemoveOptedOutRedditorAsync("existinguser");

            // Assert
            _mockRepository.Verify(r => r.DeleteAsync(existingUser.Id), Times.Once);
            _mockRepository.Verify(r => r.GetAllUsernamesAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveOptedOutRedditorAsync_WhenNotExists_ShouldNotDelete()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByUsernameAsync("nonexistentuser"))
                .ReturnsAsync((OptedOutRedditor?)null);

            // Act
            await _manager.RemoveOptedOutRedditorAsync("nonexistentuser");

            // Assert
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<object>()), Times.Never);
        }
    }
}
