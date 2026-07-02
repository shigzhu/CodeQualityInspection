using CodeCheck.Core.Engines;
using CodeCheck.Core.Build;
using CodeCheck.Core.Inputs;
using CodeCheck.Core.Rules;

namespace CodeCheck.Tests.Engines;

public sealed class CppcheckRunnerTests
{
    [Fact]
    public async Task ParseIssues_CreatesIssuesFromCppcheckXml()
    {
        var file = new ScanInputFile
        {
            FullPath = Path.Combine(TestRepository.Root, "samples", "c-demo", "src", "memory_error.c"),
            RelativePath = Path.Combine("src", "memory_error.c"),
            Language = "c"
        };
        var xml = $"""
                  <?xml version="1.0" encoding="UTF-8"?>
                  <results version="2">
                    <cppcheck version="2.13"/>
                    <errors>
                      <error id="memleak" severity="error" msg="Memory leak: p">
                        <location file="{file.FullPath}" line="12"/>
                      </error>
                    </errors>
                  </results>
                  """;

        var issues = CppcheckRunner.ParseIssues(xml, [file], await CreateMappingResolverAsync());

        var issue = Assert.Single(issues);
        Assert.Equal("Quectel-C-008", issue.RuleId);
        Assert.Equal("memleak", issue.EngineRuleId);
        Assert.Equal("Critical", issue.Severity);
        Assert.Equal(file.RelativePath, issue.File);
        Assert.Equal(12, issue.Line);
    }

    private static async Task<RuleMappingResolver> CreateMappingResolverAsync()
    {
        var ruleSet = await new RuleLoader().LoadAsync(Path.Combine(TestRepository.Root, "rules", "rules.index.json"), CancellationToken.None);
        return new RuleMappingResolver(ruleSet.Mappings);
    }

    [Fact]
    public void BuildArguments_UsesCompileContext()
    {
        var file = new ScanInputFile
        {
            FullPath = Path.Combine(TestRepository.Root, "samples", "c-demo", "src", "main.c"),
            RelativePath = Path.Combine("src", "main.c"),
            Language = "c"
        };
        var compileContext = new CompileContext
        {
            IncludeDirectories = [Path.Combine(TestRepository.Root, "samples", "c-demo", "include")],
            Defines = ["FROM_CONTEXT"]
        };

        var arguments = CppcheckRunner.BuildArguments([file], compileContext);

        Assert.Contains(arguments, argument => argument.StartsWith("-I", StringComparison.Ordinal) && argument.Contains("include", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("-DFROM_CONTEXT", arguments);
        Assert.Contains(file.FullPath, arguments);
    }
}
