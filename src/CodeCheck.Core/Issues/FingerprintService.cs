using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeCheck.Core.Issues;

public interface IFingerprintService
{
    void ApplyFingerprints(IReadOnlyList<Issue> issues);
}

public sealed class FingerprintService : IFingerprintService
{
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public void ApplyFingerprints(IReadOnlyList<Issue> issues)
    {
        foreach (var issue in issues)
        {
            issue.Fingerprint = CreateStableFingerprint(issue);
            issue.PrimaryFingerprint = CreatePrimaryFingerprint(issue);
        }
    }

    public static string CreateStableFingerprint(Issue issue)
    {
        return "sha256-stable-" + Hash(string.Join('|',
            Normalize(issue.RuleId),
            NormalizePath(issue.File),
            Normalize(issue.Message)));
    }

    public static string CreatePrimaryFingerprint(Issue issue)
    {
        return "sha256-primary-" + Hash(string.Join('|',
            Normalize(issue.RuleId),
            NormalizePath(issue.File),
            issue.Line.ToString(),
            Normalize(issue.Message)));
    }

    private static string Normalize(string value)
    {
        return WhitespacePattern.Replace(value.Trim().ToLowerInvariant(), " ");
    }

    private static string NormalizePath(string value)
    {
        return Normalize(value.Replace('\\', '/'));
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }
}
