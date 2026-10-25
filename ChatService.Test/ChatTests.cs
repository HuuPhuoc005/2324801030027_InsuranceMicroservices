using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ChatService.Api.Commands;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PolicyService.Api.Events;

namespace ChatService.Test;

public class ChatTests : IClassFixture<ChatFactory>
{
    private readonly ChatFactory factory;
    public ChatTests(ChatFactory factory) => this.factory = factory;

    [Fact]
    public async Task AnonymousClientsCannotConnectOrSendNotifications()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/agentsChat/negotiate?negotiateVersion=1", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/Notification", new SendNotificationCommand { Message = "test" })).StatusCode);
    }

    [Fact]
    public async Task AuthenticatedAgentsReceiveMessagesAndPolicyNotifications()
    {
        await using var first = ConnectAs("jimmy.solid");
        await using var second = ConnectAs("danny.solid");
        var received = new TaskCompletionSource<(string, string, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var notification = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        second.On<string, string, string>("ReceiveMessage", (name, avatar, text) => received.TrySetResult((name, avatar, text)));
        second.On<string>("ReceiveNotification", text => { if (text.Contains("sold policy")) notification.TrySetResult(text); });
        await first.StartAsync();
        await second.StartAsync();
        await first.InvokeAsync("SendMessage", "Hello agent");
        var message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(("jimmy.solid", "avatar.png", "Hello agent"), message);
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().Publish(new PolicyCreated { AgentLogin = "jimmy.solid", ProductCode = "TRI" });
        Assert.Contains("jimmy.solid just sold policy for TRI", await notification.Task.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    private HubConnection ConnectAs(string name)
    {
        factory.CreateClient();
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new[] { new Claim(ClaimTypes.Name, name), new Claim("avatar", "avatar.png") },
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(ChatFactory.Secret)), SecurityAlgorithms.HmacSha256)));
        return new HubConnectionBuilder().WithUrl("http://localhost/agentsChat", options =>
        {
            options.Transports = HttpTransportType.LongPolling;
            options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            options.AccessTokenProvider = () => Task.FromResult<string?>(token);
        }).Build();
    }
}

public class ChatFactory : WebApplicationFactory<global::ChatService.Program>
{
    public const string Secret = "THIS_IS_A_RANDOM_SECRET_2e7a1e80-16ee-4e52-b5c6-5e8892453459";
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:Secret"] = Secret,
            ["RabbitMqOptions:Enabled"] = "false",
            ["eureka:client:enabled"] = "false",
            ["eureka:client:shouldRegisterWithEureka"] = "false",
            ["eureka:client:shouldFetchRegistry"] = "false"
        }));
}
