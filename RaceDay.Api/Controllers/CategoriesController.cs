using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lists the categories (e.g. 5km, 10km, 21km) for an event. Public.</summary>
    [HttpGet("api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound();

        var categories = await _context.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new CategoryResponse(c.CategoryId, c.EventId, c.CategoryName, c.DistanceKm, c.MaxParticipants, c.Price))
            .ToListAsync();

        return Ok(categories);
    }

    /// <summary>Adds a category to an event. Only the owning organiser may add one.</summary>
    [HttpPost("api/events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateCategory(int eventId, CreateCategoryRequest request)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName,
            DistanceKm = request.DistanceKm,
            MaxParticipants = request.MaxParticipants,
            Price = request.Price
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
            new CategoryResponse(category.CategoryId, category.EventId, category.CategoryName, category.DistanceKm, category.MaxParticipants, category.Price));
    }

    /// <summary>Updates a category. Only the owning organiser may update it.</summary>
    [HttpPut("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryRequest request)
    {
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != CurrentUserId) return Forbid();

        category.CategoryName = request.CategoryName;
        category.DistanceKm = request.DistanceKm;
        category.MaxParticipants = request.MaxParticipants;
        category.Price = request.Price;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Deletes a category. Only the owning organiser may delete it.</summary>
    [HttpDelete("api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != CurrentUserId) return Forbid();

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
