using Bdtheque.Frontend.Security;
using Microsoft.Extensions.Options;

namespace Bdtheque.Frontend.Tests.Security;

/// <summary>
/// The second factor of the administrator account on an exposed deployment
/// (contraintes-techniques/architecture-deploiement/authentification.md): a time-based one-time code (RFC 6238).
/// </summary>
public sealed class AdminSecondFactorTests
{
    // The secret of the test vectors of RFC 6238 (appendix B) for HMAC-SHA1, "12345678901234567890" in ASCII, in Base32.
    private const string RfcSecret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

    // The six last digits of the eight-digit codes of the RFC: a code of six digits is the same value, modulo 10^6.
    [Theory]
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    public void Code_IsThatOfTheRfcTestVectors(long unixSeconds, string code)
    {
        var factor = Factor(RfcSecret, new StepClock(DateTimeOffset.FromUnixTimeSeconds(unixSeconds)));

        Assert.True(factor.IsValid(code));
    }

    [Fact]
    public void Code_OfThePreviousOrNextStep_IsAccepted_ForTheClockOfThePhone()
    {
        // 287082 is the code of the step [30, 60[ of the RFC.
        Assert.True(Factor(RfcSecret, new StepClock(DateTimeOffset.FromUnixTimeSeconds(89))).IsValid("287082"));
        Assert.True(Factor(RfcSecret, new StepClock(DateTimeOffset.FromUnixTimeSeconds(1))).IsValid("287082"));
    }

    [Fact]
    public void Code_TwoStepsAway_IsRefused()
    {
        Assert.False(Factor(RfcSecret, new StepClock(DateTimeOffset.FromUnixTimeSeconds(119))).IsValid("287082"));
    }

    [Fact]
    public void Code_AlreadyAccepted_IsRefused()
    {
        var clock = new StepClock(DateTimeOffset.FromUnixTimeSeconds(59));
        var factor = Factor(RfcSecret, clock);

        Assert.True(factor.IsValid("287082"));
        Assert.False(factor.IsValid("287082"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("28708")]
    [InlineData("2870820")]
    [InlineData("28708a")]
    [InlineData("287083")]
    public void Code_ThatIsNotTheRightOne_IsRefused(string? code)
    {
        Assert.False(Factor(RfcSecret, new StepClock(DateTimeOffset.FromUnixTimeSeconds(59))).IsValid(code));
    }

    [Fact]
    public void Code_TypedWithSpaces_AsAuthenticatorsShowIt_IsAccepted()
    {
        Assert.True(Factor(RfcSecret, new StepClock(DateTimeOffset.FromUnixTimeSeconds(59))).IsValid(" 287 082 "));
    }

    [Theory]
    [InlineData("gezdgnbvgy3tqojqgezdgnbvgy3tqojq")]
    [InlineData("GEZD GNBV GY3T QOJQ GEZD GNBV GY3T QOJQ")]
    [InlineData("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ====")]
    public void Secret_IsReadAsAuthenticatorsShowIt(string secret)
    {
        Assert.True(Factor(secret, new StepClock(DateTimeOffset.FromUnixTimeSeconds(59))).IsValid("287082"));
    }

    [Fact]
    public void WithoutSecret_NoSecondFactorIsAsked()
    {
        Assert.False(Factor(null, TimeProvider.System).IsRequired);
        Assert.True(Factor(RfcSecret, TimeProvider.System).IsRequired);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, "http://bdtheque.lan:8080", true)]
    [InlineData(null, "https://bdtheque.example.org", false)]
    [InlineData(RfcSecret, "https://bdtheque.example.org", true)]
    [InlineData(RfcSecret, null, true)]
    [InlineData("GEZDGNBVGY3TQOJQ", null, false)]
    [InlineData("GEZDGNBVGY3TQOJ1GEZDGNBVGY3TQOJQ", null, false)]
    [InlineData("   ", "https://bdtheque.example.org", false)]
    public void Settings_AreValid_OnlyWithASecretOfAtLeast160Bits_RequiredOnAnHttpsPublicAddress(
        string? secret, string? publicAddress, bool valid)
    {
        var account = new AdminAccountOptions { Login = "admin", Password = "Tr0ub4dor&3", TotpSecret = secret };
        var address = new Bdtheque.Frontend.Hosting.PublicAddressOptions { Url = publicAddress };

        Assert.Equal(valid, AdminSecondFactor.AreSettingsValid(account, address));
    }

    private static AdminSecondFactor Factor(string? secret, TimeProvider clock) =>
        new(Options.Create(new AdminAccountOptions { Login = "admin", Password = "Tr0ub4dor&3", TotpSecret = secret }), clock);

    private sealed class StepClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
