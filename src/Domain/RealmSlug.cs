using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace WarHack.Domain;

public sealed partial record RealmSlug
{
    private RealmSlug(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static RealmSlug From(string input)
    {
        return TryFrom(input, out var slug) ? slug : throw new ArgumentException($"'{input}' is not a valid realm name.", nameof(input));
    }

    public static bool TryFrom([NotNullWhen(true)] string? input, [NotNullWhen(true)] out RealmSlug? slug)
    {
        slug = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.Trim().ToLowerInvariant().Replace("'", "").Replace(" ", "-");

        if (!SlugPattern().IsMatch(normalized))
        {
            return false;
        }

        slug = new RealmSlug(normalized);
        return true;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
