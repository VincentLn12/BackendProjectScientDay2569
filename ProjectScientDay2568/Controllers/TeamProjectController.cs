using Microsoft.AspNetCore.RateLimiting;
using ProjectScientDay2568.Helper;
using ProjectScientDay2568.Models.Dtos.Team;

[Route("api/[controller]")]
[ApiController]
public class TeamProjectController : ControllerBase
{
    private readonly TeamProjectService _teamProject;

    public TeamProjectController(TeamProjectService teamProject)
    {
        _teamProject = teamProject;
    }

    [HttpGet("by-team/{teamId:int}")]
    public async Task<IActionResult> GetByTeamId(int teamId)
    {
        var result = await _teamProject.GetByTeamIdAsync(teamId);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _teamProject.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<IActionResult> Create([FromForm] TeamProjectCreateDto dto)
    {
        var result = await _teamProject.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.TeamProjectID }, result);
    }

    [HttpPut("{id:int}")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<IActionResult> Update(int id, [FromForm] TeamProjectUpdateDto dto)
    {
        var result = await _teamProject.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _teamProject.DeleteAsync(id);
        return NoContent();
    }
}