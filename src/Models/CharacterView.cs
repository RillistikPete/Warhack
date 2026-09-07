using WarHack.Blizzard.Contracts;
using WarHack.Domain;

namespace WarHack.Models;

public sealed class CharacterView
{
    public required string Name { get; init; }
    public required string Realm { get; init; }
    public required string RealmSlug { get; init; }
    public int Level { get; init; }
    public string? Race { get; init; }
    public string? Class { get; init; }
    public string? Spec { get; init; }
    public string? Faction { get; init; }
    public string? Guild { get; init; }
    public int AchievementPoints { get; init; }
    public double AverageItemLevel { get; init; }
    public double EquippedItemLevel { get; init; }
    public DateTimeOffset? LastLogin { get; init; }

    public IReadOnlyList<StatLine> Stats { get; init; } = [];
    public IReadOnlyList<GearLine> Gear { get; init; } = [];

    /// <summary>Slots with no permanent enchant, for the "what am I missing" glance.</summary>
    public IReadOnlyList<string> MissingEnchants { get; init; } = [];

    /// <summary>Slots carrying at least one socket with no gem in it.</summary>
    public IReadOnlyList<string> EmptySockets { get; init; } = [];
}

public sealed record StatLine(string Label, string Value);

public sealed record GearLine(
    string Slot,
    string ItemName,
    int ItemLevel,
    string Quality,
    IReadOnlyList<string> Enchants,
    IReadOnlyList<string> Gems,
    int EmptySocketCount);
