using MediatR;
using Steeltoe.Discovery.Client;
using AuthService.Api.Queries;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
builder.Services.AddDiscoveryClient(builder.Configuration);
var app = builder.Build();
app.MapControllers();
app.MapGet("/health", (ISender sender, CancellationToken ct) => sender.Send(new GetHealthQuery(), ct));
app.MapGet("/api/auth/health", (ISender sender, CancellationToken ct) => sender.Send(new GetHealthQuery(), ct));
app.Run();
public partial class Program { }
