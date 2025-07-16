using AutoMapper;
using Core.Models;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Game;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Services;
using Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ServicesTests
{
    public class GameServiceTest
    {
        private readonly Mock<IGameRepository> _gameRepositoryMock;
        private readonly Mock<IPlayerRepository> _playerRepositoryMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GameService _gameService;

        public GameServiceTest()
        {
            _gameRepositoryMock = new Mock<IGameRepository>();
            _playerRepositoryMock = new Mock<IPlayerRepository>();
            _configurationMock = new Mock<IConfiguration>();
            _mapperMock = new Mock<IMapper>();

            SetupDefaultConfig();

            _gameService = new GameService(_gameRepositoryMock.Object, _playerRepositoryMock.Object, _configurationMock.Object, _mapperMock.Object);
        }

        private void SetupDefaultConfig()
        {
            _configurationMock.Setup(x => x["Game:BoardSize"]).Returns("3");
            _configurationMock.Setup(x => x["Game:WinConditionLength"]).Returns("3");
        }

        [Fact]
        public async Task AddGameAsync_SuccessfulCreation_ReturnsSuccess()
        {
            var (playerXId, playerOId, request, playerX, playerO, game, gameResponse) = CreateAddGameTestData();

            SetupPlayerMocks(playerXId, playerX, playerOId, playerO);
            _gameRepositoryMock.Setup(x => x.AddGameAsync(It.IsAny<Game>())).ReturnsAsync(game);
            _mapperMock.Setup(x => x.Map<GameResponse>(It.IsAny<Game>())).Returns(gameResponse);

            var result = await _gameService.AddGameAsync(request);

            Assert.True(result.Success);
            Assert.Equal(gameResponse, result.Data);

            _gameRepositoryMock.Verify(x => x.AddGameAsync(It.Is<Game>(g =>
                g.PlayerXId == playerXId &&
                g.PlayerOId == playerOId &&
                g.Status == GameStatus.InProgress &&
                g.CurrentTurn == PlayerSymbol.X &&
                g.Size == 3 &&
                g.WinConditionLength == 3)), Times.Once);
        }

        [Theory]
        [InlineData("PlayerX", 404, "Игрок X не найден")]
        [InlineData("PlayerO", 404, "Игрок O не найден")]
        [InlineData("SamePlayer", 400, "Игрок не может играть сам с собой")]
        public async Task AddGameAsync_InvalidPlayers_ReturnsError(string scenario, int expectedCode, string expectedMessage)
        {
            var playerXId = Guid.NewGuid();
            var playerOId = Guid.NewGuid();
            var request = new GameAddRequest { PlayerXId = playerXId, PlayerOId = playerOId };

            switch (scenario)
            {
                case "PlayerX":
                    _playerRepositoryMock.Setup(x => x.GetPlayerAsync(playerXId)).ReturnsAsync((Player)null);
                    break;

                case "PlayerO":
                    _playerRepositoryMock.Setup(x => x.GetPlayerAsync(playerXId)).ReturnsAsync(new Player { Id = playerXId });
                    _playerRepositoryMock.Setup(x => x.GetPlayerAsync(playerOId)).ReturnsAsync((Player)null);
                    break;

                case "SamePlayer":
                    request.PlayerOId = playerXId;
                    var player = new Player { Id = playerXId };
                    _playerRepositoryMock.Setup(x => x.GetPlayerAsync(playerXId)).ReturnsAsync(player);
                    break;
            }

            var result = await _gameService.AddGameAsync(request);

            Assert.False(result.Success);
            Assert.Equal(expectedCode, result.ErrorCode);
            Assert.Equal(expectedMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task AddGameAsync_RepositoryException_ReturnsError()
        {
            var (playerXId, playerOId, request, playerX, playerO, _, _) = CreateAddGameTestData();

            SetupPlayerMocks(playerXId, playerX, playerOId, playerO);
            _gameRepositoryMock.Setup(x => x.AddGameAsync(It.IsAny<Game>())).ThrowsAsync(new Exception("Database error"));

            var result = await _gameService.AddGameAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.ErrorCode);
            Assert.Equal("Database error", result.ErrorMessage);
        }

        [Fact]
        public async Task GetGameAsync_ExistingGame_ReturnsSuccess()
        {
            var gameId = Guid.NewGuid();
            var game = new Game { Id = gameId, Status = GameStatus.InProgress };
            var gameResponse = new GameResponse { Id = gameId, Status = GameStatus.InProgress };

            _gameRepositoryMock.Setup(x => x.GetGameAsync(gameId)).ReturnsAsync(game);
            _mapperMock.Setup(x => x.Map<GameResponse>(game)).Returns(gameResponse);

            var result = await _gameService.GetGameAsync(gameId);

            Assert.True(result.Success);
            Assert.Equal(gameResponse, result.Data);
        }

        [Theory]
        [InlineData(false, 404, "Игра не найдена")]
        [InlineData(true, 500, "Database error")]
        public async Task GetGameAsync_ErrorScenarios_ReturnsExpectedError(bool throwException, int expectedCode, string expectedMessage)
        {
            var gameId = Guid.NewGuid();

            if (throwException)
                _gameRepositoryMock.Setup(x => x.GetGameAsync(gameId)).ThrowsAsync(new Exception("Database error"));
            else
                _gameRepositoryMock.Setup(x => x.GetGameAsync(gameId)).ReturnsAsync((Game)null);

            var result = await _gameService.GetGameAsync(gameId);

            Assert.False(result.Success);
            Assert.Equal(expectedCode, result.ErrorCode);
            Assert.Equal(expectedMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task SearchGamesAsync_SuccessfulSearch_ReturnsSuccess()
        {
            var request = new GameSearchRequest
            {
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 },
                Filter = new GameFilterRequest { Status = GameStatus.InProgress },
                Sort = new List<SortRequest> { new SortRequest { Field = "CreatedAt", Direction = SortDirection.Descending } }
            };

            var games = new PagedResponse<Game>
            {
                Items = new List<Game> { new Game { Id = Guid.NewGuid() } },
                TotalCount = 1,
                PageSize = 10,
                PageNumber = 1
            };

            var gameResponses = new List<GameResponse> { new GameResponse { Id = games.Items.First().Id } };

            _gameRepositoryMock.Setup(x => x.SearchGamesAsync(1, 10, GameStatus.InProgress, null, It.IsAny<List<(string, SortDirection)>>()))
                .ReturnsAsync(games);
            _mapperMock.Setup(x => x.Map<List<GameResponse>>(games.Items)).Returns(gameResponses);

            var result = await _gameService.SearchGamesAsync(request);

            Assert.True(result.Success);
            Assert.Equal(1, result.Data.TotalCount);
            Assert.Equal(gameResponses, result.Data.Items);
        }

        [Fact]
        public async Task SearchGamesAsync_RepositoryException_ReturnsError()
        {
            var request = new GameSearchRequest
            {
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 }
            };

            _gameRepositoryMock.Setup(x => x.SearchGamesAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<GameStatus?>(),
                It.IsAny<List<Guid>?>(),
                It.IsAny<List<(string field, SortDirection direction)>>()))
            .ThrowsAsync(new Exception("Database error"));

            var result = await _gameService.SearchGamesAsync(request);

            Assert.False(result.Success);
            Assert.Equal(500, result.ErrorCode);
            Assert.Equal("Database error", result.ErrorMessage);
        }

        [Fact]
        public async Task AddGameAsync_CustomBoardSize_CreatesGameWithCorrectSize()
        {
            _configurationMock.Setup(x => x["Game:BoardSize"]).Returns("5");
            _configurationMock.Setup(x => x["Game:WinConditionLength"]).Returns("4");

            var gameService = new GameService(_gameRepositoryMock.Object, _playerRepositoryMock.Object, _configurationMock.Object, _mapperMock.Object);

            var (playerXId, playerOId, request, playerX, playerO, game, gameResponse) = CreateAddGameTestData();

            SetupPlayerMocks(playerXId, playerX, playerOId, playerO);

            _gameRepositoryMock.Setup(x => x.AddGameAsync(It.IsAny<Game>())).ReturnsAsync(game);
            _mapperMock.Setup(x => x.Map<GameResponse>(It.IsAny<Game>())).Returns(gameResponse);

            var result = await gameService.AddGameAsync(request);

            Assert.True(result.Success);
            _gameRepositoryMock.Verify(x => x.AddGameAsync(It.Is<Game>(g => g.Size == 5 && g.WinConditionLength == 4)), Times.Once);
        }

        private (Guid playerXId, Guid playerOId, GameAddRequest request, Player playerX, Player playerO, Game game, GameResponse gameResponse) CreateAddGameTestData()
        {
            var playerXId = Guid.NewGuid();
            var playerOId = Guid.NewGuid();
            var request = new GameAddRequest { PlayerXId = playerXId, PlayerOId = playerOId };
            var playerX = new Player { Id = playerXId, Username = "PlayerX" };
            var playerO = new Player { Id = playerOId, Username = "PlayerO" };
            var game = new Game { Id = Guid.NewGuid(), PlayerXId = playerXId, PlayerOId = playerOId };
            var gameResponse = new GameResponse { Id = game.Id, PlayerXId = playerXId, PlayerOId = playerOId };
            return (playerXId, playerOId, request, playerX, playerO, game, gameResponse);
        }

        private void SetupPlayerMocks(Guid playerXId, Player playerX, Guid playerOId, Player playerO)
        {
            _playerRepositoryMock.Setup(x => x.GetPlayerAsync(playerXId)).ReturnsAsync(playerX);
            _playerRepositoryMock.Setup(x => x.GetPlayerAsync(playerOId)).ReturnsAsync(playerO);
        }
    }
}
