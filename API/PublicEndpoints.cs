using DotNetEnv;

namespace API;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this WebApplication app)
    {
        Env.Load();
        var dataGroup = app.MapGroup("/data");
        
        dataGroup.MapGet("/home", async (Database db) =>
        {
            try
            {
                var home = await db.GetHomeDataAsync();
                return Results.Ok(home ?? new Types.HomeData());
            }
            catch (Exception ex)
            {
                return Results.Json(new ErrorResponse($"Database error: {ex.Message}"), statusCode: 500);
            }
        });
        
        dataGroup.MapGet("/about", async (Database db) =>
        {
            try
            {
                var about = await db.GetAboutDataAsync();
                return Results.Ok(about ?? new Types.AboutData());
            }
            catch (Exception ex)
            {
                return Results.Json(new ErrorResponse($"Database error: {ex.Message}"), statusCode: 500);
            }
        });
        
        dataGroup.MapGet("/contact", async (Database db) =>
        {
            try
            {
                var contact = await db.GetContactDataAsync();
                return Results.Ok(contact ?? new Types.ContactData());
            }
            catch (Exception ex)
            {
                return Results.Json(new ErrorResponse($"Database error: {ex.Message}"), statusCode: 500);
            }
        });
        
        dataGroup.MapGet("/projects", async (Database db) =>
        {
            try
            {
                var projects = await db.GetProjectsAsync();
                return Results.Ok(projects ?? new List<Types.ProjectData>());
            }
            catch (Exception ex)
            {
                return Results.Json(new ErrorResponse($"Database error: {ex.Message}"), statusCode: 500);
            }
        });
        
        dataGroup.MapGet("/imprint", async (Database db) =>
        {
            try
            {
                var imprint = await db.GetImprintDataAsync();
                return Results.Ok(imprint ?? new Types.ImprintData());
            }
            catch (Exception ex)
            {
                return Results.Json(new ErrorResponse($"Database error: {ex.Message}"), statusCode: 500);
            }
        });
        
        dataGroup.MapGet("/login", () =>
        {
            var clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
            var redirectUri = Environment.GetEnvironmentVariable("DISCORD_REDIRECT_URI");
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            {
                return Results.BadRequest(new ErrorResponse("Missing Discord OAuth configuration on server."));
            }
            
            var scope = "identify";
            var authUrl = $"https://discord.com/oauth2/authorize?client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={scope}";
            
            return Results.Ok(new UrlResponse(authUrl));
        });
    }
}