using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Bdtheque.Frontend.Formatting;

/// <summary>
/// The ranges of the International ISBN Agency (its range message, <c>RangeMessage.xml</c>), which place the hyphens of an
/// ISBN: its prefix (978 or 979), its registration group, its registrant and its publication, then its check digit
/// (fonctionnel/presentation/libelle-d-une-edition.md).
/// </summary>
public sealed class IsbnRanges
{
    // The digits that follow an element, compared with the ranges of its rules, written on seven digits.
    private const int RangeDigits = 7;

    private readonly Dictionary<string, Rule[]> _prefixes;
    private readonly Dictionary<string, Rule[]> _groups;

    private IsbnRanges(Dictionary<string, Rule[]> prefixes, Dictionary<string, Rule[]> groups)
    {
        _prefixes = prefixes;
        _groups = groups;
    }

    /// <summary>A range of the digits that follow an element, and the length of the next element; 0 for a range not in use.</summary>
    private readonly record struct Rule(int From, int To, int Length);

    /// <summary>
    /// Reads the range message of the agency, whose document type declaration is ignored. Every rule is checked against the
    /// format the hyphenation relies on: a message whose format changed is refused whole, never read in part.
    /// </summary>
    /// <exception cref="InvalidDataException">Not a range message in the format expected, whatever the difference.</exception>
    public static IsbnRanges Parse(Stream message)
    {
        XElement root;
        try
        {
            using var reader = XmlReader.Create(message, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
            root = XDocument.Load(reader).Root ?? throw new InvalidDataException("An empty range message.");
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException($"Not an XML range message: {exception.Message}", exception);
        }

        var prefixes = Elements(root.Element("EAN.UCCPrefixes"), "EAN.UCC");
        var groups = Elements(root.Element("RegistrationGroups"), "Group");
        if (!prefixes.ContainsKey("978") || !prefixes.ContainsKey("979") || groups.Count == 0)
            throw new InvalidDataException("A range message without the prefixes 978 and 979, or without registration groups.");
        return new IsbnRanges(prefixes, groups);
    }

    /// <summary>
    /// The ISBN with its hyphens, an ISBN-10 as its ISBN-13 with the prefix 978, without it; <c>null</c> for what is not an
    /// ISBN, or an ISBN whose group or registrant no range places. The check digit is not checked: its place is the same.
    /// </summary>
    public string? Hyphenate(string isbn)
    {
        var digits = string.Concat(isbn.Where(c => c is not ('-' or ' '))).ToUpperInvariant();
        var isbn10 = digits.Length == 10;
        if (isbn10 ? !digits[..9].All(char.IsAsciiDigit) || !(char.IsAsciiDigit(digits[9]) || digits[9] == 'X')
            : digits.Length != 13 || !digits.All(char.IsAsciiDigit))
            return null;

        var full = isbn10 ? "978" + digits : digits;
        var prefix = full[..3];
        if (!_prefixes.TryGetValue(prefix, out var prefixRules) || LengthAfter(full, 3, prefixRules) is not (> 0 and var groupLength))
            return null;
        var group = full.Substring(3, groupLength);
        if (!_groups.TryGetValue($"{prefix}-{group}", out var groupRules)
            || LengthAfter(full, 3 + groupLength, groupRules) is not (> 0 and var registrantLength)
            || 3 + groupLength + registrantLength >= 12)
            return null;

        var registrant = full.Substring(3 + groupLength, registrantLength);
        var publication = full[(3 + groupLength + registrantLength)..12];
        var hyphenated = $"{group}-{registrant}-{publication}-{full[12]}";
        return isbn10 ? hyphenated : $"{prefix}-{hyphenated}";
    }

    // The length the rules give the next element, from the digits after the start (before the check digit), padded to the
    // width of the ranges; 0 when no rule covers them.
    private static int LengthAfter(string full, int start, Rule[] rules)
    {
        var following = full[start..12].PadRight(RangeDigits, '0')[..RangeDigits];
        var value = int.Parse(following, CultureInfo.InvariantCulture);
        return rules.FirstOrDefault(rule => rule.From <= value && value <= rule.To).Length;
    }

    private static Dictionary<string, Rule[]> Elements(XElement? container, string name)
    {
        var elements = new Dictionary<string, Rule[]>();
        foreach (var element in container?.Elements(name) ?? [])
        {
            var prefix = (string?)element.Element("Prefix") ?? throw new InvalidDataException($"An element {name} without prefix.");
            var rules = element.Element("Rules")?.Elements("Rule").Select(rule => ToRule(prefix, rule)).ToArray() ?? [];
            if (rules.Length == 0 || !elements.TryAdd(prefix, rules))
                throw new InvalidDataException($"The element {name} {prefix} has no rule, or is given twice.");
        }

        return elements;
    }

    // A range of two bounds of seven digits, in order, and a length that the seven digits hold.
    private static Rule ToRule(string prefix, XElement rule)
    {
        var range = ((string?)rule.Element("Range"))?.Split('-');
        var length = (string?)rule.Element("Length");
        if (range is not [{ Length: RangeDigits } from, { Length: RangeDigits } to]
            || !int.TryParse(from, NumberStyles.None, CultureInfo.InvariantCulture, out var first)
            || !int.TryParse(to, NumberStyles.None, CultureInfo.InvariantCulture, out var last)
            || first > last
            || !int.TryParse(length, NumberStyles.None, CultureInfo.InvariantCulture, out var digits)
            || digits > RangeDigits)
            throw new InvalidDataException($"A rule of {prefix} that is not a range of two numbers of {RangeDigits} digits with a length: {rule}.");
        return new Rule(first, last, digits);
    }
}
