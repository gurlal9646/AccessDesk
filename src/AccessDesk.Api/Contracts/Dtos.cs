using System.ComponentModel.DataAnnotations;
using AccessDesk.Api.Domain;

namespace AccessDesk.Api.Contracts;

// On records, validation attributes belong on the constructor parameters (not [property: ...]).
public record CreateAccessRequestDto(
    [Required, StringLength(200, MinimumLength = 2)] string System,
    [Required, StringLength(2000, MinimumLength = 10)] string Justification);

public record DecisionDto([StringLength(2000)] string? Comment);

public record AccessRequestDto(
    int Id,
    string RequesterName,
    string System,
    string Justification,
    AccessRequestStatus Status,
    DateTime CreatedAtUtc,
    string? DecidedBy,
    DateTime? DecidedAtUtc,
    string? DecisionComment)
{
    public static AccessRequestDto From(AccessRequest r) => new(
        r.Id, r.RequesterName, r.System, r.Justification, r.Status,
        r.CreatedAtUtc, r.DecidedBy, r.DecidedAtUtc, r.DecisionComment);
}

public record AuditEntryDto(int Id, DateTime TimestampUtc, string Actor, string Action, string Details);
