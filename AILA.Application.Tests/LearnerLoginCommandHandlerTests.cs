using AILA.Application.Common.Interfaces;
using AILA.Application.Common.Interfaces.Repositories;
using AILA.Application.Features.Authentication.Commands.LearnerLogin;
using AILA.Domain.Entities;
using AILA.Domain.Enums;
using Moq;

namespace AILA.Application.Tests;

public class LearnerLoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCredentialsAreValid_ShouldPersistRefreshToken()
    {
        var user = new User("learner@example.com", "Learner", UserRole.Learner, passwordHash: "hash");

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var userRepositoryMock = new Mock<IUserRepository>();
        var userTokenRepositoryMock = new Mock<IUserTokenRepository>();
        var passwordHasherMock = new Mock<IPasswordHasher>();
        var tokenProviderMock = new Mock<ITokenProvider>();

        userRepositoryMock
            .Setup(x => x.GetByEmailAsync("learner@example.com"))
            .ReturnsAsync(user);

        unitOfWorkMock.SetupGet(x => x.Users).Returns(userRepositoryMock.Object);
        unitOfWorkMock.SetupGet(x => x.UserTokens).Returns(userTokenRepositoryMock.Object);
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        passwordHasherMock.Setup(x => x.Verify("Password123!", "hash")).Returns(true);
        tokenProviderMock.Setup(x => x.GenerateAccessToken(user)).Returns("access-token");
        tokenProviderMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        tokenProviderMock.Setup(x => x.HashToken("refresh-token")).Returns("hashed-refresh-token");

        var handler = new LearnerLoginCommandHandler(
            unitOfWorkMock.Object,
            passwordHasherMock.Object,
            tokenProviderMock.Object);

        var result = await handler.Handle(
            new LearnerLoginCommand
            {
                Email = "learner@example.com",
                Password = "Password123!"
            },
            CancellationToken.None);

        Assert.True(result.Success);
        userTokenRepositoryMock.Verify(x => x.Add(It.Is<UserToken>(token =>
            token.UserId == user.Id && token.RefreshTokenHash == "hashed-refresh-token")), Times.Once);
        unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
