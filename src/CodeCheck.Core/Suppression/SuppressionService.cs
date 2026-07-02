using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCheck.Core.Configuration;
using CodeCheck.Core.Issues;

namespace CodeCheck.Core.Suppression;

public interface ISuppressionService
{
    Task<SuppressionInfo> ApplyAsync(IReadOnlyList<Issue> issues, CodeCheckConfig config, string rootDirectory, CancellationToken cancellationToken);
}

public sealed class SuppressionService : ISuppressionService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<SuppressionInfo> ApplyAsync(IReadOnlyList<Issue> issues, CodeCheckConfig config, string rootDirectory, CancellationToken cancellationToken)
    {
        var suppressionPath = ResolveSuppressionPath(config, rootDirectory);
        var info = new SuppressionInfo
        {
            Enabled = config.Suppression.Enabled,
            SuppressionPath = suppressionPath
        };

        if (!config.Suppression.Enabled)
        {
            foreach (var issue in issues)
            {
                issue.SuppressionState = "Active";
            }

            return info;
        }

        var file = await LoadOrCreateAsync(suppressionPath, config, cancellationToken);
        var activeSuppressions = file.Suppressions
            .Where(suppression => string.Equals(suppression.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var issue in issues)
        {
            var suppression = activeSuppressions.FirstOrDefault(item => IsMatched(item, issue));
            if (suppression is null)
            {
                issue.IsSuppressed = false;
                issue.SuppressionState = "Active";
                continue;
            }

            issue.IsSuppressed = true;
            issue.SuppressionState = "Suppressed";
            info.SuppressedIssues.Add(new SuppressedIssue
            {
                SuppressionId = suppression.SuppressionId,
                Fingerprint = issue.Fingerprint,
                RuleId = issue.RuleId,
                File = issue.File,
                Line = issue.Line,
                Reason = suppression.Reason,
                Status = suppression.Status,
                Scope = suppression.Scope
            });
        }

        info.ActiveSuppressionCount = activeSuppressions.Count;
        info.SuppressedIssueCount = info.SuppressedIssues.Count;
        return info;
    }

    private static bool IsMatched(SuppressionItem suppression, Issue issue)
    {
        if (string.Equals(suppression.Scope, "single-issue", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(suppression.Scope))
        {
            return string.Equals(suppression.Fingerprint, issue.Fingerprint, StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(suppression.Scope, "file-rule", StringComparison.OrdinalIgnoreCase))
        {
            return IsSameRule(suppression, issue) &&
                string.Equals(NormalizePath(suppression.File), NormalizePath(issue.File), StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(suppression.Scope, "path-rule", StringComparison.OrdinalIgnoreCase))
        {
            var suppressionPath = EnsureTrailingSlash(NormalizePath(suppression.File));
            var issuePath = NormalizePath(issue.File);
            return IsSameRule(suppression, issue) && issuePath.StartsWith(suppressionPath, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsSameRule(SuppressionItem suppression, Issue issue)
    {
        return string.Equals(suppression.RuleId, issue.RuleId, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string value)
    {
        return value.Replace('\\', '/').Trim().TrimStart('/');
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith('/') ? value : value + "/";
    }

    private static async Task<SuppressionFile> LoadOrCreateAsync(string suppressionPath, CodeCheckConfig config, CancellationToken cancellationToken)
    {
        if (File.Exists(suppressionPath))
        {
            await using var readStream = File.OpenRead(suppressionPath);
            return await JsonSerializer.DeserializeAsync<SuppressionFile>(readStream, Options, cancellationToken) ?? new SuppressionFile();
        }

        var directory = Path.GetDirectoryName(suppressionPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var now = DateTime.Now;
        var file = new SuppressionFile
        {
            SuppressionId = $"{GetProjectName(config)}-{now:yyyyMMddHHmmss}",
            CreatedAt = now,
            UpdatedAt = now,
            Project = new SuppressionProjectInfo
            {
                Name = GetProjectName(config),
                Root = config.Project.Root,
                ProjectKey = config.Project.ProjectKey
            }
        };

        await using var writeStream = File.Create(suppressionPath);
        await JsonSerializer.SerializeAsync(writeStream, file, Options, cancellationToken);
        return file;
    }

    private static string ResolveSuppressionPath(CodeCheckConfig config, string rootDirectory)
    {
        if (!string.IsNullOrWhiteSpace(config.Suppression.Path))
        {
            return Path.GetFullPath(Path.IsPathRooted(config.Suppression.Path) ? config.Suppression.Path : Path.Combine(rootDirectory, config.Suppression.Path));
        }

        var projectName = SanitizeFileName(GetProjectName(config));
        var projectKey = SanitizeFileName(config.Project.ProjectKey);
        var fileName = string.IsNullOrWhiteSpace(projectKey) ? $"{projectName}.suppressions.json" : $"{projectName}_{projectKey}.suppressions.json";
        return Path.Combine(rootDirectory, "suppressions", fileName);
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

public sealed class SuppressionFile
{
    public string SchemaVersion { get; set; } = "1.0.0";
    public string SuppressionId { get; set; } = string.Empty;
    public SuppressionProjectInfo Project { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SuppressionItem> Suppressions { get; set; } = [];
}

public sealed class SuppressionProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Root { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
}

public sealed class SuppressionItem
{
    public string SuppressionId { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string Scope { get; set; } = "single-issue";
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}

public sealed class SuppressionInfo
{
    public bool Enabled { get; set; }
    public string SuppressionPath { get; set; } = string.Empty;
    public int ActiveSuppressionCount { get; set; }
    public int SuppressedIssueCount { get; set; }
    public List<SuppressedIssue> SuppressedIssues { get; set; } = [];
}

public sealed class SuppressedIssue
{
    public string SuppressionId { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string Scope { get; set; } = "single-issue";
}
