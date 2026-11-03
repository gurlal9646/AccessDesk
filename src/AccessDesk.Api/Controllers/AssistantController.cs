using AccessDesk.Api.Contracts;
using AccessDesk.Api.Services.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/assistant")]
public class AssistantController(IPolicyAssistant assistant) : ControllerBase
{
    [HttpPost("ask")]
    public async Task<ActionResult<AskPolicyResponseDto>> Ask([FromBody] AskPolicyDto dto, CancellationToken ct)
    {
        try
        {
            var result = await assistant.AskAsync(dto.Question, ct);
            return Ok(new AskPolicyResponseDto(
                result.Answer,
                result.Sources.Select(s => new PolicySourceDto(s.Document, s.Section)).ToList()));
        }
        catch (AssistantUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }
}
