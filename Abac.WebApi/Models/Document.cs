namespace Abac.WebApi.Models;

public class Document
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // Draft, UnderReview, Approved
    public string Confidentiality { get; set; } = "Low"; // Low, Medium, High
}