namespace Abac.WebApi.Models;


public class User
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // 演示用，实际应哈希
    public string Department { get; set; } = string.Empty;
    public int Level { get; set; }
    public List<string> Roles { get; set; } = new();
}