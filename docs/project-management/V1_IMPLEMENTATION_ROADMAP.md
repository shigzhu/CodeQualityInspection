# CodeCheck V1 Implementation Roadmap

## Goal

Move CodeCheck from the current partial implementation to a V1 offline desktop product that matches the confirmed scope.

## Confirmed V1 Scope

- Self-contained .NET release.
- Bundled `clang-tidy`, `cppcheck`, and `lizard`.
- Full multi-page WPF GUI.
- No XLSX in V1.
- Every rule declares a detection method.
- Cleanup and CI first.

## Phase 0: Environment And Workspace Baseline

1. Install Visual Studio 2022 with .NET desktop development workload.
2. Confirm `.NET 8 SDK` is available:

```powershell
dotnet --info
```

3. Run initial local verification:

```powershell
dotnet restore .\CodeCheck.sln
dotnet build .\CodeCheck.sln -c Release
dotnet test .\tests\CodeCheck.Tests\CodeCheck.Tests.csproj -c Release
```

4. Record all failures before changing implementation.

Exit gate:

- Build/test status is known.
- Existing failures are triaged.

## Phase 1: Cleanup And CI

Tasks:

- Add `.gitignore`.
- Decide archive/delete policy for `release`, `reports`, `src-decrypted`, `bin`, `obj`.
- Add generated artifact guard.
- Add rule metadata tests.
- Add GitHub Actions workflow on `windows-latest`.

Exit gate:

- CI builds solution.
- CI runs tests.
- CI checks rule count and allowed detection values.
- Generated artifacts are excluded.

## Phase 2: Config, Rule, And Report Schema Reconciliation

Tasks:

- Align `CodeCheckConfig` with design document fields.
- Add allowed detection validation.
- Decide whether `rule-mapping.json` becomes authoritative in V1.
- Align `ScanReport` with design fields:
  - metrics
  - disabled rules
  - failed files
  - baseline
  - suppression
  - outputs
- Remove or mark XLSX as disabled/deferred in CLI/GUI/report options.

Exit gate:

- Default config validates.
- 100-rule metadata test passes.
- Report schema test covers required V1 fields except deferred XLSX.

## Phase 3: Bundled Tools And Self-Contained Release

Tasks:

- Add third-party tool version manifest.
- Stage fixed versions under `third_party_tools`.
- Update build-release script to self-contained by default.
- Update check-release script to validate tools and licenses.
- Add tool locator that resolves relative to executable/release root.
- Add Desktop Home health check.

Exit gate:

- Release runs on a machine without global .NET.
- Release validates bundled tools.
- Release works after being moved to a new path.

## Phase 4: Samples And CLI E2E

Tasks:

- Complete `samples/c-demo`.
- Complete `samples/cpp-demo`.
- Complete `samples/qt-demo`.
- Add sample configs under `samples/**` or `tests/TestData`.
- Add CLI E2E tests for:
  - directory scan
  - single file scan
  - explicit header scan
  - file-list scan
  - failed file behavior
  - baseline first/second scan
  - suppression application

Exit gate:

- CLI scans sample projects and produces JSON/HTML/SARIF/CSV.
- Expected core rule IDs appear in sample reports.

## Phase 5: Full Multi-Page WPF GUI

Tasks:

- Introduce MVVM navigation shell.
- Add pages:
  - Home
  - New Scan
  - Scan Config
  - Scan Progress
  - Results
  - Report Preview
  - Rule Management
  - Baseline Management
  - Suppression Management
  - Settings
- Add WebView2 report preview.
- Add scan session model and temporary config generation.
- Add suppression dialog with required reason.
- Add rule disable dialog with required reason and high-risk confirmation.

Exit gate:

- GUI can complete directory, single-file, and explicit-header scan flows.
- GUI displays report summary and issues.
- GUI can mark suppression and update baseline.
- GUI previews HTML report.

## Phase 6: Acceptance And Hardening

Tasks:

- Run all design acceptance cases except XLSX.
- Test Chinese path.
- Test path with spaces.
- Test moved release.
- Test missing tool behavior.
- Test missing WebView2 behavior.
- Review logs and error messages.

Exit gate:

- No first-version "not pass" condition remains except explicitly deferred XLSX.
- Release is ready for user trial.

## Verification Matrix

| Area | Command Or Check |
|---|---|
| Build | `dotnet build .\CodeCheck.sln -c Release` |
| Unit tests | `dotnet test .\tests\CodeCheck.Tests\CodeCheck.Tests.csproj -c Release` |
| Release build | `.\scripts\build-release.ps1` |
| Release check | `.\scripts\check-release.ps1 -ReleaseRoot .\release` |
| CLI version | `.\release\CodeCheck.Cli.exe --version` |
| CLI validate | `.\release\CodeCheck.Cli.exe validate --config .\release\configs\default-codecheck.json` |
| CLI scan C sample | scan `samples/c-demo` using release config |
| CLI scan C++ sample | scan `samples/cpp-demo` using release config |
| GUI smoke | launch Desktop, run scan, preview report |

## Near-Term Next Step

After Visual Studio 2022 is installed, run Phase 0 verification and capture the exact failures. Then start Phase 1 cleanup and CI.
