using System.Text.Json;
using CodeCheck.Core.Configuration;
using CodeCheck.Core.Issues;
using CodeCheck.Core.Suppression;

namespace CodeCheck.Tests.Suppression;

public sealed class SuppressionServiceTests
{
    [Fact]
    public async Task ApplyAsync_CreatesEmptySuppressionFileWhenMissing()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-suppression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var suppressionPath = Path.Combine(directory, "demo.suppressions.json");
        var issue = CreateIssue("fp-1");

        try
        {
            var info = await new SuppressionService().ApplyAsync([issue], CreateConfig(suppressionPath), directory, CancellationToken.None);

            Assert.True(File.Exists(suppressionPath));
            Assert.True(info.Enabled);
            Assert.Equal("Active", issue.SuppressionState);
            Assert.False(issue.IsSuppressed);
            Assert.Empty(info.SuppressedIssues);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyAsync_SuppressesIssueByFingerprint()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-suppression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var suppressionPath = Path.Combine(directory, "demo.suppressions.json");
        await File.WriteAllTextAsync(suppressionPath,
            """
            {
              "schemaVersion": "1.0.0",
              "suppressionId": "sup-file",
              "suppressions": [
                {
                  "suppressionId": "SUP-000001",
                  "fingerprint": "fp-1",
                  "scope": "single-issue",
                  "reason": "false positive",
                  "status": "Active"
                }
              ]
            }
            """);
        var issue = CreateIssue("fp-1");

        try
        {
            var info = await new SuppressionService().ApplyAsync([issue], CreateConfig(suppressionPath), directory, CancellationToken.None);

            Assert.True(issue.IsSuppressed);
            Assert.Equal("Suppressed", issue.SuppressionState);
            Assert.Equal(1, info.ActiveSuppressionCount);
            var suppressedIssue = Assert.Single(info.SuppressedIssues);
            Assert.Equal("SUP-000001", suppressedIssue.SuppressionId);
            Assert.Equal("false positive", suppressedIssue.Reason);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyAsync_SuppressesIssueByFileRule()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-suppression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var suppressionPath = Path.Combine(directory, "demo.suppressions.json");
        await File.WriteAllTextAsync(suppressionPath,
            """
            {
              "schemaVersion": "1.0.0",
              "suppressionId": "sup-file",
              "suppressions": [
                {
                  "suppressionId": "SUP-000002",
                  "scope": "file-rule",
                  "ruleId": "Quectel-CPP-001",
                  "file": "include/bad_header.hpp",
                  "reason": "legacy header",
                  "status": "Active"
                }
              ]
            }
            """);
        var matched = CreateIssue("fp-1");
        var unmatched = CreateIssue("fp-2");
        unmatched.RuleId = "Quectel-CPP-002";

        try
        {
            var info = await new SuppressionService().ApplyAsync([matched, unmatched], CreateConfig(suppressionPath), directory, CancellationToken.None);

            Assert.True(matched.IsSuppressed);
            Assert.False(unmatched.IsSuppressed);
            var suppressedIssue = Assert.Single(info.SuppressedIssues);
            Assert.Equal("file-rule", suppressedIssue.Scope);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyAsync_SuppressesIssueByPathRule()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-suppression-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var suppressionPath = Path.Combine(directory, "demo.suppressions.json");
        await File.WriteAllTextAsync(suppressionPath,
            """
            {
              "schemaVersion": "1.0.0",
              "suppressionId": "sup-file",
              "suppressions": [
                {
                  "suppressionId": "SUP-000003",
                  "scope": "path-rule",
                  "ruleId": "Quectel-CPP-001",
                  "file": "include",
                  "reason": "legacy include directory",
                  "status": "Active"
                }
              ]
            }
            """);
        var matched = CreateIssue("fp-1");
        var unmatched = CreateIssue("fp-2");
        unmatched.File = "src/main.cpp";

        try
        {
            var info = await new SuppressionService().ApplyAsync([matched, unmatched], CreateConfig(suppressionPath), directory, CancellationToken.None);

            Assert.True(matched.IsSuppressed);
            Assert.False(unmatched.IsSuppressed);
            var suppressedIssue = Assert.Single(info.SuppressedIssues);
            Assert.Equal("path-rule", suppressedIssue.Scope);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static CodeCheckConfig CreateConfig(string suppressionPath)
    {
        return new CodeCheckConfig
        {
            Project = new ProjectConfig { Name = "Demo", ProjectKey = "DemoKey" },
            Suppression = new SuppressionConfig { Enabled = true, Path = suppressionPath }
        };
    }

    private static Issue CreateIssue(string fingerprint)
    {
        return new Issue
        {
            IssueId = fingerprint,
            Fingerprint = fingerprint,
            PrimaryFingerprint = fingerprint + "-primary",
            RuleId = "Quectel-CPP-001",
            Severity = "Warning",
            File = "include/bad_header.hpp",
            Line = 6,
            Message = "Header should not use using namespace."
        };
    }
}
