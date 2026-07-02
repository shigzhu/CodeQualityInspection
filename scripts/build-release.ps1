param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Output = "release",
    [switch]$FrameworkDependent,
    [switch]$SkipTools,
    [switch]$NoPublish
)

$ErrorActionPreference = "Stop"

function Copy-DirectoryIfExists($Source, $Destination) {
    if (Test-Path $Source) {
        New-Item -ItemType Directory -Force -Path $Destination | Out-Null
        Copy-Item -Path (Join-Path $Source "*") -Destination $Destination -Recurse -Force
    }
}

function Assert-RequiredFile($Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required bundled tool file is missing: $Path. Place tools under third_party_tools or use -SkipTools for development-only release skeletons."
    }
}

if (Test-Path $Output) {
    Get-ChildItem $Output -Exclude reports,baseline,suppressions,logs | Remove-Item -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $Output | Out-Null
New-Item -ItemType Directory -Force -Path "$Output/reports", "$Output/baseline", "$Output/suppressions", "$Output/logs", "$Output/temp", "$Output/tools", "$Output/licenses" | Out-Null
Copy-Item -Path "third_party_tools/third-party-versions.json" -Destination "$Output/tools/third-party-versions.json" -Force

$selfContainedArg = if ($FrameworkDependent) { "false" } else { "true" }

if (-not $NoPublish) {
    dotnet publish "src/CodeCheck.Cli/CodeCheck.Cli.csproj" -c $Configuration -r $Runtime --self-contained $selfContainedArg -o $Output
    dotnet publish "src/CodeCheck.Desktop/CodeCheck.Desktop.csproj" -c $Configuration -r $Runtime --self-contained $selfContainedArg -o $Output
}

Copy-DirectoryIfExists "configs" "$Output/configs"
Copy-DirectoryIfExists "rules" "$Output/rules"
Copy-DirectoryIfExists "report-templates" "$Output/report-templates"
Copy-DirectoryIfExists "docs" "$Output/docs"
Copy-DirectoryIfExists "samples" "$Output/samples"
Copy-DirectoryIfExists "licenses" "$Output/licenses"

if (-not $SkipTools) {
    Assert-RequiredFile "third_party_tools/llvm/bin/clang-tidy.exe"
    Assert-RequiredFile "third_party_tools/cppcheck/cppcheck.exe"
    Assert-RequiredFile "third_party_tools/lizard/lizard.exe"
    Copy-DirectoryIfExists "third_party_tools/llvm" "$Output/tools/llvm"
    Copy-DirectoryIfExists "third_party_tools/cppcheck" "$Output/tools/cppcheck"
    Copy-DirectoryIfExists "third_party_tools/lizard" "$Output/tools/lizard"
}

Write-Host "Release directory generated: $Output"
