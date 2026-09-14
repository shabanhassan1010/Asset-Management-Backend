#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Features.Auth.Commands.Refresh;
using Asset.Application.Features.Auth.DTOs;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Enum;
using Asset.Domain.Exceptions;
using Asset.Domain.Identity;
using AutoMapper;
using FluentAssertions;
using Moq;
using RefreshTokenEntity = Asset.Domain.Identity.RefreshToken;
#endregion

namespace Asset.XUnitTest.ApplicationTests.RefreshToken
{
    public class RefreshTokenCommandHandlerTests
    {
        #region Setup
        private readonly Mock<IUserRepository> _usersMock = new();
        private readonly Mock<IRefreshTokenRepository> _refreshTokensMock = new();
        private readonly Mock<IJwtTokenService> _tokenServiceMock = new();
        private readonly Mock<ITokenHasher> _tokenHasherMock = new();
        private readonly Mock<IIdentityUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IMapper> _mapperMock = new();

        private readonly RefreshTokenCommandHandler _handler;
        private readonly CancellationToken _ct = CancellationToken.None;

        private const string InvalidMessage = "The refresh token is invalid or has expired. Please sign in again.";
        private const string IncomingRawToken = "incoming-raw-refresh-token";
        private const string IncomingHash = "incoming-hash";

        public RefreshTokenCommandHandlerTests()
        {
            _tokenHasherMock.Setup(x => x.Hash(IncomingRawToken)).Returns(IncomingHash);

            _handler = new RefreshTokenCommandHandler(_refreshTokensMock.Object, _tokenHasherMock.Object, _usersMock.Object, _tokenServiceMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
        }

        private static RefreshTokenEntity StoredToken( string userId = "user-1", bool isRevoked = false, DateTime? expiresAt = null) => new()
        {
              Id = 1,
              UserId = userId,
              TokenHash = IncomingHash,
              IsRevoked = isRevoked,
              ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7)
        };

        private static ApplicationUser ActiveUser(string id = "user-1") => new()
        {
            Id = id,
            UserName = "shaban",
            IsActive = true
        };

