using WarHack.Logging;

namespace WarHack.Models;

// used to replace ViewData keys in view
public sealed record LogsPageView(
    IReadOnlyList<LogRow> Rows,
    IReadOnlyList<LevelCount> Levels,
    string? Level,
    int Page,
    int PageSize,
    int TotalRows)
{
    public int TotalPages => PageCount(TotalRows, PageSize);

    /// <summary>
    /// Pagination logic
    /// </summary>
    public static int PageCount(int totalRows, int pageSize)
    {
        if (totalRows <= pageSize)
        {
            return 1;
        }
        // C# does integer division, so 3 / 50 = 0
        var pages = totalRows / pageSize;
        if (totalRows % pageSize > 0) // if remainder, add one more page
        {
            pages++;
        }
        return pages;
    }
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public int FirstRowOnPage => TotalRows == 0 ? 0 : ((Page - 1) * PageSize) + 1; // 1, 51, 101...
    public int LastRowOnPage => Math.Min(Page * PageSize, TotalRows);
}
