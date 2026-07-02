param(
    [string]$ReleaseRoot = "release",
    [switch]$SkipExecution
)

$failed = $false

function Test-RequiredPath([string]$Path) {
    $fullPath = Join-Path $ReleaseRoot $Path
    if (Test-Path -LiteralPath $fullPath) {
        Write-Host "[OK] $Path"
    }
    else {
        Write-Host "[ERROR] $Path"
        $script:failed = $true
    }
}

$items = @(
    "CodeCheck.Cli.exe",
    "CodeCheck.Desktop.exe",
    "configs/default-codecheck.json",
    "rules/rules.index.json",
    "docs",
    "samples",
    "tools/third-party-versions.json",
    "tools/llvm/bin/clang-tidy.exe",
    "tools/cppcheck/cppcheck.exe",
    "tools/lizard/lizard.exe",
    "licenses",
    "reports",
    "baseline",
    "suppressions",
    "logs",
    "temp"
)

foreach ($item in $items) {
    Test-RequiredPath $item
}

if (-not $SkipExecution) {
    $cliPath = Join-Path $ReleaseRoot "CodeCheck.Cli.exe"
    $configPath = Join-Path $ReleaseRoot "configs/default-codecheck.json"

    if (Test-Path -LiteralPath $cliPath) {
        & $cliPath --version
        if ($LASTEXITCODE -ne 0) {
            Write-Host "[ERROR] CodeCheck.Cli.exe --version failed."
            $failed = $true
        }

        if (Test-Path -LiteralPath $configPath) {
            & $cliPath validate --config $configPath
            if ($LASTEXITCODE -ne 0) {
                Write-Host "[ERROR] CodeCheck.Cli.exe validate failed."
                $failed = $true
            }
        }
    }
}

if ($failed) {
    exit 1
}

Write-Host "[OK] Release checks passed."
