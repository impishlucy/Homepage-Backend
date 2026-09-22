namespace API;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this WebApplication app)
    {
        var dataGroup = app.MapGroup("/data");
        
        // GET /data/home
        dataGroup.MapGet("/home", async (Database db) =>
        {
            try
            {
                var home = await db.GetHomeDataAsync();
                return Results.Ok(home);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Database error: {ex.Message}");
            }
        });
        
        // GET /data/about
        dataGroup.MapGet("/about", async (Database db) =>
        {
            try
            {
                var about = await db.GetAboutDataAsync();
                return Results.Ok(about);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Database error: {ex.Message}");
            }
        });
        
        // GET /data/contact
        dataGroup.MapGet("/contact", async (Database db) =>
        {
            try
            {
                var contact = await db.GetContactDataAsync();
                return Results.Ok(contact);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Database error: {ex.Message}");
            }
        });
        
        
        // GET /data/projects
        dataGroup.MapGet("/projects", async (Database db) =>
        {
            try
            {
                var projects = await db.GetProjectsAsync();
                return Results.Ok(projects);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Database error: {ex.Message}");
            }
        });
        
        // GET /data/imprint
        dataGroup.MapGet("/imprint", async (Database db) =>
        {
            try
            {
                var imprint = await db.GetImprintDataAsync();
                return Results.Ok(imprint);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Database error: {ex.Message}");
            }
        });
        
        // GET /data/login
        dataGroup.MapGet("/login", () =>
        {
            var clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
            var redirectUri = Environment.GetEnvironmentVariable("DISCORD_REDIRECT_URI");
    
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
            {
                return Results.Problem("Missing Discord OAuth configuration on server.");
            }

            var scope = "identify";
            // Build the URL exactly as Discord requires
            var authUrl = $"https://discord.com/oauth2/authorize?client_id={clientId}&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&scope={scope}";
    
            return Results.Ok(new { url = authUrl });
        });
    }
}