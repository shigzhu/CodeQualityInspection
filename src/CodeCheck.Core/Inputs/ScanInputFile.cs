namespace CodeCheck.Core.Inputs;

public sealed class ScanInputFile
{
    public string FullPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool IsHeader { get; set; }
    public bool IsExplicitInput { get; set; }
}
