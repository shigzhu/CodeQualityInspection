# CodeCheck Phase 1 Cleanup CI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean the CodeCheck repository boundaries and add stable CI so V1 work can proceed on a reproducible baseline.

**Architecture:** Keep source-of-truth files in the repository while excluding generated artifacts. Add rule metadata tests in the existing xUnit project and a Windows GitHub Actions workflow that mirrors local build/test checks.

**Tech Stack:** C#/.NET 8, xUnit, PowerShell, GitHub Actions on `windows-latest`.

---

## File Structure

Create or modify these files:

- Create: `.gitignore`
- Create: `.github/workflows/ci.yml`
- Create: `tests/CodeCheck.Tests/Rules/RuleMetadataValidationTests.cs`
- Modify: `tests/CodeCheck.Tests/CodeCheck.Tests.csproj` only if needed for test data copying; first attempt should not require changes.
- Create: `scripts/check-source-hygiene.ps1`
- Modify: `docs/project-management/CLEANUP_AND_CI_PLAN.md` after execution to record final cleanup decisions.

Do not delete `release`, `reports`, `src-decrypted`, `baseline`, or `suppressions` until the user confirms archive/delete policy.

---

### Task 1: Add Repository Ignore Policy

**Files:**
- Create: `.gitignore`

- [ ] **Step 1: Create `.gitignore`**

Add:

```gitignore
# Visual Studio
.vs/
*.user
*.suo

# .NET build outputs
**/bin/
**/obj/
TestResults/
coverage/

# CodeCheck generated runtime outputs
release/
reports/
temp/
logs/
suppressions/
src-decrypted/

# OS/editor noise
.DS_Store
Thumbs.db
```

- [ ] **Step 2: Verify ignored files are hidden from future status**

Run:

```powershell
git status --short --ignored
```

Expected:

- `.gitignore` appears as new.
- Generated folders appear as ignored if this workspace is a git repository.
- If this workspace is not a git repository, record that in the final notes and continue.

---

### Task 2: Add Source Hygiene Script

**Files:**
- Create: `scripts/check-source-hygiene.ps1`

- [ ] **Step 1: Create script**

Add:

```powershell
param(
    [string]$Root = "."
)

$ErrorActionPreference = "Stop"

$forbiddenPatterns = @(
    "\\.vs($|\\)",
    "\\bin($|\\)",
    "\\obj($|\\)",
    "\\TestResults($|\\)",
    "\\coverage($|\\)"
)

$forbiddenRoots = @(
    "src-decrypted"
)

$failed = $false

foreach ($rootName in $forbiddenRoots) {
    $path = Join-Path $Root $rootName
    if (Test-Path $path) {
        Write-Host "[WARN] Generated or duplicate directory exists: $rootName"
    }
}

$trackedCandidateFiles = Get-ChildItem -Path $Root -Recurse -File -Force |
    Where-Object {
        $relative = Resolve-Path -LiteralPath $_.FullName -Relative
        foreach ($pattern in $forbiddenPatterns) {
            if ($relative -match $pattern) {
                return $true
            }
        }
        return $false
    }

foreach ($file in $trackedCandidateFiles) {
    Write-Host "[ERROR] Generated file present under source tree: $($file.FullName)"
    $failed = $true
}

$ruleFiles = @(
    "rules/quectel-c-rules.json",
    "rules/quectel-cpp-rules.json",
    "rules/cert-c-rules.json",
    "rules/cert-cpp-rules.json"
)

$allRules = @()
foreach ($ruleFile in $ruleFiles) {
    $path = Join-Path $Root $ruleFile
    if (-not (Test-Path $path)) {
        Write-Host "[ERROR] Missing rule file: $ruleFile"
        $failed = $true
        continue
    }

    $json = Get-Content -Raw -Path $path | ConvertFrom-Json
    $allRules += $json.rules
}

if ($allRules.Count -ne 100) {
    Write-Host "[ERROR] Expected 100 rules, found $($allRules.Count)"
    $failed = $true
}

$allowedDetection = @("builtin", "clang-tidy", "cppcheck", "lizard", "manual")
foreach ($rule in $allRules) {
    if ([string]::IsNullOrWhiteSpace($rule.id)) {
        Write-Host "[ERROR] Rule has empty id"
        $failed = $true
    }

    if ($allowedDetection -notcontains $rule.detection) {
        Write-Host "[ERROR] Rule $($rule.id) has invalid detection '$($rule.detection)'"
        $failed = $true
    }
}

if ($failed) {
    exit 1
}

Write-Host "[OK] Source hygiene checks passed."
```

- [ ] **Step 2: Run source hygiene script**

Run:

```powershell
.\scripts\check-source-hygiene.ps1
```

Expected:

- May fail initially because existing `bin/obj` are present.
- Record actual failures.

- [ ] **Step 3: Adjust script if needed**

If the script fails because it scans intentionally existing generated directories before cleanup, modify it to support:

```powershell
[switch]$WarnOnlyGeneratedArtifacts
```

and use warnings before the cleanup pass. Keep rule metadata errors fatal.

---

### Task 3: Add Rule Metadata Tests

**Files:**
- Create: `tests/CodeCheck.Tests/Rules/RuleMetadataValidationTests.cs`

- [ ] **Step 1: Write tests**

Add:

