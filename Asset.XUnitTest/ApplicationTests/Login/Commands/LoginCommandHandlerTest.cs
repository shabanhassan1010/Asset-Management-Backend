using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Features.Auth.Commands.Login;
using Asset.Application.Features.Auth.DTOs;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Enum;
using Asset.Domain.Exceptions;
using Asset.Domain.Identity;
using AutoMapper;
using FluentAssertions;
using Moq;
using RefreshTokenEntity = Asset.Domain.Identity.RefreshToken;


namespace Asset.XUnitTest.ApplicationTests.Login.Commands
{
    public class LoginCommandHandlerTest
    {
        #region Fields
        private readonly Mock<IUserRepository> _usersMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokensMock;
        private readonly Mock<IJwtTokenService> _tokenServiceMock;
        private readonly Mock<ITokenHasher> _tokenHasherMock;
        private readonly Mock<IIdentityUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IMapper> _mapperMock;

        private readonly LoginCommandHandler _handler;
        private readonly CancellationToken _ct = CancellationToken.None;
        private const string BadCredentials = "The username or password is incorrect.";
        private const string DisabledAccount = "This account has been disabled by an administrator.";

        #endregion

        #region Constructor
        public LoginCommandHandlerTest()
        {
            _usersMock = new();
            _refreshTokensMock = new();
            _tokenServiceMock = new();
            _tokenHasherMock = new();
            _unitOfWorkMock = new();
            _mapperMock = new();

            _handler = new LoginCommandHandler(_usersMock.Object, _refreshTokensMock.Object, _tokenServiceMock.Object, _tokenHasherMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
        }
        #endregion

        #region Login
        [Fact]
        public async Task Login_Should_Return_AuthResponseDto_When_UserName_And_Password_Are_Valid()
        {
            // Arrange
            var request = new LoginCommand("admin", "Admin123");

            var Activeuser = new ApplicationUser
            {
                Id = "user-1", UserName = "admin", IsActive = true , EmployeeId = 1 , Email = "Shabanhassan1010@gmail.com"
            };

            var role = Role.Admin;
            var accessToken  = new AccessTokenResult("access-token-value", DateTime.UtcNow.AddMinutes(30));
            var refreshToken = new RefreshTokenResult("refresh-token-row", DateTime.UtcNow.AddDays(7));

            var expectedUserDto = new CurrentUserDto
            {
                UserName = "admin" , EmployeeCode= "SW", Email = "Shabanhassan1010@gmail.com" , DepartmentName = "Software", EmployeeName="Shaban" ,IsActive = true 
            };

            _usersMock.Setup(x => x.GetByUserNameAsync( request.UserName, _ct)).ReturnsAsync(Activeuser);
            _usersMock.Setup(x => x.CheckPasswordAsync(Activeuser, request.Password, _ct)).ReturnsAsync(true);
            _usersMock.Setup(x => x.GetRoleAsync(Activeuser, _ct)).ReturnsAsync(role);

            _tokenServiceMock.Setup(x => x.CreateAccessToken(Activeuser, role)).Returns(accessToken);
            _tokenServiceMock.Setup(x => x.CreateRefreshToken()).Returns(refreshToken);

            _tokenHasherMock.Setup(x => x.Hash(refreshToken.Token)).Returns("hashed-refresh-token");

            _mapperMock.Setup(x => x.Map<CurrentUserDto>(It.IsAny<UserWithRole>())).Returns(expectedUserDto);


            // Act
            var result = await _handler.Handle(request, _ct);

            // Assert          
            result.AccessToken.Should().Be("access-token-value");
            result.RefreshToken.Should().Be("refresh-token-row");
            result.AccessTokenExpiresAtUtc.Should().Be(accessToken.ExpiresAtUtc);
            result.RefreshTokenExpiresAtUtc.Should().Be(refreshToken.ExpiresAtUtc);
            result.User.Should().BeSameAs(expectedUserDto);

            // Verify user lookup
            _usersMock.Verify( x => x.GetByUserNameAsync( request.UserName, _ct),Times.Once);

            // Verify password
            _usersMock.Verify( x => x.CheckPasswordAsync(Activeuser, request.Password,_ct), Times.Once);

            // Verify role
            _usersMock.Verify(x => x.GetRoleAsync(Activeuser, _ct),Times.Once);

            // Verify Access Token
            _tokenServiceMock.Verify( x => x.CreateAccessToken(Activeuser, role), Times.Once);

            // Verify Refresh Token
            _tokenServiceMock.Verify( x => x.CreateRefreshToken(),Times.Once);

            // Verify Refresh Token was saved in Database
            _refreshTokensMock.Verify(x => x.AddAsync(It.Is<RefreshTokenEntity>(t =>
                            t.UserId == Activeuser.Id &&
                            t.TokenHash != refreshToken.Token &&   
                            t.ExpiresAt == refreshToken.ExpiresAtUtc &&
                            t.IsRevoked == false), _ct),
            Times.Once);

            // Verify SaveChanges
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(_ct),Times.Once);
        }

        [Fact]
        public async Task Login_Should_Throw_AuthenticationFailedException_When_User_Does_Not_Exist()
        {
            // Arrange
            var request = new LoginCommand("admin", "Admin123");

            _usersMock.Setup(x => x.GetByUserNameAsync(request.UserName,_ct))
                      .ReturnsAsync((ApplicationUser?)null);

            // Act
            var action = async () => await _handler.Handle(request, _ct);

            // Assert
            await action.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(BadCredentials);

            // These operations should never happen
            _usersMock.Verify(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(),It.IsAny<string>(), It.IsAny<CancellationToken>()),Times.Never);
            VerifyNothingWasIssued();
        }

        [Fact]
        public async Task Login_Should_Throw_AuthenticationFailedException_When_Password_Is_Wrong()
        {
            // Arrange
            var request = new LoginCommand("admin", "Admin123");

            var user = new ApplicationUser
            {
                Id = "user-1", UserName = "admin", IsActive = true
            };

            _usersMock.Setup(x => x.GetByUserNameAsync(request.UserName,_ct)).ReturnsAsync(user);
            _usersMock.Setup(x => x.CheckPasswordAsync(user, request.Password,_ct)).ReturnsAsync(false);

            // Act
            var action = async () => await _handler.Handle(request, _ct);

            // Assert
            await action.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(BadCredentials);
            VerifyNothingWasIssued();
        }

        [Fact]
        public async Task Handle_Should_Throw_AuthenticationFailed_When_AccountIsDisabled()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-1", UserName = "admin", IsActive = true
            };
            user.IsActive = false;

            var request = new LoginCommand(user.UserName, "P@ssw0rd");

            _usersMock.Setup(x => x.GetByUserNameAsync(request.UserName, _ct)).ReturnsAsync(user);
            _usersMock.Setup(x => x.CheckPasswordAsync(user, request.Password, _ct)).ReturnsAsync(true);

            // Act
            var act = async () => await _handler.Handle(request, _ct);

            await act.Should().ThrowAsync<AuthenticationFailedException>().WithMessage(DisabledAccount);
            VerifyNothingWasIssued();
        }

        #endregion

        #region Private Methods
        // In Case any fail: any token must not make token or save it in database
        private void VerifyNothingWasIssued()
        {
            _tokenServiceMock.Verify(x => x.CreateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<Role>()), Times.Never);
            _tokenServiceMock.Verify(x => x.CreateRefreshToken(), Times.Never);
            _refreshTokensMock.Verify(x => x.AddAsync(It.IsAny<RefreshTokenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
        #endregion
    }
}