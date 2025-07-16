using AutoMapper;
using Core.Models;
using Core.Models.DTOs.Move;
using Core.Models.Entities;
using Core.Models.Enums;
using Core.Services;
using Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ServicesTests
{
    public class MoveServiceTest
    {
        private readonly Mock<IMoveRepository> _moveRepositoryMock;
        private readonly Mock<IGameRepository> _gameRepositoryMock;
        private readonly Mock<IPlayerRepository> _playerRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly MoveService _moveService;

        public MoveServiceTest()
        {
            _moveRepositoryMock = new Mock<IMoveRepository>();
            _gameRepositoryMock = new Mock<IGameRepository>();
            _playerRepositoryMock = new Mock<IPlayerRepository>();
            _mapperMock = new Mock<IMapper>();

            _moveService = new MoveService(
                _moveRepositoryMock.Object,
                _gameRepositoryMock.Object,
                _playerRepositoryMock.Object,
                _mapperMock.Object);
        }

        private Game CreateGame(Guid gameId, Guid playerXId, Guid playerOId, GameStatus status = GameStatus.InProgress, PlayerSymbol currentTurn = PlayerSymbol.X)
        {
            return new Game
            {
                Id = gameId,
                PlayerXId = playerXId,
                PlayerOId = playerOId,
                Size = 3,
                WinConditionLength = 3,
                Status = status,
                CurrentTurn = currentTurn,
                Moves = new List<Move>()
            };
        }

        private void SetupBasicMocks(Game game, Player player, Move? move = null, MoveResponse? moveResponse = null)
        {
            _gameRepositoryMock.Setup(x => x.GetGameAsync(game.Id)).ReturnsAsync(game);
            _playerRepositoryMock.Setup(x => x.GetPlayerAsync(player.Id)).ReturnsAsync(player);

            if (move != null)
                _moveRepositoryMock.Setup(x => x.AddMoveAsync(It.IsAny<Move>())).ReturnsAsync(move);

            if (moveResponse != null)
                _mapperMock.Setup(x => x.Map<MoveResponse>(It.IsAny<Move>())).Returns(moveResponse);
        }

        [Fact]
        public async Task AddMoveAsync_SuccessfulMove_ReturnsSuccessAndUpdatesGame()
        {
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();

            var game = CreateGame(gameId, playerId, Guid.NewGuid());
            var player = new Player { Id = playerId, Username = "Player1" };
            var move = new Move { Id = Guid.NewGuid(), GameId = gameId, PlayerId = playerId, Symbol = PlayerSymbol.X, X = 0, Y = 0 };
            var moveResponse = new MoveResponse { Id = move.Id, GameId = gameId, PlayerId = playerId, Symbol = PlayerSymbol.X, X = 0, Y = 0 };
            var request = new MoveAddRequest { GameId = gameId, PlayerId = playerId, X = 0, Y = 0, ClientMoveId = "move1" };

            SetupBasicMocks(game, player, move, moveResponse);

            var result = await _moveService.AddMoveAsync(request);

            Assert.True(result.Success);
            Assert.Equal(moveResponse, result.Data);
            _gameRepositoryMock.Verify(x => x.UpdateGameAsync(It.IsAny<Game>()), Times.Once);
        }

        [Fact]
        public async Task AddMoveAsync_WinningScenario_UpdatesGameStatusAndWinner()
        {
            var gameId = Guid.NewGuid();
            var playerXId = Guid.NewGuid();
            var playerOId = Guid.NewGuid();

            var game = CreateGame(gameId, playerXId, playerOId);
            game.Moves.AddRange(new[]
            {
                new Move { X = 0, Y = 0, Symbol = PlayerSymbol.X },
                new Move { X = 1, Y = 0, Symbol = PlayerSymbol.O },
                new Move { X = 0, Y = 1, Symbol = PlayerSymbol.X },
                new Move { X = 1, Y = 1, Symbol = PlayerSymbol.O }
            });

            var request = new MoveAddRequest { GameId = gameId, PlayerId = playerXId, X = 0, Y = 2, ClientMoveId = "winning_move" };
            var player = new Player { Id = playerXId, Username = "PlayerX" };
            var move = new Move { Id = Guid.NewGuid(), GameId = gameId, PlayerId = playerXId, Symbol = PlayerSymbol.X, X = 0, Y = 2 };
            var moveResponse = new MoveResponse { Id = move.Id };

            SetupBasicMocks(game, player, move, moveResponse);

            var result = await _moveService.AddMoveAsync(request);

            Assert.True(result.Success);
            _gameRepositoryMock.Verify(x => x.UpdateGameAsync(It.Is<Game>(g => g.Status == GameStatus.Finished && g.Winner == PlayerSymbol.X)), Times.Once);
        }

        [Theory]
        [InlineData("PlayerNotFound", 404, "Игрок не найден", null, null)]
        [InlineData("PlayerNotInGame", 403, "Игрок не участвует в этой игре", null, null)]
        [InlineData("GameFinished", 409, "Игра уже завершена", null, null)]
        [InlineData("NotPlayerTurn", 409, "Сейчас не ваш ход", null, null)]
        [InlineData("CoordinatesOutOfBounds", 400, "Координаты хода выходят за пределы игрового поля", 5, 5)]
        [InlineData("CellAlreadyOccupied", 409, "Эта клетка уже занята", 0, 0)]
        public async Task AddMoveAsync_InvalidScenarios_ReturnsExpectedError(string? scenario, int expectedErrorCode, string expectedErrorMessage, int? x, int? y)
        {
            var gameId = Guid.NewGuid();
            var playerXId = Guid.NewGuid();
            var playerOId = Guid.NewGuid();
            var playerId = scenario == "PlayerNotInGame" ? Guid.NewGuid() : playerXId;
            var gameStatus = scenario == "GameFinished" ? GameStatus.Finished : GameStatus.InProgress;
            var currentTurn = scenario == "NotPlayerTurn" ? PlayerSymbol.O : PlayerSymbol.X;

            Game? game = scenario == "GameNotFound" ? null : CreateGame(gameId, playerXId, playerOId, gameStatus, currentTurn);
            Player? player = scenario == "PlayerNotFound" ? null : new Player { Id = playerId, Username = "Player" };

            if (scenario == "CellAlreadyOccupied" && game != null)
            {
                game.Moves.Add(new Move { X = 0, Y = 0, Symbol = PlayerSymbol.O });
            }

            _gameRepositoryMock.Setup(x => x.GetGameAsync(gameId)).ReturnsAsync(game);
            _playerRepositoryMock.Setup(x => x.GetPlayerAsync(playerId)).ReturnsAsync(player);

            var request = new MoveAddRequest
            {
                GameId = gameId,
                PlayerId = playerId,
                X = x ?? 0,
                Y = y ?? 0
            };

            var result = await _moveService.AddMoveAsync(request);

            Assert.False(result.Success);
            Assert.Equal(expectedErrorCode, result.ErrorCode);
            Assert.Equal(expectedErrorMessage, result.ErrorMessage);
        }

        [Fact]
        public async Task AddMoveAsync_DuplicateClientMoveId_ReturnsExistingMove()
        {
            var gameId = Guid.NewGuid();
            var playerId = Guid.NewGuid();
            var clientMoveId = "duplicate_move";

            var game = CreateGame(gameId, playerId, Guid.NewGuid());
            var player = new Player { Id = playerId, Username = "Player1" };
            var existingMove = new Move { Id = Guid.NewGuid(), ClientMoveId = clientMoveId };
            var moveResponse = new MoveResponse { Id = existingMove.Id };

            _gameRepositoryMock.Setup(x => x.GetGameAsync(gameId)).ReturnsAsync(game);
            _playerRepositoryMock.Setup(x => x.GetPlayerAsync(playerId)).ReturnsAsync(player);

            _moveRepositoryMock.Setup(x => x.AddMoveAsync(It.IsAny<Move>()))
                .ThrowsAsync(new DbUpdateException("Duplicate key", new Exception("IX_Moves_GameId_ClientMoveId")));

            _moveRepositoryMock.Setup(x => x.GetMoveByClientMoveIdAsync(gameId, clientMoveId)).ReturnsAsync(existingMove);
            _mapperMock.Setup(x => x.Map<MoveResponse>(existingMove)).Returns(moveResponse);

            var request = new MoveAddRequest { GameId = gameId, PlayerId = playerId, X = 0, Y = 0, ClientMoveId = clientMoveId };

            var result = await _moveService.AddMoveAsync(request);

            Assert.True(result.Success);
            Assert.Equal(moveResponse, result.Data);
        }
    }
}
