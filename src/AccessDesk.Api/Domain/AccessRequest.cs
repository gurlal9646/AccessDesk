namespace AccessDesk.Api.Domain;

public enum AccessRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class AccessRequest
{
    public int Id { get; set; }
    public string RequesterId { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    public string System { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.Pending;
    public DateTime CreatedAtUtc { get; set; }
    public string? DecidedBy { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public string? DecisionComment { get; set; }
}
