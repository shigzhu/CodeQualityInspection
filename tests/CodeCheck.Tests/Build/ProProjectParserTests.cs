using CodeCheck.Core.Build;

namespace CodeCheck.Tests.Build;

public sealed class ProProjectParserTests
{
    [Fact]
    public async Task Parse_ReadsIncludePathAndDefines()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-pro-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var projectFile = Path.Combine(directory, "demo.pro");
        await File.WriteAllLinesAsync(projectFile,
        [
            "QT += core gui widgets",
            "INCLUDEPATH += $$PWD/include \\",
            "               $$PWD/generated",
            "DEFINES += QT_DEMO USE_WIDGETS # comment"
        ]);

        try
        {
            var info = new ProProjectParser().Parse(projectFile, directory);

            Assert.Contains(info.IncludeDirectories, path => path.EndsWith(Path.Combine("include")));
            Assert.Contains(info.IncludeDirectories, path => path.EndsWith(Path.Combine("generated")));
            Assert.Contains("QT_DEMO", info.Defines);
            Assert.Contains("USE_WIDGETS", info.Defines);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
