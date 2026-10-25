using System.Net.Http.Json;
using System.Text.Json;
using EasyNetQ;
using EasyNetQ.Topology;
using Microsoft.AspNetCore.SignalR.Client;
using PolicyService.Api.Events;

using var http = new HttpClient { BaseAddress = new Uri("http://localhost:8099") };
var loginResponse = await http.PostAsJsonAsync("/api/User", new { login = "jimmy.solid", password = "secret" });
loginResponse.EnsureSuccessStatusCode();
var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
var token = login.GetProperty("token").GetString()!;
await using var chat = new HubConnectionBuilder().WithUrl("http://localhost:8099/agentsChat", options =>
{
    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
}).Build();
var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
var notification = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
var policyNumber = "bai6_smoke_" + Guid.NewGuid().ToString("N");
var agent = policyNumber;
chat.On<string, string, string>("ReceiveMessage", (name, avatar, message) => received.TrySetResult(message));
chat.On<string>("ReceiveNotification", message => { if (message.Contains(agent)) notification.TrySetResult(message); });
await chat.StartAsync();
await chat.InvokeAsync("SendMessage", policyNumber);
if (await received.Task.WaitAsync(TimeSpan.FromSeconds(15)) != policyNumber) throw new Exception("SignalR message mismatch.");
Console.WriteLine("Auth JWT -> Gateway -> SignalR WebSocket -> ReceiveMessage: PASS");

using var elastic = new HttpClient { BaseAddress = new Uri("http://localhost:9200") };
try
{
    using var bus = RabbitHutch.CreateBus(Environment.GetEnvironmentVariable("BAI6_RABBIT_CONNECTION") ?? "host=localhost;username=guest;password=guest");
    var exchange = bus.Advanced.ExchangeDeclare("lab-dotnet-micro", ExchangeType.Topic);
    var policy = new PolicyCreated
    {
        PolicyNumber = policyNumber, AgentLogin = agent, ProductCode = "TRI", TotalPremium = 123,
        PolicyFrom = DateTime.UtcNow.Date, PolicyTo = DateTime.UtcNow.Date.AddYears(1),
        PolicyHolder = new() { FirstName = "Smoke", LastName = "Test" }
    };
    await bus.Advanced.PublishAsync(exchange, "policycreated", false, new Message<PolicyCreated>(policy));
    await notification.Task.WaitAsync(TimeSpan.FromSeconds(20));
    Console.WriteLine("RabbitMQ PolicyCreated -> Chat ReceiveNotification: PASS");
    var indexed = false;
    for (var attempt = 0; attempt < 40; attempt++)
    {
        var response = await elastic.GetAsync($"/policy_lab_stats/_doc/{policyNumber}");
        if (response.IsSuccessStatusCode)
        {
            var document = await response.Content.ReadFromJsonAsync<JsonElement>();
            var source = document.GetProperty("_source");
            if (source.GetProperty("totalPremium").GetDecimal() != 123 || source.GetProperty("agentLogin").GetString() != agent)
                throw new Exception("Indexed policy mismatch.");
            indexed = true;
            break;
        }
        await Task.Delay(500);
    }
    if (!indexed) throw new Exception("RabbitMQ event was not indexed by Dashboard.");
    Console.WriteLine("RabbitMQ PolicyCreated -> Dashboard -> Elasticsearch policy document: PASS");
}
finally
{
    // Delete only this run's generated document; existing sales data is preserved.
    var response = await elastic.DeleteAsync($"/policy_lab_stats/_doc/{policyNumber}?refresh=true");
    if (response.StatusCode != System.Net.HttpStatusCode.NotFound) response.EnsureSuccessStatusCode();
}
