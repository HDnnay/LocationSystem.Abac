namespace Abac.WebApi.Models;


public class Policy
{
    public Guid Id { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string RuleExpression { get; set; } = string.Empty;
    public string Effect { get; set; } = "Allow"; // Allow 或 Deny
    public int Priority { get; set; }
    public bool IsEnabled { get; set; } = true;
}