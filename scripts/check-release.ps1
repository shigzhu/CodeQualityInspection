param(
    [string]$ReleaseRoot = "release"
)

$items = @(
    "CodeCheck.Cli.exe",
    "CodeCheck.Desktop.exe",
    "configs/default-codecheck.json",
    "rules/rules.index.json",
    "report-templates",
    "docs",
    "samples",
    "reports",
    "baseline",
    "suppressions",
    "logs",
    "temp"
)

foreach ($item in $items) {
    $path = Join-Path $ReleaseRoot $item
    if (Test-Path $path) {
        Write-Host "[OK] $item"
    } else {
        Write-Host "[ERROR] $item"
    }
}
