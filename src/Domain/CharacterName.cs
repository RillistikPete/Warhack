using System.Diagnostics.CodeAnalysis;

namespace WarHack.Domain;

/// <summary>
/// A WoW character name. Blizzard's profile endpoints want it lowercased and
/// URL-encoded; <see cref="Display"/> keeps the form worth showing a person.
/// </summary>
public sealed record CharacterName
{
    /// <summary>Blizzard's cap, and a cheap guard against junk input.</summary>
    private const int MaxLength = 12;

    private CharacterName(string display, string value)
    {
        Display = display;
        Value = value;
    }

    /// <summary>Trimmed, title-cased for display.</summary>
    public string Display { get; }

    /// <summary>Lowercased, for building request URIs.</summary>
    public string Value { get; }

    /// <summary>Percent-encoded, safe to concatenate into a path.</summary>
    public string UriSegment => Uri.EscapeDataString(Value);

    public static CharacterName From(string input) =>
        TryFrom(input, out var name)
            ? name
            : throw new ArgumentException($"'{input}' is not a usable character name.", nameof(input));

    public static bool TryFrom([NotNullWhen(true)] string? input, [NotNullWhen(true)] out CharacterName? name)
    {
        name = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var trimmed = input.Trim();
        if (trimmed.Length > MaxLength) return false;

        // Letters only -- covers accented realms; rejects the injection-shaped input.
        if (!trimmed.All(char.IsLetter)) return false;

        var lower = trimmed.ToLowerInvariant();
        var display = char.ToUpperInvariant(lower[0]) + lower[1..];

        name = new CharacterName(display, lower);
        return true;
    }

    public override string ToString() => Display;
}
