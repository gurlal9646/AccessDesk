using AccessDesk.Api.Contracts;
using AccessDesk.Api.Domain;
using AccessDesk.Api.Security;
using AccessDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/requests")]
public class AccessRequestsController(IAccessRequestService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AccessRequestDto>>> List([FromQuery] AccessRequestStatus? status, CancellationToken ct)
    {
        var user = CurrentUser.From(User);
        var items = await service.ListAsync(user, status, ct);
        return Ok(items.Select(AccessRequestDto.From));
    }

    [HttpPost]
    public async Task<ActionResult<AccessRequestDto>> Create([FromBody] CreateAccessRequestDto dto, CancellationToken ct)
    {
        var user = CurrentUser.From(User);
        var created = await service.CreateAsync(user, dto.System, dto.Justification, ct);
        return Created($"/api/requests/{created.Id}", AccessRequestDto.From(created));
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = Policies.CanDecide)]
    public Task<ActionResult<AccessRequestDto>> Approve(int id, [FromBody] DecisionDto? dto, CancellationToken ct)
        => Decide(id, approve: true, dto?.Comment, ct);

    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = Policies.CanDecide)]
    public Task<ActionResult<AccessRequestDto>> Reject(int id, [FromBody] DecisionDto? dto, CancellationToken ct)
        => Decide(id, approve: false, dto?.Comment, ct);

    private async Task<ActionResult<AccessRequestDto>> Decide(int id, bool approve, string? comment, CancellationToken ct)
    {
        var user = CurrentUser.From(User);
        var result = await service.DecideAsync(user, id, approve, comment, ct);
        return result.Outcome switch
        {
            DecisionOutcome.Ok => Ok(AccessRequestDto.From(result.Request!)),
            DecisionOutcome.NotFound => NotFound(),
            DecisionOutcome.AlreadyDecided => Conflict(new { error = "This request has already been decided." }),
            DecisionOutcome.CannotDecideOwnRequest => StatusCode(StatusCodes.Status403Forbidden,
                new { error = "You cannot decide your own request." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
