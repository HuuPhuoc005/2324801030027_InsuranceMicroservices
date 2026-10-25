using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AuthService.Test;

public class AuthenticationTests : IClassFixture<AuthFactory>
{
    private readonly HttpClient client;
    public AuthenticationTests(AuthFactory factory) => client = factory.CreateClient();

    [Theory]
    [InlineData("unknown.agent", "secret")]
    [InlineData("jimmy.solid", "incorrect")]
    public async Task InvalidCredentialsReturnBadRequest(string login, string password)
    {
        var response = await client.PostAsJsonAsync("/api/User", new { login, password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginIssuesTokenAndProfileDoesNotExposePassword()
    {
        var response = await client.PostAsJsonAsync("/api/User", new { login = "jimmy.solid", password = "secret" });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("jimmy.solid", login.GetProperty("userName").GetString());
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/User");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("token").GetString());
        var profile = await client.SendAsync(request);
        profile.EnsureSuccessStatusCode();
        var body = await profile.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("jimmy.solid", body.GetProperty("login").GetString());
        Assert.False(body.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task ProfileRequiresToken() =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/User")).StatusCode);
}

public class AuthFactory : WebApplicationFactory<global::AuthService.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["eureka:client:enabled"] = "false",
            ["eureka:client:shouldRegisterWithEureka"] = "false",
            ["eureka:client:shouldFetchRegistry"] = "false"
        }));
}
