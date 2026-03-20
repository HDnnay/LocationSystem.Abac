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
        public Guid Id { get; set; }
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
        public decimal Amount { get; set; }
        public string Sensitivity { get; set; } = string.Empty;
        // 可以根据需要添加更多通用属性
        public Dictionary<string, object> CustomAttributes { get; set; } = new();
    }

    public class EnvironmentAttributes
    {
        public DateTime CurrentTime { get; set; }
        public string ClientIp { get; set; } = string.Empty;
    }

}
