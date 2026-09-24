using LiteDB;
using System.Text.Json.Serialization;

namespace API;

public static class Types
{
    public class ProjectData
    {
        [JsonConstructor]
        public ProjectData() { }
        public string Id { get; set; } = ObjectId.NewObjectId().ToString();
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public List<string>? Technologies { get; set; }
    }

    public class ProjectDataMap
    {
        [JsonConstructor]
        
        public ProjectDataMap() { }
        public ProjectData[] Projects { get; set; }
    }

    public class HomeData
    {
        [JsonConstructor]
        public HomeData() { }
        public string? User { get; set; }
        public string? Blurp { get; set; }
        public string? Avatar { get; set; }
    }

    public class AboutData
    {
        [JsonConstructor]
        public AboutData() { }
        public string? FullName { get; set; }
        public int? Age { get; set; }
        public string? Pronouns { get; set; }
        public string? Bio { get; set; }
        public List<Experience>? JobExperiences { get; set; }
    }

    public class ContactData
    {
        [JsonConstructor]
        public ContactData() { }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Discord { get; set; }
        public string? Twitter { get; set; }
        public string? Linkedin { get; set; }
    }

    public class ImprintData
    {
        [JsonConstructor]
        public ImprintData() { }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class AllData
    {
        [JsonConstructor]
        public AllData() { } 
        public HomeData? User { get; set; }
        public AboutData? About { get; set; }
        public ContactData? Contact { get; set; }
        public List<ProjectData>? Projects { get; set; }
        public ImprintData? Imprint { get; set; }
    }

    public class Experience
    {
        [JsonConstructor]
        public Experience() { }
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? JobDescription { get; set; }
        public string? JobLocation { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}