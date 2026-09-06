using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarHack.Data;
using WarHack.Logging;

namespace WarHack.Controllers;

/// <summary>A read-only window on the LogQueue table. Admin only.</summary>
[Route("logs"), Authorize(Policy = "IsAdmin")]
public class LogsController : Controller
{
    private const int PageSize = 100;

    private readonly WarHackDbContext _db;

    public LogsController(WarHackDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? level, CancellationToken ct)
    {
        ViewData["Level"] = level;

        // A projection, not an entity query: EF emits SELECT over four columns and
        // materializes LogRow directly. Because LogRow isn't an entity there is
        // nothing for the change tracker to hold, so AsNoTracking() is redundant --
        // and there's no Include() to reach for either.
        var rows = await _db.LogQueue
            .Where(l => level == null || l.Level == level)
            .OrderByDescending(l => l.DateAdded)
            .Take(PageSize)
            .Select(l => new LogRow(l.Id, l.DateAdded, l.Level, l.Logger, l.Message))
            .ToListAsync(ct);

        // Counts by level, also projected -- one GROUP BY, no rows loaded.
        ViewData["Levels"] = await _db.LogQueue
            .GroupBy(l => l.Level)
            .Select(g => new LevelCount(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        return View(rows);
    }
}
