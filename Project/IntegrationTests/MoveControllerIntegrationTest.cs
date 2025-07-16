using Core.Models.DTOs.Common;
using Core.Models.DTOs.Game;
using Core.Models.DTOs.Move;
using Core.Models.Enums;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using Xunit;

namespace IntegrationTests
{
    public class MoveControllerIntegrationTest : IntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        public MoveControllerIntegrationTest(WebApplicationFactory<Program> factory)
            : base(factory, "TestDb_Move_" + Guid.NewGuid()) { }

        private async Task<Guid> CreateGameAsync(Guid playerXId, Guid playerOId)
        {
            var request = new GameAddRequest
            {
                PlayerXId = playerXId,
                PlayerOId = playerOId
            };
            var response = await _client.PostAsync("/api/v1/Game", Serialize(request));
            response.EnsureSuccessStatusCode();

            var apiResponse = await DeserializeApiResponseAsync(response);
            var data = (JsonElement)apiResponse.Data!;
            return data.GetProperty("id").GetGuid();
        }

        private async Task<(Guid moveId, MoveAddRequest moveRequest)> AddMoveAsync(Guid gameId, Guid playerId, int x, int y, string clientMoveId)
        {
            var moveRequest = new MoveAddRequest
            {
                GameId = gameId,
                PlayerId = playerId,
                X = x,
                Y = y,
                ClientMoveId = clientMoveId
            };
            var response = await _client.PostAsync("/api/v1/Move", Serialize(moveRequest));
            var apiResponse = await DeserializeApiResponseAsync(response);

            Assert.True(apiResponse.Success, "Move creation failed: " + apiResponse.Error?.Message);

            var data = (JsonElement)apiResponse.Data!;
            var moveId = data.GetProperty("id").GetGuid();

            return (moveId, moveRequest);
        }

