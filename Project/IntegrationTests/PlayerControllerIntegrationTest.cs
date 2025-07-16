using Core.Models.DTOs.Auth;
using Core.Models.DTOs.Common;
using Core.Models.DTOs.Player;
using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    public class PlayerControllerIntegrationTest : IntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
    {
        public PlayerControllerIntegrationTest(WebApplicationFactory<Program> factory) : base(factory, "TestDb_Player" + Guid.NewGuid())
        {
        }

        [Fact]
        public async Task Register_NewPlayer_ReturnsTokenAndCreated()
        {
            var request = new RegisterRequest { Username = "newplayer", Password = "password123" };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/Player/register", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
            Assert.NotEmpty(apiResponse.Data.ToString());
        }

        [Fact]
        public async Task Register_ExistingPlayer_ReturnsConflict()
        {
            var request = new RegisterRequest { Username = "existingplayer", Password = "password123" };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _client.PostAsync("/api/v1/Player/register", content);
            var response = await _client.PostAsync("/api/v1/Player/register", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.False(apiResponse.Success);
            Assert.Equal("Игрок уже существует", apiResponse.Error.Message);
        }

        [Fact]
        public async Task Login_CorrectCredentials_ReturnsToken()
        {
            var registerRequest = new RegisterRequest { Username = "logintest", Password = "password123" };
            var registerJson = JsonSerializer.Serialize(registerRequest);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/v1/Player/register", registerContent);

            var loginRequest = new LoginRequest { Username = "logintest", Password = "password123" };
            var loginJson = JsonSerializer.Serialize(loginRequest);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/Player/login", loginContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
        }

        [Fact]
        public async Task Login_IncorrectPassword_ReturnsUnauthorized()
        {
            var registerRequest = new RegisterRequest { Username = "passwordtest", Password = "correctpassword" };
            var registerJson = JsonSerializer.Serialize(registerRequest);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/v1/Player/register", registerContent);

            var loginRequest = new LoginRequest { Username = "passwordtest", Password = "wrongpassword" };
            var loginJson = JsonSerializer.Serialize(loginRequest);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/Player/login", loginContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(apiResponse.Success);
            Assert.Equal("Неверный пароль", apiResponse.Error.Message);
        }

        [Fact]
        public async Task Login_PlayerNotFound_ReturnsNotFound()
        {
            var loginRequest = new LoginRequest { Username = "nonexistent", Password = "password123" };
            var loginJson = JsonSerializer.Serialize(loginRequest);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/Player/login", loginContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.False(apiResponse.Success);
            Assert.Equal("Игрок не найден", apiResponse.Error.Message);
        }

        [Fact]
        public async Task GetPlayer_ExistingPlayer_ReturnsPlayerData()
        {
            var registerRequest = new RegisterRequest { Username = "gettest", Password = "password123" };
            var registerJson = JsonSerializer.Serialize(registerRequest);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            var registerResponse = await _client.PostAsync("/api/v1/Player/register", registerContent);
            var registerResponseContent = await registerResponse.Content.ReadAsStringAsync();
            var registerApiResponse = JsonSerializer.Deserialize<ApiResponse>(registerResponseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = await context.Players.FirstOrDefaultAsync(p => p.Username == "gettest");

            var response = await _client.GetAsync($"/api/v1/Player/{player.Id}");
            var responseContent = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
        }

        [Fact]
        public async Task GetPlayer_NonExistingPlayer_ReturnsNotFound()
        {
            var nonExistentId = Guid.NewGuid();

            var response = await _client.GetAsync($"/api/v1/Player/{nonExistentId}");
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.False(apiResponse.Success);
            Assert.Equal("Игрок не найден", apiResponse.Error.Message);
        }

        [Fact]
        public async Task SearchPlayers_WithFilterAndSorting_ReturnsOnlyFilteredAndSortedPlayers()
        {
            var players = new[] { "user1", "guest", "admin", "user2" };
            foreach (var username in players)
            {
                var request = new RegisterRequest { Username = username, Password = "password123" };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PostAsync("/api/v1/Player/register", content);
            }

            var searchRequest = new PlayerSearchRequest
            {
                Pagination = new PaginationRequest
                {
                    PageNumber = 1,
                    PageSize = 10
                },
                Filter = new PlayerFilterRequest
                {
                    Usernames = new List<string> { "admin", "guest" }
                },
                Sort = new List<SortRequest>
        {
            new SortRequest { Field = "Username", Direction = Core.Models.Enums.SortDirection.Ascending }
        }
            };

            var jsonRequest = JsonSerializer.Serialize(searchRequest);
            var contentRequest = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/Player/search", contentRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);

            var pagedResponse = JsonSerializer.Deserialize<PagedResponse<PlayerResponse>>(apiResponse.Data.ToString(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(pagedResponse);
            Assert.All(pagedResponse.Items, p => Assert.Contains(p.Username, new[] { "guest", "admin" }));

            var usernames = pagedResponse.Items.Select(p => p.Username).ToList();
            Assert.DoesNotContain("user1", usernames);
            Assert.DoesNotContain("user2", usernames);

            var sortedUsernames = usernames.OrderBy(u => u).ToList();
            Assert.Equal(sortedUsernames, usernames);
        }

        [Fact]
        public async Task SearchPlayers_NoFilters_ReturnsAllPlayers()
        {
            await CreateTestPlayers();

            var searchRequest = new PlayerSearchRequest
            {
                Pagination = new PaginationRequest { PageNumber = 1, PageSize = 10 },
                Filter = new PlayerFilterRequest()
            };

            var json = JsonSerializer.Serialize(searchRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/Player/search", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
        }

        [Fact]
        public async Task SearchPlayers_WithUsernameFilter_ReturnsFilteredPlayers()
        {
            await CreateTestPlayers();

            var searchRequest = new PlayerSearchRequest
            {
                Pagination = new PaginationRequest
                {
                    PageNumber = 1,
                    PageSize = 10
                },
                Filter = new PlayerFilterRequest
                {
                    Usernames = new List<string> { "admin", "guest" }
                }
            };

            var json = JsonSerializer.Serialize(searchRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/Player/search", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
        }

        [Fact]
        public async Task SearchPlayers_WithPagination_ReturnsCorrectPage()
        {
            await CreateTestPlayers();

            var searchRequest = new PlayerSearchRequest
            {
                Pagination = new PaginationRequest
                {
                    PageNumber = 1,
                    PageSize = 2
                },
            };

            var json = JsonSerializer.Serialize(searchRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v1/Player/search", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(apiResponse.Success);
            Assert.NotNull(apiResponse.Data);
        }

        private async Task CreateTestPlayers()
        {
            var players = new[] { "admin", "guest", "user1", "user2", "user3" };
            foreach (var username in players)
            {
                var request = new RegisterRequest { Username = username, Password = "password123" };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PostAsync("/api/v1/Player/register", content);
            }
        }
    }
}