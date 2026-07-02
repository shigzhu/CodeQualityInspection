using CodeCheck.Core.Configuration;
using CodeCheck.Core.Build;
using CodeCheck.Core.Inputs;

namespace CodeCheck.Core.Engines;

public interface IAnalysisEngine
{
    string Name { get; }

    Task<EngineResult> AnalyzeAsync(
        CodeCheckConfig config,
        CompileContext compileContext,
        IReadOnlyList<ScanInputFile> files,
        CancellationToken cancellationToken);
}
