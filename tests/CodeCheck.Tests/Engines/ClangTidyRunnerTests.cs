using CodeCheck.Core.Engines;
using CodeCheck.Core.Build;
using CodeCheck.Core.Inputs;

namespace CodeCheck.Tests.Engines;

public sealed class ClangTidyRunnerTests
{
    [Fact]
    public void ParseIssues_CreatesIssuesFromClangTidyDiagnostics()
    {
        var file = new ScanInputFile
        {
            FullPath = Path.Combine(TestRepository.Root, "samples", "cpp-demo", "src", "class_error.cpp"),
            RelativePath = Path.Combine("src", "class_error.cpp"),
            Language = "cpp"
        };
        var output = $"{file.FullPath}:15:5: warning: destructor of 'Base' is public and non-virtual [cppcoreguidelines-virtual-class-destructor]";

        var issues = ClangTidyRunner.ParseIssues(output, [file]);

        var issue = Assert.Single(issues);
        Assert.Equal("Quectel-CPP-005", issue.RuleId);
        Assert.Equal("Warning", issue.Severity);
        Assert.Equal(file.RelativePath, issue.File);
        Assert.Equal(15, issue.Line);
    }

    [Fact]
    public void BuildArguments_UsesCompileContext()
    {
        var file = new ScanInputFile
        {
            FullPath = Path.Combine(TestRepository.Root, "samples", "cpp-demo", "src", "main.cpp"),
            RelativePath = Path.Combine("src", "main.cpp"),
            Language = "cpp"
        };
        var compileContext = new CompileContext
        {
            IncludeDirectories = [Path.Combine(TestRepository.Root, "samples", "cpp-demo", "include")],
            Defines = ["FROM_CONTEXT"],
            CppStandard = "c++14"
        };

        var arguments = ClangTidyRunner.BuildArguments(file, compileContext);

        Assert.Contains("-std=c++14", arguments);
        Assert.Contains(arguments, argument => argument.StartsWith("-I", StringComparison.Ordinal) && argument.Contains("include", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("-DFROM_CONTEXT", arguments);
    }
}
