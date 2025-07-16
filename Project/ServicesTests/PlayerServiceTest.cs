using AutoMapper;
using Core.Models;
using Core.Models.DTOs.Auth;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Player;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Services;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ServicesTests
{
    public class PlayerServiceTest
    {
        private readonly Mock<IPlayerRepository> _playerRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly PlayerService _playerService;

        public PlayerServiceTest()
        {
            _playerRepositoryMock = new Mock<IPlayerRepository>();
            _mapperMock = new Mock<IMapper>();
            _configurationMock = new Mock<IConfiguration>();

            _configurationMock.Setup(x => x["Jwt:Key"]).Returns("test-key-that-is-at-least-32-characters-long");
            _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("TestIssuer");
            _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("TestAudience");

            _playerService = new PlayerService(_playerRepositoryMock.Object, _mapperMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_NewUsername_ReturnsSuccessAndToken()
        {
            var request = new RegisterRequest { Username = "newuser", Password = "password123" };
            var createdPlayer = new Player { Id = Guid.NewGuid(), Username = "newuser" };

            _playerRepositoryMock.Setup(x => x.GetPlayerByUsernameAsync("newuser")).ReturnsAsync((Player?)null);
            _playerRepositoryMock.Setup(x => x.AddPlayerAsync(It.IsAny<Player>())).ReturnsAsync(createdPlayer);

            var result = await _playerService.RegisterAsync(request);

            Assert.True(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.Data));
        }

        [Theory]
        [InlineData("existinguser", 409, "Игрок уже существует")]
        [InlineData(null, 500, "Database error")]
        public async Task RegisterAsync_FailureCases_ReturnsExpectedError(string? existingUsername, int expectedCode, string expectedMessage)
        {
            var request = new RegisterRequest { Username = "existinguser", Password = "password123" };

            if (existingUsername == null)
                _playerRepositoryMock.Setup(x => x.GetPlayerByUsernameAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Database error"));
            else
                _playerRepositoryMock.Setup(x => x.GetPlayerByUsernameAsync(existingUsername))
                    .ReturnsAsync(existingUsername == "existinguser" ? new Player { Id = Guid.NewGuid(), Username = existingUsername } : null);

            var result = await _playerService.RegisterAsync(request);

            Assert.False(result.Success);
            Assert.Equal(expectedCode, result.ErrorCode);
            Assert.Equal(expectedMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task AuthenticateAsync_ValidAndInvalidCases_ReturnsExpectedResults()
        {
            var correctPassword = "password123";
            var wrongPassword = "wrongpassword";
            var player = new Player
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = HashPassword(correctPassword)
            };

            _playerRepositoryMock.Setup(x => x.GetPlayerByUsernameAsync("testuser")).ReturnsAsync(player);

            _playerRepositoryMock.Setup(x => x.GetPlayerByUsernameAsync("nonexistent")).ReturnsAsync((Player?)null);

            _playerRepositoryMock.Setup(x => x.GetPlayerByUsernameAsync("dberror")).ThrowsAsync(new Exception("Database error"));

            var resultSuccess = await _playerService.AuthenticateAsync(new LoginRequest { Username = "testuser", Password = correctPassword });
            Assert.True(resultSuccess.Success);
            Assert.False(string.IsNullOrWhiteSpace(resultSuccess.Data));

            var resultNotFound = await _playerService.AuthenticateAsync(new LoginRequest { Username = "nonexistent", Password = correctPassword });
            Assert.False(resultNotFound.Success);
            Assert.Equal(404, resultNotFound.ErrorCode);
            Assert.Equal("Игрок не найден", resultNotFound.ErrorMessage);

            var resultWrongPassword = await _playerService.AuthenticateAsync(new LoginRequest { Username = "testuser", Password = wrongPassword });
            Assert.False(resultWrongPassword.Success);
            Assert.Equal(401, resultWrongPassword.ErrorCode);
            Assert.Equal("Неверный пароль", resultWrongPassword.ErrorMessage);

            var resultException = await _playerService.AuthenticateAsync(new LoginRequest { Username = "dberror", Password = correctPassword });
            Assert.False(resultException.Success);
            Assert.Equal(500, resultException.ErrorCode);
            Assert.Equal("Database error", resultException.ErrorMessage);
        }

        [Fact]
        public async Task GetPlayerAsync_ValidAndInvalidCases_ReturnsExpectedResults()
        {
            var playerId = Guid.NewGuid();
            var player = new Player { Id = playerId, Username = "testuser" };
            var playerResponse = new PlayerResponse { Id = playerId, Username = "testuser" };

            _playerRepositoryMock.Setup(x => x.GetPlayerAsync(playerId)).ReturnsAsync(player);
            _playerRepositoryMock.Setup(x => x.GetPlayerAsync(It.Is<Guid>(id => id != playerId))).ReturnsAsync((Player?)null);
            _playerRepositoryMock.Setup(x => x.GetPlayerAsync(It.Is<Guid>(id => id == Guid.Empty))).ThrowsAsync(new Exception("Database error"));

            _mapperMock.Setup(x => x.Map<PlayerResponse>(player)).Returns(playerResponse);

            // Existing player
            var resultFound = await _playerService.GetPlayerAsync(playerId);
            Assert.True(resultFound.Success);
            Assert.Equal(playerResponse, resultFound.Data);

            // Not found
            var resultNotFound = await _playerService.GetPlayerAsync(Guid.NewGuid());
            Assert.False(resultNotFound.Success);
            Assert.Equal(404, resultNotFound.ErrorCode);
            Assert.Equal("Игрок не найден", resultNotFound.ErrorMessage);

            // Exception
            var resultException = await _playerService.GetPlayerAsync(Guid.Empty);
            Assert.False(resultException.Success);
            Assert.Equal(500, resultException.ErrorCode);
            Assert.Equal("Database error", resultException.ErrorMessage);
        }


        public static IEnumerable<object?[]> UsernameTestData =>
            new List<object?[]>
            {
                new object?[] { null },
                new object?[] { new string[] { } },
                new object?[] { new string[] { "admin", "guest" } }
            };

        [Theory]
        [MemberData(nameof(UsernameTestData))]
        public async Task SearchPlayersAsync_VariousUsernameFilters_ReturnsExpectedResults(string[]? usernames)
        {
            var usernameList = usernames?.ToList();

            var request = new PlayerSearchRequest
            {
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 },
                Filter = usernames == null ? null : new PlayerFilterRequest { Usernames = usernameList }
            };

            var players = new PagedResponse<Player>
            {
                Items = new List<Player> { new Player { Id = Guid.NewGuid(), Username = usernames?.FirstOrDefault() ?? "user1" } },
                TotalCount = 1,
                PageSize = 10,
                PageNumber = 1
            };

            var playerResponses = new List<PlayerResponse>
    {
        new PlayerResponse { Id = players.Items.First().Id, Username = players.Items.First().Username }
    };

            _playerRepositoryMock
                .Setup(x => x.SearchPlayersAsync(
                    1,
                    10,
                    It.Is<List<string>>(l => usernames == null ? l == null : l.SequenceEqual(usernameList ?? new List<string>())),
                    It.IsAny<List<(string, SortDirection)>>()))
                .ReturnsAsync(players);

            _mapperMock
                .Setup(x => x.Map<List<PlayerResponse>>(players.Items))
                .Returns(playerResponses);

            var result = await _playerService.SearchPlayersAsync(request);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data.TotalCount);
            Assert.Equal(playerResponses, result.Data.Items);
        }

        [Fact]
        public async Task SearchPlayersAsync_RepositoryException_ReturnsError500()
        {
            var request = new PlayerSearchRequest
            {
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 }
            };

            _playerRepositoryMock
                .Setup(x => x.SearchPlayersAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<List<(string, SortDirection)>>()))
                .ThrowsAsync(new Exception("Database error"));

            var result = await _playerService.SearchPlayersAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.ErrorCode);
            Assert.Equal("Database error", result.ErrorMessage);
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
