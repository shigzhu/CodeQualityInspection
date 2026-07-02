using CodeCheck.Core.Build;
using CodeCheck.Core.Configuration;

namespace CodeCheck.Tests.Build;

public sealed class CompileContextBuilderTests
{
    [Fact]
    public void Build_CreatesCompileContextFromConfig()
    {
        var config = new CodeCheckConfig
        {
            Build = new BuildConfig
            {
                IncludeDirectories = ["samples/cpp-demo/include"],
                Defines = ["UNIT_TEST"],
                ProjectFiles = ["samples/cpp-demo/cpp-demo.vcxproj"]
            }
        };

        var context = new CompileContextBuilder().Build(config, [], TestRepository.Root);

        Assert.True(context.HasCompileContext);
        Assert.Contains(context.IncludeDirectories, path => path.EndsWith(Path.Combine("samples", "cpp-demo", "include")));
        Assert.Contains("UNIT_TEST", context.Defines);
        Assert.Contains(context.ProjectFiles, path => path.EndsWith(Path.Combine("samples", "cpp-demo", "cpp-demo.vcxproj")));
    }

    [Fact]
    public async Task Build_MergesProProjectIncludePathAndDefines()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-context-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var projectFile = Path.Combine(directory, "demo.pro");
        await File.WriteAllLinesAsync(projectFile,
        [
            "INCLUDEPATH += $$PWD/include",
            "DEFINES += FROM_PRO"
        ]);

        try
        {
            var config = new CodeCheckConfig
            {
                Build = new BuildConfig
                {
                    ProjectFiles = [projectFile]
                }
            };

            var context = new CompileContextBuilder().Build(config, [], TestRepository.Root);

            Assert.True(context.HasCompileContext);
            Assert.Contains(context.IncludeDirectories, path => path.EndsWith(Path.Combine("include")));
            Assert.Contains("FROM_PRO", context.Defines);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Build_MergesVcxprojProjectIncludePathAndDefines()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-vcx-context-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var projectFile = Path.Combine(directory, "demo.vcxproj");
        await File.WriteAllTextAsync(projectFile,
            """
            <Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemDefinitionGroup>
                <ClCompile>
                  <AdditionalIncludeDirectories>$(ProjectDir)include;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
                  <PreprocessorDefinitions>FROM_VCXPROJ;%(PreprocessorDefinitions)</PreprocessorDefinitions>
                </ClCompile>
              </ItemDefinitionGroup>
            </Project>
            """);

        try
        {
            var config = new CodeCheckConfig
            {
                Build = new BuildConfig
                {
                    ProjectFiles = [projectFile]
                }
            };

            var context = new CompileContextBuilder().Build(config, [], TestRepository.Root);

            Assert.True(context.HasCompileContext);
            Assert.Contains(context.IncludeDirectories, path => path.EndsWith(Path.Combine("include")));
            Assert.Contains("FROM_VCXPROJ", context.Defines);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Build_AutoDiscoversProjectFilesFromInputDirectories()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-discover-context-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        await File.WriteAllLinesAsync(Path.Combine(directory, "demo.pro"),
        [
            "INCLUDEPATH += $$PWD/pro_include",
            "DEFINES += FROM_DISCOVERED_PRO"
        ]);
        await File.WriteAllTextAsync(Path.Combine(directory, "demo.vcxproj"),
            """
            <Project DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
              <ItemDefinitionGroup>
                <ClCompile>
                  <AdditionalIncludeDirectories>$(ProjectDir)vcx_include;%(AdditionalIncludeDirectories)</AdditionalIncludeDirectories>
                  <PreprocessorDefinitions>FROM_DISCOVERED_VCXPROJ;%(PreprocessorDefinitions)</PreprocessorDefinitions>
                </ClCompile>
              </ItemDefinitionGroup>
            </Project>
            """);

        try
        {
            var config = new CodeCheckConfig
            {
                Input = new InputConfig
                {
                    Paths = [directory]
                }
            };

            var context = new CompileContextBuilder().Build(config, [], TestRepository.Root);

            Assert.True(context.HasCompileContext);
            Assert.Contains(context.IncludeDirectories, path => path.EndsWith(Path.Combine("pro_include")));
            Assert.Contains(context.IncludeDirectories, path => path.EndsWith(Path.Combine("vcx_include")));
            Assert.Contains("FROM_DISCOVERED_PRO", context.Defines);
            Assert.Contains("FROM_DISCOVERED_VCXPROJ", context.Defines);
            Assert.Contains(context.ProjectFiles, path => path.EndsWith("demo.pro"));
            Assert.Contains(context.ProjectFiles, path => path.EndsWith("demo.vcxproj"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
