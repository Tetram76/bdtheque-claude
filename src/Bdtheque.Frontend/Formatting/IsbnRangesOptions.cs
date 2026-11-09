namespace Bdtheque.Frontend.Formatting;

/// <summary>Where the ranges of the International ISBN Agency are downloaded from (section <c>IsbnRanges</c>).</summary>
public sealed class IsbnRangesOptions
{
    public const string SectionName = "IsbnRanges";

    /// <summary>The range message of the agency, on its own site; none to keep every ISBN as entered (tests, without network).</summary>
    public Uri? Source { get; set; }
}
