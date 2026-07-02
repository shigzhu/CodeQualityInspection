using CodeCheck.Core.Build;

namespace CodeCheck.Tests.Build;

public sealed class VcxprojProjectParserTests
{
    [Fact]
    public async Task Parse_ReadsAdditionalIncludeDirectoriesAndPreprocessorDefinitions()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-vcxproj-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var projectFile = Path.Combine(directory, "demo.vcxproj");
        await File.WriteAllTextAsync(projectFile,
            """
            <Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemDefinitionGroup>
                <ClCompile>
                  <AdditionalIncludeDirectories>$(ProjectDir)include;$(ProjectDir)generated;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
                  <PreprocessorDefinitions>WIN32;QT_DEMO;%(PreprocessorDefinitions)</PreprocessorDefinitions>
                </ClCompile>
              </ItemDefinitionGroup>
            </Project>
            """);

        try
        {
            var info = new VcxprojProjectParser().Parse(projectFile, directory);

            Assert.Contains(info.IncludeDirectories, path => path.EndsWith(Path.Combine("include")));
            Assert.Contains(info.IncludeDirectories, path => path.EndsWith(Path.Combine("generated")));
            Assert.Contains("WIN32", info.Defines);
            Assert.Contains("QT_DEMO", info.Defines);
            Assert.DoesNotContain(info.Defines, define => define.Contains("PreprocessorDefinitions", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
