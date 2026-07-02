param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "release",
    [switch]$SelfContained,
    [switch]$SkipTools
)

$ErrorActionPreference = "Stop"

function Copy-DirectoryIfExists($Source, $Destination) {
    if (Test-Path $Source) {
        New-Item -ItemType Directory -Force -Path $Destination | Out-Null
        Copy-Item -Path (Join-Path $Source "*") -Destination $Destination -Recurse -Force
    }
}

if (Test-Path $Output) {
    Get-ChildItem $Output -Exclude reports,baseline,suppressions,logs | Remove-Item -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null
New-Item -ItemType Directory -Force -Path "$Output/reports", "$Output/baseline", "$Output/suppressions", "$Output/logs", "$Output/temp", "$Output/tools", "$Output/licenses" | Out-Null

$selfContainedArg = if ($SelfContained) { "true" } else { "false" }

dotnet publish "src/CodeCheck.Cli/CodeCheck.Cli.csproj" -c $Configuration -r $Runtime --self-contained $selfContainedArg -o $Output
dotnet publish "src/CodeCheck.Desktop/CodeCheck.Desktop.csproj" -c $Configuration -r $Runtime --self-contained $selfContainedArg -o $Output

Copy-DirectoryIfExists "configs" "$Output/configs"
Copy-DirectoryIfExists "rules" "$Output/rules"
Copy-DirectoryIfExists "report-templates" "$Output/report-templates"
Copy-DirectoryIfExists "docs" "$Output/docs"
Copy-DirectoryIfExists "samples" "$Output/samples"
Copy-DirectoryIfExists "licenses" "$Output/licenses"

if (-not $SkipTools) {
    Copy-DirectoryIfExists "third_party_tools/llvm" "$Output/tools/llvm"
    Copy-DirectoryIfExists "third_party_tools/cppcheck" "$Output/tools/cppcheck"
    Copy-DirectoryIfExists "third_party_tools/lizard" "$Output/tools/lizard"
}

Write-Host "release 目录已生成：$Output"
