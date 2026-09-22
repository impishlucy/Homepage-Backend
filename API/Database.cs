using LiteDB;

namespace API;

public class Database : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly bool _disposeDb;

    public Database()
    {
        var dbPath = "website.db";
        
        // Ensure the directory exists before LiteDB tries to create the file
        var directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // ConnectionType.Shared is the recommended best practice for ASP.NET Core web apps
        var connectionString = new ConnectionString 
        { 
            Filename = dbPath, 
            Connection = ConnectionType.Shared 
        };
        
        _db = new LiteDatabase(connectionString);
        _disposeDb = true;

        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        var projects = _db.GetCollection<Types.ProjectData>("projects");
        projects.EnsureIndex(x => x.Id, true);
    }

    public bool CheckDBHealth()
    {
        try
        {
            // A simple ping to verify the database file is accessible
            _db.GetCollection<Types.HomeData>("home").Count();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database health check failed: {ex.Message}");
            return false;
        }
    }

    public async Task<Types.AllData> GetAllDataAsync()
    {
        return await Task.Run(() => new Types.AllData
        {
            User = _db.GetCollection<Types.HomeData>("home").FindOne(x => true),
            About = _db.GetCollection<Types.AboutData>("about").FindOne(x => true),
            Contact = _db.GetCollection<Types.ContactData>("contact").FindOne(x => true),
            Projects = _db.GetCollection<Types.ProjectData>("projects").FindAll().ToList(),
            Imprint =  _db.GetCollection<Types.ImprintData>("imprint").FindOne(x => true),
        });
    }

    public async Task<Types.HomeData> GetHomeDataAsync()
    {
        return await Task.Run(() =>
        {
            var user = _db.GetCollection<Types.HomeData>("home");
            var userInfo = user.FindOne(x => true);
            return userInfo;
        });
    }
    
    public async Task<Types.AboutData> GetAboutDataAsync()
    {
        return await Task.Run(() =>
        {
            var user = _db.GetCollection<Types.AboutData>("about");
            var userInfo = user.FindOne(x => true);
            return userInfo;
        });
    }
    
    public async Task<Types.ContactData> GetContactDataAsync()
    {
        return await Task.Run(() =>
        {
            var user = _db.GetCollection<Types.ContactData>("contact");
            var userInfo = user.FindOne(x => true);
            return userInfo;
        });
    }

    public async Task<List<Types.ProjectData>> GetProjectsAsync()
    {
        return await Task.Run(() => _db.GetCollection<Types.ProjectData>("projects")
            .Query()
            .OrderByDescending(x => x.Id)
            .ToList());
    }
    
    public async Task<Types.ImprintData> GetImprintDataAsync()
    {
        return await Task.Run(() =>
        {
            var user = _db.GetCollection<Types.ImprintData>("imprint");
            var userInfo = user.FindOne(x => true);
            return userInfo;
        });
    }
    
    public async Task<string> GetLoginDataAsync()
    {
        return await Task.Run(() =>
        {
            var user = _db.GetCollection<string>("login-url");
            var userInfo = user.FindOne(x => true);
            return userInfo;
        });
    }

    public async Task<bool> UpdateUserBasicAsync(Types.HomeData data)
    {
        return await Task.Run(() =>
        {
            var users = _db.GetCollection<Types.HomeData>("user");
            var user = users.FindOne(x => true);
            
            if (user == null) return false;

            bool updated = false;
            if (data.User != null) { user.User = data.User; updated = true; }
            if (data.Blurp != null) { user.Blurp = data.Blurp; updated = true; }
            if (data.Avatar != null) { user.Avatar = data.Avatar; updated = true; }

            if (updated)
            {
                return users.Update(user);
            }

            return false;
        });
    }

    public void Dispose()
    {
        if (_disposeDb)
        {
            _db.Dispose();
        }
    }
}