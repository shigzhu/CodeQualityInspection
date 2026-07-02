param(
    [string]$Root = ".",
    [switch]$StrictFilesystem
)

$ErrorActionPreference = "Stop"

$rootPath = (Resolve-Path -LiteralPath $Root).Path
$failed = $false

function Write-CheckError([string]$Message) {
    Write-Host "[ERROR] $Message"
    $script:failed = $true
}

function Test-IsGitRepository([string]$Path) {
    $gitCommand = Get-Command git -ErrorAction SilentlyContinue
    if ($null -eq $gitCommand) {
        return $false
    }

    $previousErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        $output = & git -C $Path rev-parse --is-inside-work-tree 2>$null
        return $LASTEXITCODE -eq 0 -and $output -eq "true"
    }
    finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }
}

function Test-ForbiddenPath([string]$RelativePath) {
    $normalized = $RelativePath.Replace('\', '/').TrimStart('/')
    return $normalized -match '(^|/)\.vs(/|$)' -or
        $normalized -match '(^|/)bin(/|$)' -or
        $normalized -match '(^|/)obj(/|$)' -or
        $normalized -match '(^|/)TestResults(/|$)' -or
        $normalized -match '(^|/)coverage(/|$)' -or
        $normalized -match '^release(/|$)' -or
        $normalized -match '^reports(/|$)' -or
        $normalized -match '^temp(/|$)' -or
        $normalized -match '^logs(/|$)' -or
        $normalized -match '^suppressions(/|$)' -or
        $normalized -match '^src-decrypted(/|$)'
}

function Get-RelativePath([string]$BasePath, [string]$Path) {
    return [System.IO.Path]::GetRelativePath($BasePath, $Path)
}

$isGitRepository = Test-IsGitRepository $rootPath

if ($isGitRepository) {
    $trackedFiles = & git -C $rootPath ls-files
    foreach ($trackedFile in $trackedFiles) {
        if (Test-ForbiddenPath $trackedFile) {
            Write-CheckError "Generated artifact is tracked by git: $trackedFile"
        }
    }
}
else {
    Write-Host "[WARN] Not a git repository; skipping tracked-file hygiene check."
}

if ($StrictFilesystem) {
    $allFiles = Get-ChildItem -LiteralPath $rootPath -Recurse -File -Force |
        Where-Object { Test-ForbiddenPath (Get-RelativePath $rootPath $_.FullName) }

    foreach ($file in $allFiles) {
        Write-CheckError "Generated artifact exists in workspace: $($file.FullName)"
    }
}
else {
    $generatedRoots = @(".vs", "release", "reports", "temp", "logs", "suppressions", "src-decrypted")
    foreach ($generatedRoot in $generatedRoots) {
        $path = Join-Path $rootPath $generatedRoot
        if (Test-Path -LiteralPath $path) {
            Write-Host "[WARN] Generated directory exists locally: $generatedRoot"
        }
    }
}

$ruleFiles = @(
    "rules/quectel-c-rules.json",
    "rules/quectel-cpp-rules.json",
    "rules/cert-c-rules.json",
    "rules/cert-cpp-rules.json"
)

$allRules = @()
foreach ($ruleFile in $ruleFiles) {
    $path = Join-Path $rootPath $ruleFile
    if (-not (Test-Path -LiteralPath $path)) {
        Write-CheckError "Missing rule file: $ruleFile"
        continue
    }

    $json = Get-Content -Raw -Path $path | ConvertFrom-Json
    $allRules += $json.rules
}

if ($allRules.Count -ne 100) {
    Write-CheckError "Expected 100 rules, found $($allRules.Count)"
}

$allowedDetection = @("builtin", "clang-tidy", "cppcheck", "lizard", "manual")
$seenRuleIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($rule in $allRules) {
    if ([string]::IsNullOrWhiteSpace($rule.id)) {
        Write-CheckError "Rule has empty id"
        continue
    }

    if (-not $seenRuleIds.Add([string]$rule.id)) {
        Write-CheckError "Duplicate rule id: $($rule.id)"
    }

    if ($allowedDetection -notcontains $rule.detection) {
        Write-CheckError "Rule $($rule.id) has invalid detection '$($rule.detection)'"
    }
}

if ($failed) {
    exit 1
}

Write-Host "[OK] Source hygiene checks passed."
