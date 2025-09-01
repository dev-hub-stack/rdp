using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RdpRelay.Portal.Api.Models;
using RdpRelay.Portal.Api.Services;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure options
builder.Services.Configure<PortalOptions>(builder.Configuration.GetSection("Portal"));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection("MongoDb"));

// Add services
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<SessionService>();

// Add authentication
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey))
        };
    });

builder.Services.AddAuthorization();

// Add controllers
builder.Services.AddControllers();

// Add OpenAPI
builder.Services.AddOpenApi();

// Add CORS
var portalOptions = builder.Configuration.GetSection("Portal").Get<PortalOptions>();
var corsOrigins = builder.Configuration.GetSection("Portal:CorsOrigins").Get<string[]>() ?? 
    new[] { "http://localhost:3000", "http://localhost:8080", "http://192.168.18.101:8080", "http://192.168.18.101:3000" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        corsBuilder.WithOrigins(corsOrigins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Ensure database indexes are created
var mongoService = app.Services.GetRequiredService<MongoDbService>();
await mongoService.EnsureIndexesAsync();

try
{
    Log.Information("Starting Portal API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Portal API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
