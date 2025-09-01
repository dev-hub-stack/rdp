using System.Diagnostics;
using System.Management;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using RdpRelay.Agent.Win.Models;

namespace RdpRelay.Agent.Win.Services;

public interface ISystemInfoService
{
    Task<SystemInfo> GetSystemInfoAsync();
    Task<bool> IsRdpEnabledAsync();
    Task<int> GetActiveRdpSessionsCountAsync();
}

public class SystemInfoService : ISystemInfoService
{
    private readonly ILogger<SystemInfoService> _logger;

    public SystemInfoService(ILogger<SystemInfoService> logger)
    {
        _logger = logger;
    }

    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var systemInfo = new SystemInfo
                {
                    ComputerName = Environment.MachineName,
                    OperatingSystem = GetOperatingSystem(),
                    ProcessorCount = Environment.ProcessorCount,
                    TotalMemoryMB = GetTotalMemoryMB(),
                    Architecture = GetArchitecture(),
                    IpAddress = GetLocalIPAddress(),
                    RdpEnabled = IsRdpEnabledSync(),
                    LastUpdated = DateTime.UtcNow
                };

                return systemInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system information");
                throw;
            }
        });
    }

    public async Task<bool> IsRdpEnabledAsync()
    {
        return await Task.Run(() => IsRdpEnabledSync());
    }

    public async Task<int> GetActiveRdpSessionsCountAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var processCount = Process.GetProcessesByName("rdpclip").Length;
                return processCount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get active RDP sessions count");
                return 0;
            }
        });
    }

    private string GetOperatingSystem()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            using var collection = searcher.Get();
            
            foreach (var obj in collection)
            {
                return obj["Caption"]?.ToString() ?? "Unknown";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get OS information via WMI");
        }

        return $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
    }

    private long GetTotalMemoryMB()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            using var collection = searcher.Get();
            
            foreach (var obj in collection)
            {
                if (obj["TotalPhysicalMemory"] is ulong totalBytes)
                {
                    return (long)(totalBytes / (1024 * 1024));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get memory information via WMI");
        }

        return 0;
    }

    private string GetArchitecture()
    {
        return Environment.Is64BitOperatingSystem ? "x64" : "x86";
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up 
                          && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var networkInterface in networkInterfaces)
            {
                var properties = networkInterface.GetIPProperties();
                var addresses = properties.UnicastAddresses
                    .Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                                && !System.Net.IPAddress.IsLoopback(addr.Address))
                    .Select(addr => addr.Address.ToString());

                var firstAddress = addresses.FirstOrDefault();
                if (!string.IsNullOrEmpty(firstAddress))
                {
                    return firstAddress;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get local IP address");
        }

        return "Unknown";
    }

    private bool IsRdpEnabledSync()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Terminal Server");
            
            if (key != null)
            {
                var value = key.GetValue("fDenyTSConnections");
                return value != null && (int)value == 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check RDP status via registry");
        }

        return false;
    }
}
