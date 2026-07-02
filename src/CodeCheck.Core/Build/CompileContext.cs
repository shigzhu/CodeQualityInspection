namespace CodeCheck.Core.Build;

public sealed class CompileContext
{
    public List<string> IncludeDirectories { get; set; } = [];
    public List<string> Defines { get; set; } = [];
    public string CStandard { get; set; } = "c11";
    public string CppStandard { get; set; } = "c++14";
    public List<string> AdditionalArguments { get; set; } = [];
    public List<string> ProjectFiles { get; set; } = [];

    public bool HasCompileContext => IncludeDirectories.Count > 0 || ProjectFiles.Count > 0;
}
