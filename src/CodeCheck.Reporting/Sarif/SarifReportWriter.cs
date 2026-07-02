using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCheck.Core.Reports;

namespace CodeCheck.Reporting.Sarif;

public sealed class SarifReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task WriteAsync(ScanReport report, string outputPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sarif = new
        {
            version = "2.1.0",
            schema = "https://json.schemastore.org/sarif-2.1.0.json",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = report.Tool.Name,
                            version = report.Tool.CliVersion,
                            informationUri = "https://github.com/",
                            rules = report.Issues
                                .Where(issue => !string.IsNullOrWhiteSpace(issue.RuleId))
                                .GroupBy(issue => issue.RuleId, StringComparer.OrdinalIgnoreCase)
                                .Select(group => new
                                {
                                    id = group.Key,
                                    name = group.Key,
                                    shortDescription = new { text = group.First().Message },
                                    properties = new
                                    {
                                        severity = group.First().Severity,
                                        engine = group.First().Engine,
                                        language = group.First().Language
                                    }
                                })
                                .ToArray()
                        }
                    },
                    results = report.Issues.Select(issue => new
                    {
                        ruleId = issue.RuleId,
                        level = ToSarifLevel(issue.Severity),
                        message = new { text = issue.Message },
                        fingerprints = new
                        {
                            stable = issue.Fingerprint,
                            primary = issue.PrimaryFingerprint
                        },
                        properties = new
                        {
                            issueId = issue.IssueId,
                            severity = issue.Severity,
                            engine = issue.Engine,
                            language = issue.Language,
                            baselineState = issue.BaselineState,
                            suppressionState = issue.SuppressionState,
                            isSuppressed = issue.IsSuppressed
                        },
                        locations = new[]
                        {
                            new
                            {
                                physicalLocation = new
                                {
                                    artifactLocation = new { uri = issue.File.Replace('\\', '/') },
                                    region = new { startLine = issue.Line <= 0 ? 1 : issue.Line }
                                }
                            }
                        }
                    }).ToArray()
                }
            }
        };

        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, sarif, Options, cancellationToken);
    }

    private static string ToSarifLevel(string severity)
    {
        return severity switch
        {
            "Blocker" or "Critical" => "error",
            "Warning" => "warning",
            _ => "note"
        };
    }
}
