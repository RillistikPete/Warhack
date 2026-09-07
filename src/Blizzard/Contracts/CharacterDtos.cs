namespace WarHack.Blizzard.Contracts;

// Partial maps

/// <summary>Named reference used all over the API (race, class, spec, realm...).</summary>
public sealed record NamedRef(int Id, string? Name, string? Slug);

/// <summary>GET /profile/wow/character/{realmSlug}/{characterName}</summary>
public sealed record CharacterSummary(
    long Id,
    string Name,
    int Level,
    NamedRef? Race,
    NamedRef? CharacterClass,
    NamedRef? ActiveSpec,
    NamedRef? Realm,
    NamedRef? Faction,
    GuildRef? Guild,
    int AchievementPoints,
    double AverageItemLevel,
    double EquippedItemLevel,
    long LastLoginTimestamp);

public sealed record GuildRef(long Id, string? Name, NamedRef? Realm);

/// <summary>GET .../statistics</summary>
public sealed record CharacterStatistics(
    long Health,
    long Power,
    NamedRef? PowerType,
    StatValue? Strength,
    StatValue? Agility,
    StatValue? Intellect,
    StatValue? Stamina,
    RatedStat? MeleeCrit,
    RatedStat? MeleeHaste,
    RatedStat? Mastery,
    double Versatility,
    double VersatilityDamageDoneBonus,
    RatedStat? Lifesteal,
    RatedStat? Avoidance);

/// <summary>A primary stat</summary>
public sealed record StatValue(long Base, long Effective);

/// <summary>A secondary stat</summary>
public sealed record RatedStat(long Rating, double RatingBonus, double Value);

/// <summary>GET .../equipment</summary>
public sealed record CharacterEquipment(IReadOnlyList<EquippedItem> EquippedItems);

public sealed record EquippedItem(
    NamedRef? Slot,
    NamedRef? Quality,
    string? Name,
    ItemLevel? Level,
    IReadOnlyList<Enchantment>? Enchantments,
    IReadOnlyList<Socket>? Sockets,
    IReadOnlyList<ItemStat>? Stats);

public sealed record ItemLevel(int Value, string? DisplayString);
public sealed record Enchantment(string? DisplayString, int EnchantmentId);
public sealed record Socket(NamedRef? SocketType, SocketItem? Item, string? DisplayString);
public sealed record SocketItem(int Id, string? Name);
public sealed record ItemStat(NamedRef? Type, long Value, StatDisplay? Display);
public sealed record StatDisplay(string? DisplayString);
