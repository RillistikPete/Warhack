using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace WarHack.Domain;

/// <summary>
/// A Blizzard realm slug: lowercase, hyphen-separated, no apostrophes.
/// Construction is the only place normalization happens, so nothing downstream
/// needs to remember to call ToLower() or strip punctuation.
/// </summary>
public sealed partial record RealmSlug
{
    private RealmSlug(string value) => Value = value;

    public string Value { get; }

    public static RealmSlug From(string input) =>
        TryFrom(input, out var slug)
            ? slug
            : throw new ArgumentException($"'{input}' is not a usable realm name.", nameof(input));

    public static bool TryFrom([NotNullWhen(true)] string? input, [NotNullWhen(true)] out RealmSlug? slug)
    {
        slug = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var normalized = input.Trim().ToLowerInvariant()
            .Replace("'", "")
            .Replace(" ", "-");

        // Blizzard slugs are ASCII letters, digits and hyphens.
        if (!SlugPattern().IsMatch(normalized)) return false;

        slug = new RealmSlug(normalized);
        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(RealmSlug slug) => slug.Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
