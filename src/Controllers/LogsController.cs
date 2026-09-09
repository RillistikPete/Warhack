using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarHack.Data;
using WarHack.Logging;
using WarHack.Models;

namespace WarHack.Controllers;

/// <summary>A read-only window on the LogQueue table. Admin only.</summary>
[Route("logs"), Authorize(Policy = "IsAdmin")]
public class LogsController : Controller
{
    private const int PageSize = 50;

    private readonly WarHackDbContext _db;

    public LogsController(WarHackDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? level, CancellationToken ct, int page = 1)
    {
        var filtered = _db.LogQueue.AsQueryable();

        if (!string.IsNullOrEmpty(level))
        {
            filtered = filtered.Where(l => l.Level == level);
        }
        var totalRows = await filtered.CountAsync(ct);
        var lastPage = LogsPageView.PageCount(totalRows, PageSize);

        // so that manually typing ?page=-4 isn't allowed:
        if (page < 1 || page > lastPage) return NotFound();

        // Page 1 equates to 0, so skips nothing. Page 2 -> 2 - 1 = 1, *50 -> skip 50
        var rowsToSkip = (page - 1) * PageSize;

        var rows = await filtered
            // ThenBy on Id bc timestamps are identical
            .OrderByDescending(l => l.DateAdded)
            .ThenByDescending(l => l.Id)
            .Skip(rowsToSkip)
            .Take(PageSize)
            .Select(l => new LogRow(l.Id, l.DateAdded, l.Level, l.Logger, l.Message))
            .ToListAsync(ct);

        var levelCounts = await _db.LogQueue
            .GroupBy(l => l.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var levels = levelCounts
            .OrderByDescending(x => x.Count)
            .Select(x => new LevelCount(x.Level, x.Count))
            .ToList();

        return View(new LogsPageView(rows, levels, level, page, PageSize, totalRows));
    }
}
