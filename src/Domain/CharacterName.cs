using System.Diagnostics.CodeAnalysis;

namespace WarHack.Domain;

/// <summary>
/// A WoW character name. Blizzard's profile endpoints want it lowercased and
/// URL-encoded; <see cref="Display"/> keeps the form worth showing a person.
/// </summary>
public sealed record CharacterName
{
    /// <summary>Blizzard's cap</summary>
    private const int MaxLength = 12;

    private CharacterName(string display, string value)
    {
        Display = display;
        Value = value;
    }

    public string Display { get; }

    /// <summary>
    /// Validation object
    /// </summary>
    public string Value { get; }

    public string UriSegment => Uri.EscapeDataString(Value);


    /// <summary>
    /// From a string
    /// </summary>
    /// <param name="input"></param>
    /// <returns>CharacterName</returns>
    /// <if>validation passes</if>
    public static CharacterName From(string input)
    {
        return TryFrom(input, out var name) ? name : throw new ArgumentException($"'{input}' is not a usable character name.", nameof(input));
    }

    public static bool TryFrom([NotNullWhen(true)] string? input, [NotNullWhen(true)] out CharacterName? name)
    {
        name = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var trimmed = input.Trim();
        if (trimmed.Length > MaxLength)
        {
            return false;
        }
        if (!trimmed.All(char.IsLetter))
        {
            return false;
        }

        var lower = trimmed.ToLowerInvariant();
        var display = char.ToUpperInvariant(lower[0]) + lower[1..];

        name = new CharacterName(display, lower);
        return true;
    }

    public override string ToString() => Display;
}