```csharp
using System.Text.Json;

namespace CodeCheck.Tests.Rules;

public sealed class RuleMetadataValidationTests
{
    private static readonly string[] RuleFiles =
    [
        Path.Combine(TestRepository.Root, "rules", "quectel-c-rules.json"),
        Path.Combine(TestRepository.Root, "rules", "quectel-cpp-rules.json"),
        Path.Combine(TestRepository.Root, "rules", "cert-c-rules.json"),
        Path.Combine(TestRepository.Root, "rules", "cert-cpp-rules.json")
    ];

    private static readonly HashSet<string> AllowedDetectionMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "builtin",
        "clang-tidy",
        "cppcheck",
        "lizard",
        "manual"
    };

    [Fact]
    public void RuleFiles_ContainExactlyOneHundredRules()
    {
        var rules = LoadRules();

        Assert.Equal(100, rules.Count);
    }

    [Fact]
    public void RuleFiles_HaveUniqueNonEmptyIds()
    {
        var rules = LoadRules();
        var ids = rules.Select(rule => rule.GetProperty("id").GetString()).ToList();

        Assert.DoesNotContain(ids, string.IsNullOrWhiteSpace);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void RuleFiles_UseAllowedDetectionMethods()
    {
        var rules = LoadRules();

        foreach (var rule in rules)
        {
            var id = rule.GetProperty("id").GetString();
            var detection = rule.GetProperty("detection").GetString();

            Assert.True(
                detection is not null && AllowedDetectionMethods.Contains(detection),
                $"Rule {id} has invalid detection method '{detection}'.");
        }
    }

    [Fact]
    public void RuleFiles_ContainRequiredDisplayFields()
    {
        var rules = LoadRules();

        foreach (var rule in rules)
        {
            var id = rule.GetProperty("id").GetString();

            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("title").GetString()), $"Rule {id} missing title.");
            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("source").GetString()), $"Rule {id} missing source.");
            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("severity").GetString()), $"Rule {id} missing severity.");
            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("description").GetString()), $"Rule {id} missing description.");
            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("suggestion").GetString()), $"Rule {id} missing suggestion.");
        }
    }

    private static List<JsonElement> LoadRules()
    {
        var rules = new List<JsonElement>();

        foreach (var ruleFile in RuleFiles)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(ruleFile));
            foreach (var rule in document.RootElement.GetProperty("rules").EnumerateArray())
            {
                rules.Add(rule.Clone());
            }
        }

        return rules;
    }
}
```

- [ ] **Step 2: Run focused test**

Run after .NET SDK is installed:

```powershell
dotnet test .\tests\CodeCheck.Tests\CodeCheck.Tests.csproj -c Release --filter RuleMetadataValidationTests
```

Expected:

- PASS if rule metadata is valid.
- If it fails, fix rule metadata rather than weakening the test.

---

### Task 4: Add CI Workflow

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Create workflow**

Add:

```yaml
name: CI

on:
  push:
    branches: ["**"]
  pull_request:
    branches: ["**"]

jobs:
  build-test:
    name: Build and test
    runs-on: windows-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"

      - name: Restore
        run: dotnet restore .\CodeCheck.sln

      - name: Build
        run: dotnet build .\CodeCheck.sln -c Release --no-restore

      - name: Test
        run: dotnet test .\tests\CodeCheck.Tests\CodeCheck.Tests.csproj -c Release --no-build --logger "trx;LogFileName=tests.trx"

      - name: Source hygiene
        shell: pwsh
        run: .\scripts\check-source-hygiene.ps1

      - name: Upload test results
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: "**/TestResults/*.trx"
```

- [ ] **Step 2: Validate YAML formatting locally if tooling exists**

Run:

```powershell
Get-Content .\.github\workflows\ci.yml
```

Expected:

- File is readable and indentation is spaces.

---

### Task 5: Local Verification

**Files:**
- No new files.

- [ ] **Step 1: Verify .NET install**

Run:

```powershell
dotnet --info
```

Expected:

- Shows .NET SDK 8.x.

- [ ] **Step 2: Restore**

Run:

```powershell
dotnet restore .\CodeCheck.sln
```

Expected:

- Exit code 0.

- [ ] **Step 3: Build**

Run:

```powershell
dotnet build .\CodeCheck.sln -c Release --no-restore
```

Expected:

- Exit code 0.

- [ ] **Step 4: Test**

Run:

```powershell
dotnet test .\tests\CodeCheck.Tests\CodeCheck.Tests.csproj -c Release --no-build
```

Expected:

- Exit code 0.

- [ ] **Step 5: Hygiene**

Run:

```powershell
.\scripts\check-source-hygiene.ps1
```

Expected:

- Exit code 0 after generated artifacts are cleaned or script behavior is scoped to source-tracked files.

---

### Task 6: Update Project Management Notes

**Files:**
- Modify: `docs/project-management/CLEANUP_AND_CI_PLAN.md`
- Modify: `takeover_notes.md`

- [ ] **Step 1: Record final cleanup decision**

Add a short section to `docs/project-management/CLEANUP_AND_CI_PLAN.md`:

```markdown
## Execution Notes

- Cleanup policy applied on: 2026-07-02.
- Generated artifacts are ignored by `.gitignore`.
- Historical artifacts requiring user confirmation before deletion:
  - `release`
  - `reports`
  - `src-decrypted`
  - `baseline`
  - `suppressions`
```

- [ ] **Step 2: Record verification results**

Add to `takeover_notes.md`:

```markdown
## Phase 1 Verification Results

- `dotnet --info`: [result]
- `dotnet restore`: [result]
- `dotnet build -c Release`: [result]
- `dotnet test -c Release`: [result]
- `scripts/check-source-hygiene.ps1`: [result]
```

Replace bracketed values with actual command outcomes.

---

## Self-Review Checklist

- [ ] `.gitignore` excludes generated artifacts.
- [ ] CI runs on Windows.
- [ ] Rule metadata is tested in xUnit.
- [ ] Source hygiene script validates rule count and allowed detection methods.
- [ ] No generated directories are deleted without user confirmation.
- [ ] Local verification commands and expected outcomes are documented.
