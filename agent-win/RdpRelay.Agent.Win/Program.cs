using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RdpRelay.Agent.Win.Models;
using RdpRelay.Agent.Win.Services;
using Serilog;

namespace RdpRelay.Agent.Win;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/agent-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting RDP Relay Windows Agent");

            var builder = Host.CreateApplicationBuilder(args);

            // Add Serilog
            builder.Services.AddSerilog();

            // Configure options
            builder.Services.Configure<AgentOptions>(
                builder.Configuration.GetSection("Agent"));

            // Register services
            builder.Services.AddSingleton<ISystemInfoService, SystemInfoService>();
            builder.Services.AddSingleton<IRelayWebSocketClient, RelayWebSocketClient>();
            builder.Services.AddSingleton<IRdpConnectionManager, RdpConnectionManager>();

            // Add the main agent service as Windows Service
            builder.Services.AddWindowsService();
            builder.Services.AddHostedService<AgentService>();

            var host = builder.Build();
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Agent terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
