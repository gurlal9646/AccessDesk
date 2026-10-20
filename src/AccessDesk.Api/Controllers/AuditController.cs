using AccessDesk.Api.Contracts;
using AccessDesk.Api.Data;
using AccessDesk.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessDesk.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.CanViewAudit)]
[Route("api/audit")]
public class AuditController(AccessDeskDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditEntryDto>>> List([FromQuery] int take = 100, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var items = await db.AuditEntries.AsNoTracking()
            .OrderByDescending(a => a.TimestampUtc).ThenByDescending(a => a.Id)
            .Take(take)
            .Select(a => new AuditEntryDto(a.Id, a.TimestampUtc, a.Actor, a.Action, a.Details))
            .ToListAsync(ct);
        return Ok(items);
    }
}
