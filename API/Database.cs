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
        var projects = _db.GetCollection<Types.ProjectData>("projects");
        projects.EnsureIndex(x => x.Id, true);
    }

    private static int ParseProjectId(string? id) => int.TryParse(id, out var n) ? n : int.MaxValue;

    private void RenumberProjects(ILiteCollection<Types.ProjectData> col)
    {
        var all = col.FindAll().OrderBy(p => ParseProjectId(p.Id)).ToList();
        col.DeleteAll();
        for (int i = 0; i < all.Count; i++)
        {
            all[i].Id = (i + 1).ToString();
            col.Insert(all[i]);
        }
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
            Projects = _db.GetCollection<Types.ProjectData>("projects").FindAll().ToList(),
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

    public async Task<List<Types.ProjectData>> GetProjectsAsync()
    {
        return await Task.Run(() => _db.GetCollection<Types.ProjectData>("projects")
            .FindAll()
            .OrderByDescending(p => ParseProjectId(p.Id))
            .ToList());
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

    public async Task<bool> UpdateProjectsAsync(Types.ProjectDataMap projects)
    {
        return await Task.Run(() =>
        {
            var col = _db.GetCollection<Types.ProjectData>("projects");
            col.DeleteAll();
            foreach (var p in projects.Projects)
            {
                if (string.IsNullOrEmpty(p.Id)) continue;
                {
                    p.Id = ObjectId.NewObjectId().ToString();
                }
                col.Insert(p);
            }
            return true;
        });
    }

    public async Task<Types.ProjectData> AddProjectAsync(Types.ProjectData project)
    {
        return await Task.Run(() =>
        {
            ILiteCollection<Types.ProjectData> col = _db.GetCollection<Types.ProjectData>("projects");
            RenumberProjects(col);
            project.Id = (col.Count() + 1).ToString();
            col.Insert(project);
            return project;
        });
    }

    public async Task<bool> DeleteProjectAsync(string id)
    {
        return await Task.Run(() =>
        {
            ILiteCollection<Types.ProjectData> col = _db.GetCollection<Types.ProjectData>("projects");
            if (col.FindById(id) == null) return false;
            col.Delete(id);
            RenumberProjects(col);
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