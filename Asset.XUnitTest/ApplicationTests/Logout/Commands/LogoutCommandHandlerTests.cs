#region
using Asset.Application.Features.Auth.Commands.Logout;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Identity;
using FluentAssertions;
using Moq;
using RefreshTokenEntity = Asset.Domain.Identity.RefreshToken;
#endregion

namespace Asset.XUnitTest.ApplicationTests.Logout.Commands
{
    public class LogoutCommandHandlerTests
    {
        #region Setup
        private readonly Mock<IRefreshTokenRepository> _refreshTokensMock = new();
        private readonly Mock<ICurrentUserService> _currentUserMock = new();
        private readonly Mock<IIdentityUnitOfWork> _unitOfWorkMock = new();

        private readonly LogoutCommandHandler _handler;
        private readonly CancellationToken _ct = CancellationToken.None;

        private const string CurrentUserId = "user-1";

        public LogoutCommandHandlerTests()
        {
            _currentUserMock.Setup(x => x.UserId).Returns(CurrentUserId);

            _handler = new LogoutCommandHandler(_refreshTokensMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object);
        }

        private static RefreshTokenEntity StoredToken(string userId = CurrentUserId, bool isRevoked = false) => new()
        {
            Id = 1,
            UserId = userId,
            TokenHash = "hashed-refresh-token",
            IsRevoked = isRevoked,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        #endregion

        [Fact]
        public async Task Logout_Should_RevokeToken_When_Token_Exists_And_BelongsToCurrentUser()
        {
            // Arrange
            var request = new LogoutCommand("hashed-refresh-token");
            var stored = StoredToken();

            _refreshTokensMock
                .Setup(x => x.GetByTokenHashAsync(request.RefreshToken, _ct))
                .ReturnsAsync(stored);

            // Act
            await _handler.Handle(request, _ct);

            // Assert — أهم سطر في التست كله
            stored.IsRevoked.Should().BeTrue();

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_ct), Times.Once);
        }

        [Fact]
        public async Task Logout_Should_DoNothing_When_Token_DoesNotExist()
        {
            // Arrange
            var request = new LogoutCommand("unknown-token-hash");

            _refreshTokensMock
                .Setup(x => x.GetByTokenHashAsync(request.RefreshToken, _ct))
                .ReturnsAsync((RefreshTokenEntity?)null);

            // Act
            await _handler.Handle(request, _ct);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Logout_Should_DoNothing_When_Token_IsAlreadyRevoked()
        {
            // Arrange
            var request = new LogoutCommand("hashed-refresh-token");
            var stored = StoredToken(isRevoked: true);

            _refreshTokensMock
                .Setup(x => x.GetByTokenHashAsync(request.RefreshToken, _ct))
                .ReturnsAsync(stored);

            // Act
            await _handler.Handle(request, _ct);

            // Assert
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Logout_Should_DoNothing_When_Token_BelongsToAnotherUser()
        {
            // Arrange
            // توكن حقيقي وصالح، بس بتاع يوزر تاني غير اللي بعت الريكوست
            var request = new LogoutCommand("hashed-refresh-token");
            var stored = StoredToken(userId: "some-other-user-id");

            _refreshTokensMock
                .Setup(x => x.GetByTokenHashAsync(request.RefreshToken, _ct))
                .ReturnsAsync(stored);

            // Act
            await _handler.Handle(request, _ct);

            // Assert — التوكن ده يفضل زي ما هو، مايتلمسش
            stored.IsRevoked.Should().BeFalse();

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}