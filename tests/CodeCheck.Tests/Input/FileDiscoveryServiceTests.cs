using CodeCheck.Core.Configuration;
using CodeCheck.Core.Inputs;

namespace CodeCheck.Tests.Input;

public sealed class FileDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverAsync_DirectoryMode_DoesNotScheduleHeadersByDefault()
    {
        var config = CreateConfig("directory", [Path.Combine(TestRepository.Root, "samples", "c-demo")]);

        var files = await new FileDiscoveryService().DiscoverAsync(config, CancellationToken.None);

        Assert.Contains(files, file => file.RelativePath.EndsWith(Path.Combine("src", "main.c"), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(files, file => file.IsHeader);
    }

    [Fact]
    public async Task DiscoverAsync_ExplicitHeader_SchedulesHeader()
    {
        var headerPath = Path.Combine(TestRepository.Root, "samples", "c-demo", "include", "c_demo.h");
        var config = CreateConfig("file", [headerPath]);

        var files = await new FileDiscoveryService().DiscoverAsync(config, CancellationToken.None);

        var file = Assert.Single(files);
        Assert.True(file.IsHeader);
        Assert.True(file.IsExplicitInput);
    }

    private static CodeCheckConfig CreateConfig(string inputType, List<string> paths)
    {
        return new CodeCheckConfig
        {
            Project = new ProjectConfig
            {
                Name = "test-project",
                Root = Path.Combine(TestRepository.Root, "samples", "c-demo")
            },
            Input = new InputConfig
            {
                Type = inputType,
                Paths = paths,
                SourceExtensions = [".c", ".cc", ".cpp", ".cxx"],
                HeaderExtensions = [".h", ".hh", ".hpp", ".hxx"],
                HeaderScanPolicy = new HeaderScanPolicyConfig
                {
                    ScanHeadersInDirectoryMode = false,
                    AllowHeaderAsExplicitInput = true,
                    HeaderLanguageMode = "auto"
                }
            },
            Scan = new ScanConfig
            {
                ExcludeDirectories = ["third_party", "build", "out", "bin", "obj", "Debug", "Release", ".git", ".svn", ".vs"],
                ExcludeFiles = ["moc_*.cpp", "ui_*.h", "qrc_*.cpp", "mocs_compilation.cpp"]
            }
        };
    }
}
