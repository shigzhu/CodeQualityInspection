param(
    [string]$ReleaseRoot = "release",
    [switch]$Full
)

if (-not (Test-Path $ReleaseRoot)) {
    return
}

if ($Full) {
    Remove-Item -Recurse -Force $ReleaseRoot
    return
}

Get-ChildItem $ReleaseRoot -Exclude reports,baseline,suppressions,logs | Remove-Item -Recurse -Force
