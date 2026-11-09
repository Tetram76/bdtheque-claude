using System.Text;
using Bdtheque.Frontend.Formatting;

namespace Bdtheque.Frontend.Tests.Formatting;

/// <summary>
/// The hyphens of an ISBN, placed by the ranges of the International ISBN Agency (fonctionnel/presentation/libelle-d-une-edition.md):
/// its prefix, its registration group, its registrant and its publication, then its check digit.
/// </summary>
public sealed class IsbnRangesTests
{
    private readonly IsbnRanges _ranges = Ranges();

    [Theory]
    [InlineData("9782723445671", "978-2-7234-4567-1")]
    [InlineData("978-2-7234-4567-1", "978-2-7234-4567-1")]
    [InlineData("978 272 344 5671", "978-2-7234-4567-1")]
    [InlineData("9782205012345", "978-2-205-01234-5")]
    [InlineData("9782870001234", "978-2-87000-123-4")]
    [InlineData("9791091234501", "979-10-9123-450-1")]
    // An ISBN-10 is split as its ISBN-13 with the prefix 978, without it.
    [InlineData("2723445678", "2-7234-4567-8")]
    [InlineData("272344567x", "2-7234-4567-X")]
    public void Hyphenate_PlacesTheHyphensByTheRanges(string isbn, string expected) =>
        Assert.Equal(expected, _ranges.Hyphenate(isbn));

    [Theory]
    // A registration group the ranges do not know.
    [InlineData("9780306406157")]
    // A range not defined for use (length 0), for the group or for the registrant.
    [InlineData("9798000000001")]
    [InlineData("9782990000001")]
    // Not an ISBN.
    [InlineData("97827234456")]
    [InlineData("97827234X5671")]
    [InlineData("9772723445671")]
    [InlineData("")]
    public void Hyphenate_WithoutRangeForIt_IsNone(string isbn) => Assert.Null(_ranges.Hyphenate(isbn));

    public static TheoryData<string, string> Unrecognized => new()
    {
        { "Not XML", "not a range message" },
        { "No prefix 979", Without("<EAN.UCC><Prefix>979</Prefix>", "<EAN.UCC><Prefix>970</Prefix>") },
        { "No registration group", Without("<RegistrationGroups>", "<Groups>").Replace("</RegistrationGroups>", "</Groups>") },
        { "Range written otherwise", Without("<Range>0000000-1999999</Range>", "<Range>0000000 to 1999999</Range>") },
        { "Range of six digits", Without("<Range>2000000-6999999</Range>", "<Range>200000-699999</Range>") },
        { "Range upside down", Without("<Range>7000000-8499999</Range>", "<Range>8499999-7000000</Range>") },
        { "Length that is no number", Without("<Length>4</Length>", "<Length>four</Length>") },
        { "Length beyond the digits", Without("<Length>5</Length>", "<Length>8</Length>") },
        { "Rule without length", Without("<Length>3</Length>", "") },
    };

    // A message whose format changed is refused whole, by a single error the application reports, rather than read in part
    // or failing on its first unexpected value.
    [Theory]
    [MemberData(nameof(Unrecognized))]
    public void Parse_OfAMessageWhoseFormatIsNotTheOneExpected_IsAFormatError(string change, string message)
    {
        var error = Assert.Throws<InvalidDataException>(() => IsbnRanges.Parse(new MemoryStream(Encoding.UTF8.GetBytes(message))));
        Assert.False(string.IsNullOrWhiteSpace(error.Message), change);
    }

    // A case built from the range message of the tests: a part it does not hold is a case wrongly written, which would test
    // the message unchanged; the data of the theory, built at the discovery of the tests, refuse it.
    private static string Without(string part, string replacement) =>
        RangeMessage.Contains(part, StringComparison.Ordinal)
            ? RangeMessage.Replace(part, replacement, StringComparison.Ordinal)
            : throw new InvalidOperationException($"The range message of the tests holds no « {part} ».");

    [Fact]
    public void Formatter_WithoutRanges_GivesTheIsbnAsEntered_WithThem_Hyphenated()
    {
        var formatter = new IsbnFormatter();
        Assert.Equal("9782723445671", formatter.Format("9782723445671"));

        formatter.Ranges = _ranges;

        Assert.Equal("978-2-7234-4567-1", formatter.Format("9782723445671"));
        Assert.Equal("978-0-306-40615-7", formatter.Format("978-0-306-40615-7"));
    }

    /// <summary>
    /// Ranges in the format of the range message of the agency, written for these tests: its own ranges are not to be
    /// reproduced (terms of the International ISBN Agency), and the tests only need a few of their cases.
    /// </summary>
    internal static IsbnRanges Ranges() => IsbnRanges.Parse(new MemoryStream(Encoding.UTF8.GetBytes(RangeMessage)));

    internal const string RangeMessage = """
        <?xml version='1.0' encoding='utf-8'?>
        <!DOCTYPE ISBNRangeMessage [
        <!ELEMENT ISBNRangeMessage (MessageSource?, MessageDate, EAN.UCCPrefixes, RegistrationGroups) >
        ]>
        <ISBNRangeMessage>
          <MessageSource>Tests</MessageSource>
          <MessageDate>Mon, 9 Nov 2026 12:00:00 UTC</MessageDate>
          <EAN.UCCPrefixes>
            <EAN.UCC><Prefix>978</Prefix><Agency>Tests</Agency><Rules>
              <Rule><Range>0000000-5999999</Range><Length>1</Length></Rule>
              <Rule><Range>6000000-9999999</Range><Length>0</Length></Rule>
            </Rules></EAN.UCC>
            <EAN.UCC><Prefix>979</Prefix><Agency>Tests</Agency><Rules>
              <Rule><Range>0000000-0999999</Range><Length>0</Length></Rule>
              <Rule><Range>1000000-1999999</Range><Length>2</Length></Rule>
              <Rule><Range>2000000-9999999</Range><Length>0</Length></Rule>
            </Rules></EAN.UCC>
          </EAN.UCCPrefixes>
          <RegistrationGroups>
            <Group><Prefix>978-2</Prefix><Agency>Tests</Agency><Rules>
              <Rule><Range>0000000-1999999</Range><Length>2</Length></Rule>
              <Rule><Range>2000000-6999999</Range><Length>3</Length></Rule>
              <Rule><Range>7000000-8499999</Range><Length>4</Length></Rule>
              <Rule><Range>8500000-9899999</Range><Length>5</Length></Rule>
              <Rule><Range>9900000-9999999</Range><Length>0</Length></Rule>
            </Rules></Group>
            <Group><Prefix>979-10</Prefix><Agency>Tests</Agency><Rules>
              <Rule><Range>0000000-1999999</Range><Length>2</Length></Rule>
              <Rule><Range>2000000-6999999</Range><Length>3</Length></Rule>
              <Rule><Range>7000000-9999999</Range><Length>4</Length></Rule>
            </Rules></Group>
          </RegistrationGroups>
        </ISBNRangeMessage>
        """;
}
