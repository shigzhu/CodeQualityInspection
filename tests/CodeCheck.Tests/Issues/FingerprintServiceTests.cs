using CodeCheck.Core.Issues;

namespace CodeCheck.Tests.Issues;

public sealed class FingerprintServiceTests
{
    [Fact]
    public void ApplyFingerprints_CreatesStableAndPrimaryFingerprints()
    {
        var issue = new Issue
        {
            RuleId = "Quectel-CPP-001",
            File = @"include\bad_header.hpp",
            Line = 6,
            Message = "Header should not use using namespace."
        };

        new FingerprintService().ApplyFingerprints([issue]);

        Assert.StartsWith("sha256-stable-", issue.Fingerprint);
        Assert.StartsWith("sha256-primary-", issue.PrimaryFingerprint);
        Assert.NotEqual(issue.Fingerprint, issue.PrimaryFingerprint);
    }

    [Fact]
    public void StableFingerprint_IgnoresLineNumberButPrimaryFingerprintUsesIt()
    {
        var first = new Issue
        {
            RuleId = "Quectel-CPP-001",
            File = "include/bad_header.hpp",
            Line = 6,
            Message = "Header should not use using namespace."
        };
        var second = new Issue
        {
            RuleId = "Quectel-CPP-001",
            File = @"include\bad_header.hpp",
            Line = 10,
            Message = "Header  should   not use using namespace."
        };

        new FingerprintService().ApplyFingerprints([first, second]);

        Assert.Equal(first.Fingerprint, second.Fingerprint);
        Assert.NotEqual(first.PrimaryFingerprint, second.PrimaryFingerprint);
    }
}
