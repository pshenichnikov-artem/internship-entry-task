using Core.Models.DTOs.Common;
using Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace IntegrationTests
{
    public abstract class IntegrationTestBase
    {
        protected readonly WebApplicationFactory<Program> _factory;
        protected readonly HttpClient _client;
        protected readonly Dictionary<string, string> _authTokens = new();

        protected IntegrationTestBase(WebApplicationFactory<Program> baseFactory, string dbName, Dictionary<string, string>? extraSettings = null)
        {
            _factory = baseFactory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var defaultSettings = new Dictionary<string, string>
                    {
                        ["Game:BoardSize"] = "3",
                        ["Game:WinConditionLength"] = "3",
                        ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                        ["Jwt:Key"] = "12345678901234567890123456789012",
                        ["Jwt:Issuer"] = "test_issuer",
                        ["Jwt:Audience"] = "test_audience"
                    };

                    if (extraSettings != null)
                    {
                        foreach (var kv in extraSettings)
                            defaultSettings[kv.Key] = kv.Value;
                    }

                    config.AddInMemoryCollection(defaultSettings);
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));
                });
            });

            _client =_factory.CreateClient();
        }

        protected async Task<Guid> RegisterAsync(string username)
        {
            var registerRequest = new { Username = username, Password = "password123" };
            var regContent = new StringContent(JsonSerializer.Serialize(registerRequest), Encoding.UTF8, "application/json");
            var regResponse = await _client.PostAsync("/api/v1/Player/register", regContent);
            regResponse.EnsureSuccessStatusCode();

            var regJson = await regResponse.Content.ReadAsStringAsync();
            var regData = JsonSerializer.Deserialize<ApiResponse>(regJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var token = ((JsonElement)regData!.Data!).GetString()!;

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var player = await context.Players.FirstAsync(p => p.Username == username);

            _authTokens[username] = token;

            return player.Id;
        }

        protected void UseToken(string username)
        {
            if (_authTokens.TryGetValue(username, out var token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                throw new InvalidOperationException($"Token for user '{username}' not found. Call RegisterAndLoginAsync first.");
            }
        }

        protected StringContent Serialize<T>(T obj) =>
            new(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

        protected async Task<ApiResponse> DeserializeApiResponseAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }
    }
}
