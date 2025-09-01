using Serilog;
using RdpRelay.Relay.Services;
using RdpRelay.Relay.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/relay-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.WithProperty("Service", "RdpRelay")
    .CreateLogger();

builder.Host.UseSerilog();

// Configuration
builder.Services.Configure<RelayOptions>(builder.Configuration.GetSection("Relay"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

// JWT Configuration
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Redis (optional)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
}

// Services
builder.Services.AddSingleton<IAgentRegistry, AgentRegistry>();
builder.Services.AddSingleton<ISessionBroker, SessionBroker>();
builder.Services.AddSingleton<ITcpRelayService, TcpRelayService>();
builder.Services.AddHostedService<RelayBackgroundService>();

var app = builder.Build();

// Configure middleware
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.UseAuthentication();

// WebSocket endpoint for agents
app.Map("/api/agent", async (HttpContext context, IAgentRegistry agentRegistry) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var agentConnection = new AgentConnection(webSocket, context.RequestAborted);
    
    await agentRegistry.HandleAgentConnectionAsync(agentConnection);
});

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Metrics endpoint
app.MapGet("/metrics", async (ISessionBroker sessionBroker, IAgentRegistry agentRegistry) =>
{
    var metrics = new
    {
        active_agents = await agentRegistry.GetActiveAgentCountAsync(),
        active_sessions = await sessionBroker.GetActiveSessionCountAsync(),
        timestamp = DateTime.UtcNow
    };
    return Results.Ok(metrics);
});

try
{
    Log.Information("Starting RDP Relay Server");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
