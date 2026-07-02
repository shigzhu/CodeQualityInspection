using CodeCheck.Core.Baseline;
using CodeCheck.Core.Configuration;
using CodeCheck.Core.Issues;

namespace CodeCheck.Tests.Baseline;

public sealed class BaselineServiceTests
{
    [Fact]
    public async Task ApplyAsync_CreatesBaselineWhenMissing()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-baseline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var baselinePath = Path.Combine(directory, "demo.baseline.json");
        var config = CreateConfig(baselinePath);
        var issue = CreateIssue("fp-1");

        try
        {
            var info = await new BaselineService().ApplyAsync([issue], config, directory, CancellationToken.None);

            Assert.True(File.Exists(baselinePath));
            Assert.True(info.CreatedAutomatically);
            Assert.False(info.ExistsBeforeScan);
            Assert.Equal("Created", info.State);
            Assert.Equal("NotCompared", issue.BaselineState);
            Assert.Equal(1, info.Summary.NotComparedIssues);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyAsync_ComparesExistingBaseline()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-baseline-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var baselinePath = Path.Combine(directory, "demo.baseline.json");
        var config = CreateConfig(baselinePath);
        var service = new BaselineService();

        try
        {
            await service.ApplyAsync([CreateIssue("existing"), CreateIssue("fixed")], config, directory, CancellationToken.None);
            var existing = CreateIssue("existing");
            var @new = CreateIssue("new");

            var info = await service.ApplyAsync([existing, @new], config, directory, CancellationToken.None);

            Assert.True(info.ExistsBeforeScan);
            Assert.Equal("Compared", info.State);
            Assert.Equal("Existing", existing.BaselineState);
            Assert.Equal("New", @new.BaselineState);
            Assert.Equal(1, info.Summary.ExistingIssues);
            Assert.Equal(1, info.Summary.NewIssues);
            Assert.Equal(1, info.Summary.FixedIssues);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static CodeCheckConfig CreateConfig(string baselinePath)
    {
        return new CodeCheckConfig
        {
            Project = new ProjectConfig { Name = "Demo", ProjectKey = "DemoKey" },
            Baseline = new BaselineConfig { Enabled = true, Path = baselinePath, CreateIfMissing = true }
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
