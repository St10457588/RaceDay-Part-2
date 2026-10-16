using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db) => _db = db;

    /// <summary>Captures a participant's finish time and position. Event owner only.</summary>
    /// <param name="enrolmentId">The enrolment ID.</param>
    /// <param name="request">Finish time and position.</param>
    /// <response code="201">Result captured.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The user is not the event owner.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [Authorize(Roles = nameof(UserRole.Organiser))]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> CreateResult(int enrolmentId, CreateResultDto request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event.OrganiserId != GetUserId()) return Forbid();

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Conflict(new { message = "A result already exists for this enrolment." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            RecordedAt = DateTime.UtcNow
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyResults), new { }, new ResultDto
        {
            ResultId = result.ResultId,
            EnrolmentId = result.EnrolmentId,
            FinishTime = result.FinishTime,
            Position = result.Position,
            EventId = enrolment.EventId,
            EventName = enrolment.Event.Name,
            CategoryName = enrolment.Category.Name
        });
    }

    /// <summary>Returns the authenticated Participant's own results.</summary>
    /// <response code="200">Results returned.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The user is not a Participant.</response>
    [HttpGet("results/my")]
    [Authorize(Roles = nameof(UserRole.Participant))]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultDto>>> GetMyResults()
    {
        var participantId = GetUserId();

        var results = await _db.Results
            .Include(r => r.Enrolment.Event)
            .Include(r => r.Enrolment.Category)
            .Where(r => r.Enrolment.ParticipantId == participantId)
            .Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                FinishTime = r.FinishTime,
                Position = r.Position,
                EventId = r.Enrolment.EventId,
                EventName = r.Enrolment.Event.Name,
                CategoryName = r.Enrolment.Category.Name
            })
            .ToListAsync();

        return Ok(results);
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
