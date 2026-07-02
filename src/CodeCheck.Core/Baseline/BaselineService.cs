using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCheck.Core.Configuration;
using CodeCheck.Core.Issues;

namespace CodeCheck.Core.Baseline;

public interface IBaselineService
{
    Task<BaselineInfo> ApplyAsync(IReadOnlyList<Issue> issues, CodeCheckConfig config, string rootDirectory, CancellationToken cancellationToken);
}

public sealed class BaselineService : IBaselineService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<BaselineInfo> ApplyAsync(IReadOnlyList<Issue> issues, CodeCheckConfig config, string rootDirectory, CancellationToken cancellationToken)
    {
        var baselinePath = ResolveBaselinePath(config, rootDirectory);
        var info = new BaselineInfo
        {
            Enabled = config.Baseline.Enabled,
            Mode = config.Baseline.Mode,
            BaselinePath = baselinePath
        };

        if (!config.Baseline.Enabled)
        {
            info.State = "Disabled";
            return info;
        }

        info.ExistsBeforeScan = File.Exists(baselinePath);
        if (!info.ExistsBeforeScan)
        {
            foreach (var issue in issues)
            {
                issue.BaselineState = "NotCompared";
            }

            info.Summary.NotComparedIssues = issues.Count(issue => !issue.IsSuppressed);
            if (config.Baseline.CreateIfMissing)
            {
                await WriteBaselineAsync(baselinePath, config, issues.Where(issue => !issue.IsSuppressed).ToList(), cancellationToken);
                info.CreatedAutomatically = true;
                info.State = "Created";
            }
            else
            {
                info.State = "NotFound";
            }

            return info;
        }

        var baseline = await ReadBaselineAsync(baselinePath, cancellationToken);
        var currentFingerprints = issues.Where(issue => !issue.IsSuppressed).Select(issue => issue.Fingerprint).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var baselineFingerprints = baseline.Issues.Select(issue => issue.Fingerprint).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var issue in issues)
        {
            if (issue.IsSuppressed)
            {
                continue;
            }

            if (baselineFingerprints.Contains(issue.Fingerprint))
            {
                issue.BaselineState = "Existing";
                info.Summary.ExistingIssues++;
            }
            else
            {
                issue.BaselineState = "New";
                info.Summary.NewIssues++;
            }
        }

        info.Summary.FixedIssues = baselineFingerprints.Count(fingerprint => !currentFingerprints.Contains(fingerprint));
        info.State = "Compared";
        return info;
    }

    private static async Task<BaselineFile> ReadBaselineAsync(string baselinePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(baselinePath);
        return await JsonSerializer.DeserializeAsync<BaselineFile>(stream, Options, cancellationToken) ?? new BaselineFile();
    }

    private static async Task WriteBaselineAsync(string baselinePath, CodeCheckConfig config, IReadOnlyList<Issue> issues, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(baselinePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var now = DateTime.Now;
        var baseline = new BaselineFile
        {
            BaselineId = $"{GetProjectName(config)}-{now:yyyyMMddHHmmss}",
            CreatedAt = now,
            UpdatedAt = now,
            Project = new BaselineProjectInfo
            {
                Name = GetProjectName(config),
                Root = config.Project.Root,
                ProjectKey = config.Project.ProjectKey
            },
            Summary = new BaselineFileSummary { TotalIssues = issues.Count },
            Issues = issues.Select(issue => new BaselineIssue
            {
                Fingerprint = issue.Fingerprint,
                PrimaryFingerprint = issue.PrimaryFingerprint,
                RuleId = issue.RuleId,
                File = issue.File,
                Line = issue.Line,
                Message = issue.Message,
                Severity = issue.Severity
            }).ToList()
        };

        await using var stream = File.Create(baselinePath);
        await JsonSerializer.SerializeAsync(stream, baseline, Options, cancellationToken);
    }

    private static string ResolveBaselinePath(CodeCheckConfig config, string rootDirectory)
    {
        if (!string.IsNullOrWhiteSpace(config.Baseline.Path))
        {
            return Path.GetFullPath(Path.IsPathRooted(config.Baseline.Path) ? config.Baseline.Path : Path.Combine(rootDirectory, config.Baseline.Path));
        }

        var projectName = SanitizeFileName(GetProjectName(config));
        var projectKey = SanitizeFileName(config.Project.ProjectKey);
        var fileName = string.IsNullOrWhiteSpace(projectKey) ? $"{projectName}.baseline.json" : $"{projectName}_{projectKey}.baseline.json";
        return Path.Combine(rootDirectory, "baseline", fileName);
    }

    private static string GetProjectName(CodeCheckConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.Project.Name))
        {
            return config.Project.Name;
        }

        var firstPath = config.Input.Paths.FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstPath) ? "CodeCheckProject" : Path.GetFileNameWithoutExtension(firstPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "CodeCheckProject" : sanitized;
    }
}

public sealed class BaselineFile
{
    public string SchemaVersion { get; set; } = "1.0.0";
    public string BaselineId { get; set; } = string.Empty;
    public BaselineProjectInfo Project { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Mode { get; set; } = "snapshot";
    public BaselineFileSummary Summary { get; set; } = new();
    public List<BaselineIssue> Issues { get; set; } = [];
}

public sealed class BaselineProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Root { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
}

public sealed class BaselineFileSummary
{
    public int TotalIssues { get; set; }
}

public sealed class BaselineIssue
{
    public string Fingerprint { get; set; } = string.Empty;
    public string PrimaryFingerprint { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class BaselineInfo
{
    public bool Enabled { get; set; }
    public string Mode { get; set; } = "compare";
    public string BaselinePath { get; set; } = string.Empty;
    public bool ExistsBeforeScan { get; set; }
    public bool CreatedAutomatically { get; set; }
    public string State { get; set; } = string.Empty;
    public BaselineCompareSummary Summary { get; set; } = new();
}

public sealed class BaselineCompareSummary
{
    public int NewIssues { get; set; }
    public int ExistingIssues { get; set; }
    public int FixedIssues { get; set; }
    public int NotComparedIssues { get; set; }
}
