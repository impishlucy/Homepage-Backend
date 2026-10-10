using LiteDB;

namespace API;

public class Database : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly bool _disposeDb;

    public Database()
    {
        var dbPath = "website.db";

        var directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

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
        var projects = _db.GetCollection<Types.Project>("projects");
        projects.EnsureIndex(x => x.Id, true);
    }
    

    private bool UpsertSingleton<T>(string collection, T data)
    {
        var col = _db.GetCollection<T>(collection);
        col.DeleteAll();
        col.Insert(data);
        return true;
    }

    public bool CheckDBHealth()
    {
        try
        {
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
            Projects = _db.GetCollection<Types.ProjectData>("projects").FindOne(x => true),
            Imprint = _db.GetCollection<Types.ImprintData>("imprint").FindOne(x => true),
        });
    }

    public async Task<Types.HomeData> GetHomeDataAsync()
    {
        return await Task.Run(() => _db.GetCollection<Types.HomeData>("home").FindOne(x => true));
    }

    public async Task<Types.AboutData> GetAboutDataAsync()
    {
        return await Task.Run(() => _db.GetCollection<Types.AboutData>("about").FindOne(x => true));
    }

    public async Task<Types.ContactData> GetContactDataAsync()
    {
        return await Task.Run(() => _db.GetCollection<Types.ContactData>("contact").FindOne(x => true));
    }

    public async Task<Types.ProjectData> GetProjectsAsync()
    {
        return await Task.Run(() => _db.GetCollection<Types.ProjectData>("projects").FindOne(x => true));
    }

    public async Task<Types.ImprintData> GetImprintDataAsync()
    {
        return await Task.Run(() => _db.GetCollection<Types.ImprintData>("imprint").FindOne(x => true));
    }

    public async Task<bool> UpdateHomeAsync(Types.HomeData data) =>
        await Task.Run(() => UpsertSingleton("home", data));

    public async Task<bool> UpdateAboutAsync(Types.AboutData data) =>
        await Task.Run(() => UpsertSingleton("about", data));

    public async Task<bool> UpdateContactAsync(Types.ContactData data) =>
        await Task.Run(() => UpsertSingleton("contact", data));

    public async Task<bool> UpdateImprintAsync(Types.ImprintData data) =>
        await Task.Run(() => UpsertSingleton("imprint", data));

    public async Task<bool> UpdateProjectsAsync(Types.ProjectData projects)
    {
        return await Task.Run(() =>
        {
            //Delte outdated data
            _db.GetCollection<Types.Project>("projects").DeleteAll();
            _db.GetCollection<Types.ProjectData>("projects").DeleteAll();
            
            var col = _db.GetCollection<Types.ProjectData>("projects");
            col.DeleteAll();
            
            //Insert new Data
            col.Insert(projects);
            
            return true;
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