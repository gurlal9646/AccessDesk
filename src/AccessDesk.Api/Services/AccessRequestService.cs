using AccessDesk.Api.Data;
using AccessDesk.Api.Domain;
using AccessDesk.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace AccessDesk.Api.Services;

public enum DecisionOutcome
{
    Ok,
    NotFound,
    AlreadyDecided,
    CannotDecideOwnRequest
}

public record DecisionResult(DecisionOutcome Outcome, AccessRequest? Request = null);

public interface IAccessRequestService
{
    Task<AccessRequest> CreateAsync(CurrentUser user, string system, string justification, CancellationToken ct = default);
    Task<IReadOnlyList<AccessRequest>> ListAsync(CurrentUser user, AccessRequestStatus? status, CancellationToken ct = default);
    Task<DecisionResult> DecideAsync(CurrentUser manager, int id, bool approve, string? comment, CancellationToken ct = default);
}

public class AccessRequestService(AccessDeskDbContext db, TimeProvider clock) : IAccessRequestService
{
    public async Task<AccessRequest> CreateAsync(CurrentUser user, string system, string justification, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var request = new AccessRequest
        {
            RequesterId = user.Id,
            RequesterName = user.Name,
            System = system.Trim(),
            Justification = justification.Trim(),
            Status = AccessRequestStatus.Pending,
            CreatedAtUtc = now
        };
        db.AccessRequests.Add(request);
        db.AuditEntries.Add(new AuditEntry
        {
            TimestampUtc = now,
            Actor = user.Name,
            Action = "request.created",
            Details = $"Requested access to {request.System}"
        });
        await db.SaveChangesAsync(ct);
        return request;
    }

    public async Task<IReadOnlyList<AccessRequest>> ListAsync(CurrentUser user, AccessRequestStatus? status, CancellationToken ct = default)
    {
        IQueryable<AccessRequest> query = db.AccessRequests.AsNoTracking();
        if (!user.IsManager)
        {
            query = query.Where(r => r.RequesterId == user.Id);
        }
        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }
        return await query.OrderByDescending(r => r.CreatedAtUtc).ThenByDescending(r => r.Id).ToListAsync(ct);
    }

    public async Task<DecisionResult> DecideAsync(CurrentUser manager, int id, bool approve, string? comment, CancellationToken ct = default)
    {
        var request = await db.AccessRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request is null)
        {
            return new DecisionResult(DecisionOutcome.NotFound);
        }
        if (request.RequesterId == manager.Id)
        {
            return new DecisionResult(DecisionOutcome.CannotDecideOwnRequest, request);
        }
        if (request.Status != AccessRequestStatus.Pending)
        {
            return new DecisionResult(DecisionOutcome.AlreadyDecided, request);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        request.Status = approve ? AccessRequestStatus.Approved : AccessRequestStatus.Rejected;
        request.DecidedBy = manager.Name;
        request.DecidedAtUtc = now;
        request.DecisionComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        db.AuditEntries.Add(new AuditEntry
        {
            TimestampUtc = now,
            Actor = manager.Name,
            Action = approve ? "request.approved" : "request.rejected",
            Details = $"Request {request.Id} for {request.System} by {request.RequesterName}"
        });
        await db.SaveChangesAsync(ct);
        return new DecisionResult(DecisionOutcome.Ok, request);
    }
}
