using Core.Models.DTOs.Common;
using Core.Models.DTOs.Game;
using Core.Models.Entities;
using Core.Models.Enums;
using Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace IntegrationTests
{
    public class GameControllerIntegrationTest : IntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        public GameControllerIntegrationTest(WebApplicationFactory<Program> factory)
            : base(factory, "TestDb_Game_" + Guid.NewGuid()) { }

        private async Task<Guid> GetGameIdFromResponseAsync(HttpResponseMessage response)
        {
            var apiResponse = await DeserializeApiResponseAsync(response);
            var json = (JsonElement)apiResponse.Data!;
            return Guid.Parse(json.GetProperty("id").GetString()!);
        }

        private async Task<Guid> AddGameAsync(Guid xId, Guid oId)
        {
            var request = new GameAddRequest { PlayerXId = xId, PlayerOId = oId };
            var response = await _client.PostAsync("/api/v1/Game", Serialize(request));
            response.EnsureSuccessStatusCode();
            return await GetGameIdFromResponseAsync(response);
        }

        private async Task<Guid> CreateGameWithStatusAsync(Guid xId, Guid oId, GameStatus status, DateTime createdAt)
        {
            var gameId = await AddGameAsync(xId, oId);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var game = await db.Games.FindAsync(gameId);
            game!.Status = status;
            game.CreatedAt = createdAt;
            await db.SaveChangesAsync();
            return gameId;
        }

        [Fact]
        public async Task AddGame_ValidPlayers_Success()
        {
            var x = await RegisterAsync("X");
            var o = await RegisterAsync("O");

            UseToken("X");

            var response = await _client.PostAsync("/api/v1/Game", Serialize(new GameAddRequest { PlayerXId = x, PlayerOId = o }));
            var api = await DeserializeApiResponseAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(api.Success);
            Assert.NotNull(api.Data);
        }

        [Fact]
        public async Task AddGame_SamePlayer_BadRequest()
        {
            var p = await RegisterAsync("Self");

            UseToken("Self");

            var response = await _client.PostAsync("/api/v1/Game", Serialize(new GameAddRequest { PlayerXId = p, PlayerOId = p }));
            var api = await DeserializeApiResponseAsync(response);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(api.Success);
            Assert.Contains("не может играть сам с собой", api.Error?.Message);
        }

        [Fact]
        public async Task AddGame_NonExistentPlayers_NotFound()
        {
            var user = await RegisterAsync("user");

            UseToken("user");

            var response = await _client.PostAsync("/api/v1/Game", Serialize(new GameAddRequest { PlayerXId = Guid.NewGuid(), PlayerOId = Guid.NewGuid() }));
            var api = await DeserializeApiResponseAsync(response);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(api.Success);
        }

        [Fact]
        public async Task GetGame_ValidId_Success()
        {
            var x = await RegisterAsync("gX");
            var o = await RegisterAsync("gO");

            UseToken("gX");

            var id = await AddGameAsync(x, o);

            var response = await _client.GetAsync($"/api/v1/Game/{id}");
            var api = await DeserializeApiResponseAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(api.Success);
        }

        [Fact]
        public async Task GetGame_InvalidId_NotFound()
        {
            var user = await RegisterAsync("userInvalid");
            UseToken("userInvalid");

            var response = await _client.GetAsync($"/api/v1/Game/{Guid.NewGuid()}");
            var api = await DeserializeApiResponseAsync(response);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.False(api.Success);
        }

        [Fact]
        public async Task SearchGames_EmptyFilter_ReturnsAll()
        {
            var x1 = await RegisterAsync("sx1");
            var o1 = await RegisterAsync("so1");

            UseToken("sx1");

            await AddGameAsync(x1, o1);

            var response = await _client.PostAsync("/api/v1/Game/search", Serialize(new GameSearchRequest()));
            var api = await DeserializeApiResponseAsync(response);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(api.Success);
            Assert.NotNull(api.Data);
        }

        private static readonly string[] SortFields = new[]
        {
            nameof(Game.CreatedAt),
            nameof(Game.Id),
            nameof(Game.PlayerXId),
            nameof(Game.PlayerOId),
            nameof(Game.Status),
            nameof(Game.EndedAt)
        };

        private static bool IsSorted<T>(List<T> list, bool ascending = true) where T : IComparable<T>
        {
            for (int i = 1; i < list.Count; i++)
            {
                int cmp = list[i].CompareTo(list[i - 1]);
                if (ascending && cmp < 0)
                    return false;
                if (!ascending && cmp > 0)
                    return false;
            }
            return true;
        }

        public static IEnumerable<object[]> GetSortTestData()
        {
            foreach (var field in SortFields)
            {
                yield return new object[] { field, SortDirection.Ascending };
                yield return new object[] { field, SortDirection.Descending };
            }
        }

        [Theory]
        [MemberData(nameof(GetSortTestData))]
        public async Task SearchGames_SortByField_WorksCorrectly(string sortField, SortDirection direction)
        {
            var p1 = await RegisterAsync("p1");
            var p2 = await RegisterAsync("p2");
            var p3 = await RegisterAsync("p3");

            UseToken("p1");
            var now = DateTime.UtcNow;
            await CreateGameWithStatusAsync(p1, p2, GameStatus.InProgress, now.AddMinutes(-30));

            UseToken("p3");
            await CreateGameWithStatusAsync(p2, p3, GameStatus.Finished, now.AddMinutes(-20));
            await CreateGameWithStatusAsync(p1, p3, GameStatus.InProgress, now.AddMinutes(-10));

            var search = new GameSearchRequest
            {
                Filter = null,
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 },
                Sort = new List<SortRequest> { new() { Field = sortField, Direction = direction } }
            };

            var response = await _client.PostAsync("/api/v1/Game/search", Serialize(search));
            var api = await DeserializeApiResponseAsync(response);

            Assert.True(api.Success);
            var data = (JsonElement)api.Data!;
            var items = data.GetProperty("items").EnumerateArray().ToList();

            Assert.True(items.Count >= 3);

            bool ascending = direction == SortDirection.Ascending;

            switch (sortField)
            {
                case nameof(Game.CreatedAt):
                    var dates = items.Select(i => DateTime.Parse(i.GetProperty("createdAt").GetString()!)).ToList();
                    Assert.True(IsSorted(dates, ascending), $"Items are not sorted by {sortField} in {(ascending ? "ascending" : "descending")} order");
                    break;

                case nameof(Game.Id):
                    var guidsId = items.Select(i => Guid.Parse(i.GetProperty("id").GetString()!)).ToList();
                    Assert.True(IsSorted(guidsId, ascending), $"Items are not sorted by {sortField} in {(ascending ? "ascending" : "descending")} order");
                    break;

                case nameof(Game.PlayerXId):
                    var guidsX = items.Select(i => Guid.Parse(i.GetProperty("playerXId").GetString()!)).ToList();
                    Assert.True(IsSorted(guidsX, ascending), $"Items are not sorted by {sortField} in {(ascending ? "ascending" : "descending")} order");
                    break;

                case nameof(Game.PlayerOId):
                    var guidsO = items.Select(i => Guid.Parse(i.GetProperty("playerOId").GetString()!)).ToList();
                    Assert.True(IsSorted(guidsO, ascending), $"Items are not sorted by {sortField} in {(ascending ? "ascending" : "descending")} order");
                    break;

                case nameof(Game.Status):
                    var statuses = items.Select(i => i.GetProperty("status").GetInt32()).ToList();
                    Assert.True(IsSorted(statuses, ascending), $"Items are not sorted by {sortField} in {(ascending ? "ascending" : "descending")} order");
                    break;

                case nameof(Game.EndedAt):
                    var endedAts = items.Select(i =>
                        i.TryGetProperty("endedAt", out var endedAtProp) && endedAtProp.ValueKind != JsonValueKind.Null
                            ? DateTime.Parse(endedAtProp.GetString()!)
                            : DateTime.MinValue).ToList();
                    Assert.True(IsSorted(endedAts, ascending), $"Items are not sorted by {sortField} in {(ascending ? "ascending" : "descending")} order");
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported sort field: {sortField}");
            }
        }

        [Fact]
        public async Task SearchGames_FilterByStatus_ReturnsCorrectGames()
        {
            var p1 = await RegisterAsync("p1");
            var p2 = await RegisterAsync("p2");

            UseToken("p1");

            await CreateGameWithStatusAsync(p1, p2, GameStatus.InProgress, DateTime.UtcNow.AddMinutes(-15));
            await CreateGameWithStatusAsync(p2, p1, GameStatus.Finished, DateTime.UtcNow.AddMinutes(-10));

            var search = new GameSearchRequest
            {
                Filter = new GameFilterRequest { Status = GameStatus.Finished },
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 }
            };

            var response = await _client.PostAsync("/api/v1/Game/search", Serialize(search));
            var api = await DeserializeApiResponseAsync(response);

            Assert.True(api.Success);
            var data = (JsonElement)api.Data!;
            var items = data.GetProperty("items").EnumerateArray();

            foreach (var game in items)
                Assert.Equal((int)GameStatus.Finished, game.GetProperty("status").GetInt32());
        }

        [Fact]
        public async Task SearchGames_FilterByPlayerIds_ReturnsCorrectGames()
        {
            var p1 = await RegisterAsync("p1");
            var p2 = await RegisterAsync("p2");
            var p3 = await RegisterAsync("p3");

            UseToken("p1");
            await CreateGameWithStatusAsync(p1, p2, GameStatus.InProgress, DateTime.UtcNow.AddMinutes(-30));

            UseToken("p3");
            await CreateGameWithStatusAsync(p2, p3, GameStatus.Finished, DateTime.UtcNow.AddMinutes(-20));           
            await CreateGameWithStatusAsync(p1, p3, GameStatus.InProgress, DateTime.UtcNow.AddMinutes(-10));

            var search = new GameSearchRequest
            {
                Filter = new GameFilterRequest { PlayerIds = new List<Guid> { p3 } },
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 }
            };

            var response = await _client.PostAsync("/api/v1/Game/search", Serialize(search));
            var api = await DeserializeApiResponseAsync(response);

            Assert.True(api.Success);
            var data = (JsonElement)api.Data!;
            var items = data.GetProperty("items").EnumerateArray();

            foreach (var game in items)
            {
                var xId = Guid.Parse(game.GetProperty("playerXId").GetString()!);
                var oId = Guid.Parse(game.GetProperty("playerOId").GetString()!);
                Assert.True(xId == p3 || oId == p3);
            }
        }

        [Fact]
        public async Task SearchGames_FilterByStatusAndPlayerIds_ReturnsCorrectGames()
        {
            var p1 = await RegisterAsync("p1");
            var p2 = await RegisterAsync("p2");
            var p3 = await RegisterAsync("p3");

            UseToken("p1");

            await CreateGameWithStatusAsync(p1, p2, GameStatus.InProgress, DateTime.UtcNow.AddMinutes(-30));

            UseToken("p3");
            await CreateGameWithStatusAsync(p2, p3, GameStatus.Finished, DateTime.UtcNow.AddMinutes(-20));
            await CreateGameWithStatusAsync(p1, p3, GameStatus.InProgress, DateTime.UtcNow.AddMinutes(-10));

            var search = new GameSearchRequest
            {
                Filter = new GameFilterRequest { Status = GameStatus.InProgress, PlayerIds = new List<Guid> { p1 } },
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 }
            };

            var response = await _client.PostAsync("/api/v1/Game/search", Serialize(search));
            var api = await DeserializeApiResponseAsync(response);

            Assert.True(api.Success);
            var data = (JsonElement)api.Data!;
            var items = data.GetProperty("items").EnumerateArray();

            foreach (var game in items)
            {
                var status = game.GetProperty("status").GetInt32();
                var xId = Guid.Parse(game.GetProperty("playerXId").GetString()!);
                var oId = Guid.Parse(game.GetProperty("playerOId").GetString()!);

                Assert.Equal((int)GameStatus.InProgress, status);
                Assert.True(xId == p1 || oId == p1);
            }
        }

        [Fact]
        public async Task AddGame_UserNotParticipant_BadRequest()
        {
            var user1 = await RegisterAsync("user1");
            var user2 = await RegisterAsync("user2");
            var outsider = await RegisterAsync("outsider");

            UseToken("outsider");

            var response = await _client.PostAsync("/api/v1/Game", Serialize(new GameAddRequest { PlayerXId = user1, PlayerOId = user2 }));
            var api = await DeserializeApiResponseAsync(response);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(api.Success);
            Assert.Contains("Нельзя создать игру не учавствя в ней", api.Error?.Message);
        }

    }
}
