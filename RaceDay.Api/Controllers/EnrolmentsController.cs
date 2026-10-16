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
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    /// <summary>Enrols the authenticated Participant in an event category.</summary>
    /// <param name="request">The event and category to enter.</param>
    /// <response code="201">Enrolment created.</response>
    /// <response code="400">The category does not belong to the event.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The user is not a Participant.</response>
    /// <response code="404">Event or category not found.</response>
    /// <response code="409">The participant is already enrolled in this event.</response>
    [HttpPost("enrolments")]
    [Authorize(Roles = nameof(UserRole.Participant))]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(CreateEnrolmentDto request)
    {
        var participantId = GetUserId();

        var ev = await _db.Events.Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == request.EventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        var category = ev.Categories.FirstOrDefault(c => c.CategoryId == request.CategoryId);
        if (category is null)
            return BadRequest(new { message = "The selected category does not belong to this event." });

        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == participantId && e.EventId == request.EventId))
            return Conflict(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            EnrolledAt = DateTime.UtcNow
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMyEnrolments), new { }, await MapEnrolment(enrolment.EnrolmentId));
    }

    /// <summary>Returns the authenticated Participant's enrolments.</summary>
    /// <response code="200">Enrolments returned.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The user is not a Participant.</response>
    [HttpGet("enrolments/my")]
    [Authorize(Roles = nameof(UserRole.Participant))]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetMyEnrolments()
    {
        var participantId = GetUserId();

        var enrolments = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant.FullName,
                EventId = e.EventId,
                EventName = e.Event.Name,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                EnrolledAt = e.EnrolledAt
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Returns all enrolments for an event. Event owner only.</summary>
    /// <param name="eventId">The event ID.</param>
    /// <response code="200">Enrolments returned.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The user is not the event owner.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [Authorize(Roles = nameof(UserRole.Organiser))]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentDto>>> GetEventEnrolments(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != GetUserId()) return Forbid();

        var enrolments = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.Category.Name).ThenBy(e => e.Participant.FullName)
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant.FullName,
                EventId = e.EventId,
                EventName = e.Event.Name,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                EnrolledAt = e.EnrolledAt
            })
            .ToListAsync();

        return Ok(enrolments);
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<EnrolmentDto> MapEnrolment(int enrolmentId)
    {
        var e = await _db.Enrolments
            .Include(x => x.Event)
            .Include(x => x.Category)
            .Include(x => x.Participant)
            .FirstAsync(x => x.EnrolmentId == enrolmentId);

        return new EnrolmentDto
        {
            EnrolmentId = e.EnrolmentId,
            ParticipantId = e.ParticipantId,
            ParticipantName = e.Participant.FullName,
            EventId = e.EventId,
            EventName = e.Event.Name,
            CategoryId = e.CategoryId,
            CategoryName = e.Category.Name,
            EnrolledAt = e.EnrolledAt
        };
    }
}