        // كل حالات الفشل غير الـ reuse لازم يبقوا سواء: مافيش أي كتابة في الداتابيز
        private void VerifyNoRotationHappened()
        {
            _refreshTokensMock.Verify(x => x.AddAsync(
                It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);

            _tokenServiceMock.Verify(x => x.CreateAccessToken(
                It.IsAny<ApplicationUser>(), It.IsAny<Role>()), Times.Never);
        }

        #endregion

        [Fact]
        public async Task RefreshToken_Should_Throw_When_Token_DoesNotExist()
        {
            // Arrange
            var request = new RefreshTokenCommand(IncomingRawToken);

            _refreshTokensMock.Setup(x => x.GetByTokenHashAsync(IncomingHash, _ct))
                              .ReturnsAsync((RefreshTokenEntity?)null);

            // Act
            var act = async () => await _handler.Handle(request, _ct);

            // Assert
            await act.Should().ThrowAsync<AuthenticationFailedException>()
                     .WithMessage(InvalidMessage);

            _refreshTokensMock.Verify(x => x.RevokeAllForUserAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            VerifyNoRotationHappened();
        }

        [Fact]
        public async Task RefreshToken_Should_RevokeAllUserTokens_When_ReusedTokenIsAlreadyRevoked()
        {
            // Arrange — ده الـ theft-detection scenario: حد بيحاول يستخدم توكن اتلغى قبل كده
            var request = new RefreshTokenCommand(IncomingRawToken);
            var stored = StoredToken(userId: "user-1", isRevoked: true);

            _refreshTokensMock
                .Setup(x => x.GetByTokenHashAsync(IncomingHash, _ct))
                .ReturnsAsync(stored);

            // Act
            var act = async () => await _handler.Handle(request, _ct);

            // Assert
            await act.Should().ThrowAsync<AuthenticationFailedException>()
                     .WithMessage(InvalidMessage);

            // أهم اتنين سطر في الملف ده: لازم يلغي كل توكنات نفس اليوزر ويحفظهم قبل ما يرمي
            _refreshTokensMock.Verify(x => x.RevokeAllForUserAsync(stored.UserId, _ct), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_ct), Times.Once);

            VerifyNoRotationHappened();
        }

        [Fact]
        public async Task RefreshToken_Should_Throw_When_TokenIsExpired()
        {
            // Arrange
            var request = new RefreshTokenCommand(IncomingRawToken);
            var stored = StoredToken(expiresAt: DateTime.UtcNow.AddMinutes(-1));

            _refreshTokensMock
                .Setup(x => x.GetByTokenHashAsync(IncomingHash, _ct))
                .ReturnsAsync(stored);

            // Act
            var act = async () => await _handler.Handle(request, _ct);

            // Assert
            await act.Should().ThrowAsync<AuthenticationFailedException>()
                     .WithMessage(InvalidMessage);

            // مهم: توكن منتهي مش نفسه توكن معاد استخدامه — مايستحقش عملية الـ revoke-all
            _refreshTokensMock.Verify(x => x.RevokeAllForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            VerifyNoRotationHappened();
        }

        [Fact]
        public async Task RefreshToken_Should_Throw_When_UserNoLongerExists()
        {
            // Arrange
            var request = new RefreshTokenCommand(IncomingRawToken);
            var stored = StoredToken();

            _refreshTokensMock.Setup(x => x.GetByTokenHashAsync(IncomingHash, _ct)).ReturnsAsync(stored);
            _usersMock.Setup(x => x.GetByIdAsync(stored.UserId, _ct)).ReturnsAsync((ApplicationUser?)null);

            // Act
            var act = async () => await _handler.Handle(request, _ct);

            // Assert
            await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(InvalidMessage);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            VerifyNoRotationHappened();
        }

        [Fact]
        public async Task RefreshToken_Should_Throw_When_UserIsDisabled()
        {
            // Arrange
            var request = new RefreshTokenCommand(IncomingRawToken);
            var stored = StoredToken();
            var user = ActiveUser(stored.UserId);
            user.IsActive = false;

            _refreshTokensMock.Setup(x => x.GetByTokenHashAsync(IncomingHash, _ct)).ReturnsAsync(stored);
            _usersMock.Setup(x => x.GetByIdAsync(stored.UserId, _ct)).ReturnsAsync(user);

            // Act
            var act = async () => await _handler.Handle(request, _ct);

            // Assert
            await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(InvalidMessage);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
            VerifyNoRotationHappened();
        }

        [Fact]
        public async Task RefreshToken_Should_RotateToken_When_RequestIsValid()
        {
            // Arrange
            var request = new RefreshTokenCommand(IncomingRawToken);
            var stored = StoredToken();
            var user = ActiveUser(stored.UserId);
            const Role role = Role.Admin;

            var accessToken = new AccessTokenResult("new-access-token", DateTime.UtcNow.AddMinutes(15));
            var newRefreshToken = new RefreshTokenResult("new-raw-refresh-token", DateTime.UtcNow.AddDays(7));
            const string newHash = "new-token-hash";

            var expectedUserDto = new CurrentUserDto {UserName = user.UserName, Role = role };

            _refreshTokensMock.Setup(x => x.GetByTokenHashAsync(IncomingHash, _ct)).ReturnsAsync(stored);
            _usersMock.Setup(x => x.GetByIdAsync(stored.UserId, _ct)).ReturnsAsync(user);
            _usersMock.Setup(x => x.GetRoleAsync(user, _ct)).ReturnsAsync(role);

            _tokenServiceMock.Setup(x => x.CreateAccessToken(user, role)).Returns(accessToken);
            _tokenServiceMock.Setup(x => x.CreateRefreshToken()).Returns(newRefreshToken);
            _tokenHasherMock.Setup(x => x.Hash(newRefreshToken.Token)).Returns(newHash);

            _mapperMock.Setup(x => x.Map<CurrentUserDto>(It.IsAny<UserWithRole>())).Returns(expectedUserDto);

            // Act
            var result = await _handler.Handle(request, _ct);

            // Assert — الرد اللي رايح للفرونت
            result.AccessToken.Should().Be("new-access-token");
            result.RefreshToken.Should().Be("new-raw-refresh-token");   // الخام، مش الـ hash
            result.User.Should().BeSameAs(expectedUserDto);

            // Assert — التوكن القديم اتقفل وربطناه باللي جاي بدل منه (audit chain)
            stored.IsRevoked.Should().BeTrue();
            stored.ReplacedByTokenHash.Should().Be(newHash);

            // Assert — التوكن الجديد المخزّن هو الـ hash، مش الخام
            _refreshTokensMock.Verify(x => x.AddAsync(
                It.Is<RefreshTokenEntity>(t =>
                    t.UserId == user.Id &&
                    t.TokenHash == newHash &&
                    t.TokenHash != newRefreshToken.Token &&
                    t.IsRevoked == false),
                _ct),
                Times.Once);

            // الـ theft-detection متنداش هنا — ده مش reuse، ده استخدام سليم
            _refreshTokensMock.Verify(x => x.RevokeAllForUserAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

            // الاتنين لازم يتحفظوا معًا في نفس الـ SaveChangesAsync
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_ct), Times.Once);
        }
    }
}