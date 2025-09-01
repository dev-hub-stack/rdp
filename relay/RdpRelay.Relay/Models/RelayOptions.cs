namespace RdpRelay.Relay.Models;

public class RelayOptions
{
    public string PublicHost { get; set; } = "localhost";
    public int PublicPort { get; set; } = 443;
    public string Bind { get; set; } = ":443";
    public string WssPath { get; set; } = "/api/agent";
    public int InternalPort { get; set; } = 8080;
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(8);
    public string[] AllowedOperatorCidrs { get; set; } = ["0.0.0.0/0"];
    public bool EnableConnectCodes { get; set; } = true;
    public TimeSpan ConnectCodeExpiry { get; set; } = TimeSpan.FromMinutes(5);
}

public class JwtOptions
{
    public string Issuer { get; set; } = "https://portal.local";
    public string Audience { get; set; } = "relay";
    public string SigningKey { get; set; } = "your-super-secret-jwt-signing-key-32-chars-minimum-length";
    public TimeSpan SessionTtl { get; set; } = TimeSpan.FromMinutes(1);
}
