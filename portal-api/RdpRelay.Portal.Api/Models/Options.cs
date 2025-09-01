namespace RdpRelay.Portal.Api.Models;

public class PortalOptions
{
    public string Bind { get; set; } = ":8443";
    public string PublicUrl { get; set; } = "https://localhost:8443";
    public string RelayPublicHost { get; set; } = "localhost";
    public int RelayPublicPort { get; set; } = 443;
}

public class JwtOptions
{
    public string Issuer { get; set; } = "https://portal.local";
    public string Audience { get; set; } = "portal";
    public string SigningKey { get; set; } = "your-super-secret-jwt-signing-key-32-chars-minimum-length";
    public TimeSpan AccessTokenExpiry { get; set; } = TimeSpan.FromHours(8);
    public TimeSpan SessionTokenExpiry { get; set; } = TimeSpan.FromMinutes(1);
}

public class MongoDbOptions
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017/rrdp";
    public string DatabaseName { get; set; } = "rrdp";
}

public class SecurityOptions
{
    public bool RequireConnectCode { get; set; } = true;
    public TimeSpan ConnectCodeExpiry { get; set; } = TimeSpan.FromMinutes(5);
    public string[] AllowedOperatorCidrs { get; set; } = ["0.0.0.0/0"];
}
