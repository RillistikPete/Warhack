using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WarHack.Blizzard;
using WarHack.Blizzard.Contracts;
using WarHack.Models;
using WarHack.Domain;
using WarHack.Blizzard.Interfaces;

namespace WarHack.Application;

public sealed class Athenaeum : IAthenaeum
{
    private static readonly string[] EnchantableSlots =
        ["BACK", "CHEST", "WRIST", "LEGS", "FEET", "FINGER_1", "FINGER_2", "MAIN_HAND", "OFF_HAND"];

    private static readonly SemaphoreSlim[] KeyLocks = [.. Enumerable.Range(0, 32).Select(_ => new SemaphoreSlim(1, 1))];

    private readonly IBlizzardClient _blizzard;
    private readonly IMemoryCache _cache;
    private readonly BlizzardOptions _options;
    private readonly ILogger<Athenaeum> _log;

    public Athenaeum(IBlizzardClient blizzard, IMemoryCache cache, IOptions<BlizzardOptions> options, ILogger<Athenaeum> log)
    {
        _blizzard = blizzard;
        _cache = cache;
        _options = options.Value;
        _log = log;
    }

    public async Task<ApiResult<CharacterView>> GetCharacterAsync(RealmSlug realm, CharacterName name, CancellationToken ct = default)
    {
        var cacheKey = $"character:{realm.Value}:{name.Value}";

        if (_cache.TryGetValue(cacheKey, out CharacterView? cached) && cached is not null)
        {
            _log.LogDebug("Cache hit for {CacheKey}.", cacheKey);
            return ApiResult<CharacterView>.Ok(cached);
        }

        var gate = KeyLocks[(uint)cacheKey.GetHashCode() % KeyLocks.Length];
        await gate.WaitAsync(ct);
        try
        {
            // Someone may have populated it while we queued for the lock.
            if (_cache.TryGetValue(cacheKey, out cached) && cached is not null)
            {
                return ApiResult<CharacterView>.Ok(cached);
            }

            var result = await FetchAsync(realm, name, ct);

            if (result.IsSuccess && result.Value is not null)
            {
                _cache.Set(cacheKey, result.Value, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.CharacterCacheDuration,
                    Size = 1
                });
            }

            return result;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<ApiResult<CharacterView>> FetchAsync(RealmSlug realm, CharacterName name, CancellationToken ct)
    {
        var root = $"profile/wow/character/{realm.Value}/{name.UriSegment}";

        var summaryTask = _blizzard.GetAsync<CharacterSummary>(root, ct: ct);
        var statsTask = _blizzard.GetAsync<CharacterStatistics>($"{root}/statistics", ct: ct);
        var gearTask = _blizzard.GetAsync<CharacterEquipment>($"{root}/equipment", ct: ct);

        await Task.WhenAll(summaryTask, statsTask, gearTask);

        var summary = summaryTask.Result;
        if (!summary.IsSuccess || summary.Value is null)
        {
            _log.LogInformation("Character lookup failed for {Realm}/{Name}: {Error}", realm.Value, name.Value, summary.Error);
            return ApiResult<CharacterView>.Fail(summary.Status == 404 ? $"No character named \"{name.Display}\" found on {realm.Value}." : summary.Error ?? "Character lookup failed.", summary.Status);
        }

        var stats = statsTask.Result.IsSuccess ? statsTask.Result.Value : null;
        var gear = gearTask.Result.IsSuccess ? gearTask.Result.Value : null;

        return ApiResult<CharacterView>.Ok(Compose(summary.Value, stats, gear, realm));
    }

    private static CharacterView Compose(CharacterSummary s, CharacterStatistics? st, CharacterEquipment? eq, RealmSlug realm)
    {
        var items = eq?.EquippedItems ?? [];

        var gear = items
            .Select(i => new GearLine(
                Slot: i.Slot?.Name ?? i.Slot?.Slug ?? "Unknown",
                ItemName: i.Name ?? "(unnamed)",
                ItemLevel: i.Level?.Value ?? 0,
                Quality: i.Quality?.Name ?? "",
                Enchants: (i.Enchantments ?? [])
                    .Select(e => e.DisplayString)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Select(d => d!)
                    .ToList(),
                Gems: (i.Sockets ?? [])
                    .Where(x => x.Item is not null)
                    .Select(x => x.Item!.Name ?? x.DisplayString)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Select(d => d!)
                    .ToList(),
                EmptySocketCount: (i.Sockets ?? []).Count(x => x.Item is null)))
            .OrderByDescending(g => g.ItemLevel)
            .ToList();

        var missingEnchants = EnchantableSlots
            .Select(slotType => items.FirstOrDefault(i =>
                string.Equals(i.Slot?.Slug, slotType, StringComparison.OrdinalIgnoreCase)))
            .Where(i => i is not null && (i.Enchantments is null || i.Enchantments.Count == 0))
            .Select(i => i!.Slot?.Name ?? "Unknown")
            .ToList();

        var emptySockets = gear
            .Where(g => g.EmptySocketCount > 0)
            .Select(g => g.EmptySocketCount > 1 ? $"{g.Slot} ({g.EmptySocketCount})" : g.Slot)
            .ToList();

        return new CharacterView
        {
            Name = s.Name,
            Realm = s.Realm?.Name ?? realm.Value,
            RealmSlug = s.Realm?.Slug ?? realm.Value,
            Level = s.Level,
            Race = s.Race?.Name,
            Class = s.CharacterClass?.Name,
            Spec = s.ActiveSpec?.Name,
            Faction = s.Faction?.Name,
            Guild = s.Guild?.Name,
            AchievementPoints = s.AchievementPoints,
            AverageItemLevel = s.AverageItemLevel,
            EquippedItemLevel = s.EquippedItemLevel,
            LastLogin = s.LastLoginTimestamp > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(s.LastLoginTimestamp) : null,
            Stats = BuildStats(st),
            Gear = gear,
            MissingEnchants = missingEnchants,
            EmptySockets = emptySockets
        };
    }

    private static List<StatLine> BuildStats(CharacterStatistics? st)
    {
        if (st is null)
        {
            return [];
        }

        var lines = new List<StatLine>
        {
            new("Health", st.Health.ToString("N0", CultureInfo.InvariantCulture)),
            new(st.PowerType?.Name ?? "Power", st.Power.ToString("N0", CultureInfo.InvariantCulture))
        };

        AddPrimary(lines, "Strength", st.Strength);
        AddPrimary(lines, "Agility", st.Agility);
        AddPrimary(lines, "Intellect", st.Intellect);
        AddPrimary(lines, "Stamina", st.Stamina);
        AddRated(lines, "Crit", st.CriticalStrike);
        AddRated(lines, "Haste", st.Haste);
        AddRated(lines, "Mastery", st.Mastery);

        // Versatility is percentage 0-100
        if (st.Versatility > 0)
        {
            lines.Add(new StatLine("Versatility",
                $"{st.VersatilityDamageDoneBonus:0.##}%  ({st.Versatility:N0})"));
        }

        AddRated(lines, "Leech", st.Lifesteal);
        AddRated(lines, "Avoidance", st.Avoidance);

        return lines;

        // Local functions
        // Two blizzard stat shapes:
        static void AddPrimary(List<StatLine> into, string label, StatValue? v)
        {
            if (v is not null && v.Effective > 0)
                into.Add(new StatLine(label, v.Effective.ToString("N0", CultureInfo.InvariantCulture)));
        }

        static void AddRated(List<StatLine> into, string label, RatedStat? v)
        {
            if (v is not null && v.Rating > 0)
            {
                into.Add(new StatLine(label, $"{v.Value:0.##}%  ({v.Rating:N0})"));
            }
        }
    }
}
