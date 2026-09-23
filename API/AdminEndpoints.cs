using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json;
using DotNetEnv;

namespace API;

public static class AdminEndpoints
{
    private static readonly HttpClient _httpClient = new();
    
    private static readonly JsonSerializerOptions _jsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true 
    };

    public static void MapAdminEndpoints(this WebApplication app, string jwtKey, string jwtIssuer, string jwtAudience)
    {
        Env.Load();
        var adminGroup = app.MapGroup("/admin");
        
        adminGroup.MapGet("/login", async (HttpContext ctx, string? code) =>
        {
            if (string.IsNullOrEmpty(code))
            {
                return Results.BadRequest(new ErrorResponse("Missing authorization code"));
            }

            var clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("DISCORD_CLIENT_SECRET");
            var redirectUri = Environment.GetEnvironmentVariable("DISCORD_REDIRECT_URI");
            var adminId = Environment.GetEnvironmentVariable("ADMIN_ID");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
            {
                return Results.BadRequest(new ErrorResponse("Server OAuth configuration error."));
            }

            var tokenRequest = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri }
            };

            var tokenHttpRequest = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
            {
                Content = new FormUrlEncodedContent(tokenRequest)
            };

            var tokenResponse = await _httpClient.SendAsync(tokenHttpRequest);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                return Results.BadRequest(new ErrorResponse("Failed to exchange code for token"));
            }

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(tokenJson);
            
            if (!document.RootElement.TryGetProperty("access_token", out var accessTokenElement))
            {
                return Results.BadRequest(new ErrorResponse("Invalid token response from Discord"));
            }
            var accessToken = accessTokenElement.GetString();

            var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
            userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            
            var userResponse = await _httpClient.SendAsync(userRequest);
            if (!userResponse.IsSuccessStatusCode)
            {
                return Results.Unauthorized();
            }
            
            var userJson = await userResponse.Content.ReadAsStringAsync();
            
            // Deserializes cleanly now because DiscordUser has an explicit parameterless constructor
            var discordUser = JsonSerializer.Deserialize<DiscordUser>(userJson, _jsonOptions);
            
            if (discordUser?.Id != adminId)
            {
                return Results.Forbid();
            }
            
            var jwtToken = GenerateJwtToken(discordUser.Id, jwtKey, jwtIssuer, jwtAudience);
            var refreshToken = GenerateRefreshToken();
            
            return Results.Ok(new LoginResponse(jwtToken, refreshToken, 3600));
        });
        
        adminGroup.AddEndpointFilter(async (ctx, next) =>
        {
            var path = ctx.HttpContext.Request.Path.Value;
            if (path != null && path.EndsWith("/login"))
            {
                return await next(ctx);
            }

            var authHeader = ctx.HttpContext.Request.Headers["Authorization"].ToString();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Results.Unauthorized();
            }
            
            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            try
            {
                var principal = ValidateJwtToken(token, jwtKey, jwtIssuer, jwtAudience);
                ctx.HttpContext.Items["User"] = principal;
                
                var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var newToken = userId != null ? GenerateJwtToken(userId, jwtKey, jwtIssuer, jwtAudience) : null;
                
                var result = await next(ctx);
                
                if (newToken != null && result is IResult okResult)
                {
                    ctx.HttpContext.Response.Headers["X-New-Token"] = newToken;
                }
                
                return result;
            }
            catch
            {
                return Results.Unauthorized();
            }
        });
        
        adminGroup.MapGet("/dashboard", async (Database db) =>
        {
            try
            {
                var data = await db.GetAllDataAsync();
                return Results.Ok(data);
            }
            catch (Exception ex)
            {
                return Results.Json(new ErrorResponse($"Database error: {ex.Message}"), statusCode: 500);
            }
        });
        
        adminGroup.MapGet("/test", (Database _) =>
        {
            try
            {
                return Task.FromResult(Results.Ok());
            }
            catch (Exception ex)
            {
                return Task.FromResult(Results.Json(new ErrorResponse($"Database error: {ex.Message}"), statusCode: 500));
            }
        });
        
        adminGroup.MapPut("/update/home", (HttpContext ctx, Database db) =>
            HandleUpdate<Types.HomeData>(ctx, db, (d, p) => d.UpdateHomeAsync(p)));
            
        adminGroup.MapPut("/update/about", (HttpContext ctx, Database db) =>
            HandleUpdate<Types.AboutData>(ctx, db, (d, p) => d.UpdateAboutAsync(p)));
            
        adminGroup.MapPut("/update/contact", (HttpContext ctx, Database db) =>
            HandleUpdate<Types.ContactData>(ctx, db, (d, p) => d.UpdateContactAsync(p)));
            
        adminGroup.MapPut("/update/imprint", (HttpContext ctx, Database db) =>
            HandleUpdate<Types.ImprintData>(ctx, db, (d, p) => d.UpdateImprintAsync(p)));
            
        adminGroup.MapPut("/update/projects", (HttpContext ctx, Database db) =>
            HandleUpdate<List<Types.ProjectData>>(ctx, db, (d, p) => d.UpdateProjectsAsync(p)));
        
        adminGroup.MapPost("/projects", async (HttpContext ctx, Database db) =>
        {
            if (GetUserId(ctx) == null) return Results.Unauthorized();
            using var reader = new StreamReader(ctx.Request.Body);
            var body = await reader.ReadToEndAsync();
            var project = JsonSerializer.Deserialize<Types.ProjectData>(body, _jsonOptions);
            if (project == null) return Results.BadRequest(new ErrorResponse("Invalid request body"));
            var created = await db.AddProjectAsync(project);
            return Results.Ok(created);
        });
        
        adminGroup.MapDelete("/projects/{id}", async (HttpContext ctx, Database db, string id) =>
        {
            if (GetUserId(ctx) == null) return Results.Unauthorized();
            var success = await db.DeleteProjectAsync(id);
            return success
                ? Results.Ok(new MessageResponse("Project deleted"))
                : Results.NotFound(new ErrorResponse("Project not found"));
        });
    }
    
    private static string? GetUserId(HttpContext ctx) =>
        (ctx.Items["User"] as ClaimsPrincipal)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
    private static async Task<IResult> HandleUpdate<T>(HttpContext ctx, Database db, Func<Database, T, Task<bool>> update)
    {
        if (GetUserId(ctx) == null) return Results.Unauthorized();
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<T>(body, _jsonOptions);
        if (payload == null) return Results.BadRequest(new ErrorResponse("Invalid request body"));
        var success = await update(db, payload);
        return success
            ? Results.Ok(new MessageResponse("Updated successfully"))
            : Results.Json(new ErrorResponse("Update failed"), statusCode: 500);
    }
    
    private static string GenerateJwtToken(string userId, string key, string issuer, string audience)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        var token = new JwtSecurityToken(issuer, audience, claims, null, DateTime.UtcNow.AddHours(1), credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    }
    
    private static ClaimsPrincipal? ValidateJwtToken(string token, string key, string issuer, string audience)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}

public class DiscordUser
{
    [System.Text.Json.Serialization.JsonConstructor]
    public DiscordUser() { }
    
    public string Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string Discriminator { get; set; } = "";
    public string? Avatar { get; set; }
}