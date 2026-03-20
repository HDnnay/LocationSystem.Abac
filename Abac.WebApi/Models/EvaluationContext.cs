namespace Abac.WebApi.Models
{
    public class EvaluationContext
    {
        public UserAttributes User { get; set; } = new();
        public ResourceAttributes Resource { get; set; } = new();
        public EnvironmentAttributes Environment { get; set; } = new();
    }

    public class UserAttributes
    {
        public string Id { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int Level { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class ResourceAttributes
    {
        public string Type { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public string Department { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Confidentiality { get; set; } = string.Empty;
    }

    public class EnvironmentAttributes
    {
        public DateTime CurrentTime { get; set; }
        public string ClientIp { get; set; } = string.Empty;
    }

}
