namespace AccessDesk.Api.Domain;

public class AuditEntry
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
