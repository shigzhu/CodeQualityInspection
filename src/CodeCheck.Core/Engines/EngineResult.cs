using CodeCheck.Core.Issues;
using CodeCheck.Core.Reports;

namespace CodeCheck.Core.Engines;

public sealed class EngineResult
{
    public string EngineName { get; set; } = string.Empty;
    public List<Issue> Issues { get; set; } = [];
    public List<FailedFile> FailedFiles { get; set; } = [];
    public List<string> Logs { get; set; } = [];
}
