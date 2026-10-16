using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db) => _db = db;

    /// <summary>Returns all RaceDay events.</summary>
    /// <response code="200">Events returned.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventDto>>> GetEvents()
    {
        var events = await _db.Events
            .Include(e => e.Organiser)
            .OrderBy(e => e.EventDate)
            .Select(e => ToDto(e))
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Returns one event, including its categories.</summary>
    /// <param name="id">The event ID.</param>
    /// <response code="200">Event returned.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> GetEvent(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev is null) return NotFound(new { message = "Event not found." });

        var dto = ToDto(ev);
        return Ok(new EventDetailDto
        {
            EventId = dto.EventId,
            Name = dto.Name,
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location,
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType,
            OrganiserId = dto.OrganiserId,
            OrganiserName = dto.OrganiserName,
            Categories = ev.Categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                Name = c.Name,
                MinAge = c.MinAge,
                MaxAge = c.MaxAge,
                DistanceKm = c.DistanceKm
            }).ToList()
        });
    }

    /// <summary>Creates a new event. Organisers only.</summary>
    /// <param name="request">Event details.</param>
    /// <response code="201">Event created.</response>
    /// <response code="400">The request is invalid.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The user is not an Organiser.</response>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Organiser))]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> CreateEvent(CreateEventDto request)
    {
        var organiserId = GetUserId();

        var ev = new Event
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType,
            OrganiserId = organiserId
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        var organiser = await _db.Users.FindAsync(organiserId);
        var dto = ToDto(ev);
        dto.OrganiserName = organiser?.FullName ?? string.Empty;

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, dto);
    }

    /// <summary>Updates an event owned by the authenticated Organiser.</summary>
    /// <param name="id">The event ID.</param>
    /// <param name="request">Updated event details.</param>
    /// <response code="200">Event updated.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The user is not the event owner.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Organiser))]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(int id, UpdateEventDto request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != GetUserId()) return Forbid();

        ev.Name = request.Name.Trim();
        ev.Description = request.Description.Trim();
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType;

        await _db.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    /// <summary>Deletes an event owned by the authenticated Organiser.</summary>
    /// <param name="id">The event ID.</param>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">The user is not authenticated.</response>
    /// <response code="403">The user is not the event owner.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Organiser))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != GetUserId()) return Forbid();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static EventDto ToDto(Event e) => new()
    {
        EventId = e.EventId,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType.ToString(),
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.FullName ?? string.Empty
    };
}
