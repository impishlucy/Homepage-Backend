using API;
using System.Collections.Concurrent;
using System.Diagnostics;
using DotNetEnv;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
Env.Load();

builder.Services.AddSingleton<Database>();
builder.Services.AddEndpointsApiExplorer();

// CLEANED UP: Removed DefaultJsonTypeInfoResolver as .csproj handles reflection globally
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "https://lucy-codes.de";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("X-New-Token");
    });
});

var app = builder.Build();

var db = app.Services.GetRequiredService<Database>();
if (!db.CheckDBHealth())
{
    Console.WriteLine("Database health check failed. Shutting down...");
    return;
}

app.UseCors("AllowFrontend");

var rateLimitStore = new ConcurrentDictionary<string, RateLimitInfo>();

app.Use(async (context, next) =>
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var now = DateTime.UtcNow;
    
    if (!rateLimitStore.TryGetValue(clientIp, out var rateInfo))
    {
        rateInfo = new RateLimitInfo();
        rateLimitStore[clientIp] = rateInfo;
    }
    
    if (rateInfo.BlockedUntil > now)
    {
        context.Response.StatusCode = 429;
        await context.Response.WriteAsJsonAsync(new ErrorRetryResponse(
            "Too many requests. Try again later.",
            (int)(rateInfo.BlockedUntil - now).TotalSeconds 
        ));
        return;
    }
    
    rateInfo.Requests.RemoveAll(r => r < now.AddSeconds(-5));
    
    if (rateInfo.Requests.Count >= 15)
    {
        rateInfo.BlockedUntil = now.AddMinutes(2);
        context.Response.StatusCode = 429;
        await context.Response.WriteAsJsonAsync(new ErrorRetryResponse(
            "Rate limit exceeded. Blocked for 2 minutes.",
            120 
        ));
        return;
    }
    
    rateInfo.Requests.Add(now);
    
    await next();
});

var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "YourAPI";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "YourAPIApp";

app.MapAdminEndpoints(jwtKey, jwtIssuer, jwtAudience);
app.MapPublicEndpoints();

app.Run();



public class RateLimitInfo
{
    [System.Text.Json.Serialization.JsonConstructor]
    public RateLimitInfo() { }
    public List<DateTime> Requests { get; set; } = new();
    public DateTime BlockedUntil { get; set; } = DateTime.MinValue;
}

public record UrlResponse(string url);
public record ErrorResponse(string error);
public record ErrorRetryResponse(string error, int retryAfter);
public record MessageResponse(string message);
public record LoginResponse(string accessToken, string refreshToken, int expiresIn);