        private async Task<Guid> GetPlayerIdAsync(string username)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = await db.Players.FirstAsync(p => p.Username == username);
            return player.Id;
        }

        [Fact]
        public async Task AddMove_ValidMove_SucceedsAndReturnsMove()
        {
            var pX = await RegisterAsync("playerX");
            var pO = await RegisterAsync("playerO");

            UseToken("playerX");
            var gameId = await CreateGameAsync(pX, pO);

            var clientMoveId = Guid.NewGuid().ToString();
            var (moveId, _) = await AddMoveAsync(gameId, pX, 0, 0, clientMoveId);

            var getResponse = await _client.GetAsync($"/api/v1/Move/{moveId}");
            var api = await DeserializeApiResponseAsync(getResponse);

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.True(api.Success);

            var moveData = (JsonElement)api.Data!;
            Assert.Equal(pX, moveData.GetProperty("playerId").GetGuid());
        }

        [Fact]
        public async Task AddMove_InvalidPlayerNotInGame_ReturnsForbidden()
        {
            var pX = await RegisterAsync("playerX");
            var pO = await RegisterAsync("playerO");
            var pInvalid = await RegisterAsync("invalidPlayer");

            UseToken("playerX");
            var gameId = await CreateGameAsync(pX, pO);

            UseToken("invalidPlayer");
            var moveRequest = new MoveAddRequest
            {
                GameId = gameId,
                PlayerId = pInvalid,
                X = 0,
                Y = 0,
                ClientMoveId = Guid.NewGuid().ToString()
            };
            var response = await _client.PostAsync("/api/v1/Move", Serialize(moveRequest));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            var apiResponse = await DeserializeApiResponseAsync(response);
            Assert.False(apiResponse.Success);
            Assert.Contains("Игрок не участвует в этой игре", apiResponse.Error?.Message);
        }

        [Fact]
        public async Task AddMove_OutOfBoundsCoordinates_ReturnsBadRequest()
        {
            var pX = await RegisterAsync("playerX");
            var pO = await RegisterAsync("playerO");

            UseToken("playerX");
            var gameId = await CreateGameAsync(pX, pO);

            var moveRequest = new MoveAddRequest
            {
                GameId = gameId,
                PlayerId = pX,
                X = 10,
                Y = 0,
                ClientMoveId = Guid.NewGuid().ToString()
            };
            var response = await _client.PostAsync("/api/v1/Move", Serialize(moveRequest));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var apiResponse = await DeserializeApiResponseAsync(response);
            Assert.False(apiResponse.Success);
            Assert.Contains("выходят за пределы", apiResponse.Error?.Message);
        }

        [Fact]
        public async Task AddMove_CellAlreadyOccupied_ReturnsConflict()
        {
            var pX = await RegisterAsync("playerX");
            var pO = await RegisterAsync("playerO");

            UseToken("playerX");
            var gameId = await CreateGameAsync(pX, pO);
            await AddMoveAsync(gameId, pX, 0, 0, Guid.NewGuid().ToString());

            UseToken("playerO");
            var moveRequest = new MoveAddRequest
            {
                GameId = gameId,
                PlayerId = pO,
                X = 0,
                Y = 0,
                ClientMoveId = Guid.NewGuid().ToString()
            };
            var response = await _client.PostAsync("/api/v1/Move", Serialize(moveRequest));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var apiResponse = await DeserializeApiResponseAsync(response);
            Assert.False(apiResponse.Success);
            Assert.Contains("Эта клетка уже занята", apiResponse.Error?.Message);
        }

        [Fact]
        public async Task AddMove_NotPlayersTurn_ReturnsConflict()
        {
            var pX = await RegisterAsync("playerX");
            var pO = await RegisterAsync("playerO");

            UseToken("playerX");
            var gameId = await CreateGameAsync(pX, pO);

            // Первый ход игрока X
            await AddMoveAsync(gameId, pX, 0, 0, Guid.NewGuid().ToString());

            // Второй ход того же игрока (не его очередь)
            var moveRequest = new MoveAddRequest
            {
                GameId = gameId,
                PlayerId = pX,
                X = 1,
                Y = 0,
                ClientMoveId = Guid.NewGuid().ToString()
            };
            var response = await _client.PostAsync("/api/v1/Move", Serialize(moveRequest));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var apiResponse = await DeserializeApiResponseAsync(response);
            Assert.False(apiResponse.Success);
            Assert.Contains("Сейчас не ваш ход", apiResponse.Error?.Message);
        }

        [Fact]
        public async Task AddMove_AfterGameFinished_ReturnsConflict()
        {
            var pX = await RegisterAsync("playerX");
            var pO = await RegisterAsync("playerO");

            UseToken("playerX");
            var gameId = await CreateGameAsync(pX, pO);

            await AddMoveAsync(gameId, pX, 0, 0, Guid.NewGuid().ToString());

            UseToken("playerO");
            await AddMoveAsync(gameId, pO, 1, 0, Guid.NewGuid().ToString());
            UseToken("playerX");
            await AddMoveAsync(gameId, pX, 0, 1, Guid.NewGuid().ToString());
            UseToken("playerO");
            await AddMoveAsync(gameId, pO, 1, 1, Guid.NewGuid().ToString());
            UseToken("playerX");
            await AddMoveAsync(gameId, pX, 0, 2, Guid.NewGuid().ToString());

            UseToken("playerO");
            var moveRequest = new MoveAddRequest
            {
                GameId = gameId,
                PlayerId = pO,
                X = 2,
                Y = 2,
                ClientMoveId = Guid.NewGuid().ToString()
            };
            var response = await _client.PostAsync("/api/v1/Move", Serialize(moveRequest));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

            var apiResponse = await DeserializeApiResponseAsync(response);
            Assert.False(apiResponse.Success);
            Assert.Contains("Игра уже завершена", apiResponse.Error?.Message);
        }

        [Fact]
        public async Task SearchMoves_FiltersAndPagination_ReturnsFilteredMoves()
        {
            var pX = await RegisterAsync("playerX");
            var pO = await RegisterAsync("playerO");

            UseToken("playerX");
            var gameId = await CreateGameAsync(pX, pO);

            for (int i = 0; i < 5; i++)
            {
                var playerId = (i % 2 == 0) ? pX : pO;
                if (i % 2 == 0)
                    UseToken("playerX");
                else
                    UseToken("playerO");
                await AddMoveAsync(gameId, playerId, i % 3, i / 3, Guid.NewGuid().ToString());
            }

            var searchRequest = new MoveSearchRequest
            {
                Filter = new MoveFilterRequest { PlayerId = pX },
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 },
                Sort = new List<SortRequest> { new() { Field = "MoveNumber", Direction = SortDirection.Ascending } }
            };

            var response = await _client.PostAsync("/api/v1/Move/search", Serialize(searchRequest));
            response.EnsureSuccessStatusCode();

            var apiResponse = await DeserializeApiResponseAsync(response);
            Assert.True(apiResponse.Success);

            var data = (JsonElement)apiResponse.Data!;
            var items = data.GetProperty("items").EnumerateArray().ToList();

            Assert.All(items, item => Assert.Equal(pX, item.GetProperty("playerId").GetGuid()));
            Assert.InRange(items.Count, 1, 5);
        }
    }
}
