# CodeCheck Takeover Notes

## Evidence Reviewed

- Solution/project files: `CodeCheck.sln`, `src/**/*.csproj`, `tests/CodeCheck.Tests/CodeCheck.Tests.csproj`.
- Planning document: `docs/CodeCheck_Desktop_第一版方案设计.md`.
- Source areas: CLI orchestration, configuration, file discovery, build context parsers, analysis engines, baseline, suppression, reporting, WPF desktop.
- Data/config: `configs/default-codecheck.json`, `rules/*.json`.
- Release/sample/report artifacts: `release`, `samples`, `reports`, `baseline`.

## Current Shape

- .NET 8/C# multi-project solution:
  - `CodeCheck.Core`
  - `CodeCheck.Cli`
  - `CodeCheck.Reporting`
  - `CodeCheck.Desktop`
  - `CodeCheck.Tests`
- Rule corpus exists and matches the planned 100-rule target:
  - 22 C rules
  - 38 C++ rules
  - 20 CERT C rules
  - 20 CERT C++ rules
- Rule detection metadata distribution:
  - 19 builtin
  - 72 clang-tidy
  - 4 lizard
  - 5 manual

## Positive Findings

- The project has moved beyond skeleton/MVP: CLI orchestration, report generation, baseline, suppression, score calculation, and a simple WPF shell all exist.
- Unit tests exist for major non-UI services and parsers.
- Historical reports show JSON/HTML/SARIF/CSV generation and baseline comparison have been exercised at least once.

## Key Gaps

- Current machine has no .NET runtime/SDK on PATH; the release executable cannot run here.
- Release is framework-dependent by default, not self-contained; that weakens the "offline ready" goal.
- `release/tools` does not contain clang-tidy/cppcheck/lizard, and PATH lookup did not find those tools.
- `rule-mapping.json` is empty; actual mappings are hard-coded inside engine runners.
- CLI does not implement planned `baseline create/update/compare`, `suppress add/disable/list`, or `export xlsx` commands.
- Desktop is a single operational panel, not the multi-page GUI described in the plan.
- No CI workflow is present.
- Generated artifacts (`bin`, `obj`, `reports`, `release`, `src-decrypted`) are mixed into the working tree.

## Preliminary Completion Estimate

- Core architecture and code foundation: about 70%.
- CLI scan/report baseline: about 55-65%, depending on external tool packaging.
- Rules corpus metadata: about 80% as data, much lower as validated detection behavior.
- Desktop GUI: about 25-35% relative to planned first-version UX.
- Release/offline packaging: about 30-40%.
- Overall first-version acceptance readiness: about 50-60%.

## Recommended Discussion Topics

1. Confirm whether "offline" means no internet only, or also no preinstalled .NET/runtime/tools.
2. Decide whether first release must include self-contained .NET and bundled third-party analyzers.
3. Decide whether Excel/XLSX is required for v1 or remains optional after JSON/HTML/SARIF/CSV.
4. Decide whether GUI must be multi-page per design or whether a simpler operator panel is acceptable for v1.
5. Decide how strict the 100-rule claim should be: rule documentation only, tool-assisted approximate detection, or verified per-rule test cases.
6. Clean source-of-truth layout and remove/generated-ignore bulky artifacts before serious continuation.

## Confirmed V1 Decisions

- Visual Studio 2022 environment will be installed before full build/test verification.
- V1 must be self-contained for .NET.
- V1 must bundle clang-tidy, cppcheck, and lizard.
- V1 GUI should implement the full multi-page workflow from the design document.
- V1 does not need XLSX; JSON/HTML/SARIF/CSV are enough for the first release.
- V1 rule acceptance uses option B: every rule must clearly declare its detection method (`builtin`, `clang-tidy`, `cppcheck`, `lizard`, or `manual`).
- Cleanup and CI should happen first, and CI should be as comprehensive and stable as practical.

## Phase 0 Verification Results

- `dotnet --info`: PASS. SDK `9.0.315` is installed; .NET 8 runtime and Windows Desktop runtime `8.0.28` are installed.
- MSBuild: PASS. Visual Studio 2022 Professional MSBuild `17.14.40.60911` found at `C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe`.
- `dotnet restore .\CodeCheck.sln`: PASS. All projects are up to date.
- `dotnet build .\CodeCheck.sln -c Release --no-restore`: PASS. 0 warnings, 0 errors.
- `dotnet test .\tests\CodeCheck.Tests\CodeCheck.Tests.csproj -c Release --no-build`: PASS before Phase 1. 37 passed, 0 failed.

## Phase 1 Verification Results

- Added `.gitignore`, `scripts/check-source-hygiene.ps1`, `.github/workflows/ci.yml`, and `tests/CodeCheck.Tests/Rules/RuleMetadataValidationTests.cs`.
- `dotnet test .\tests\CodeCheck.Tests\CodeCheck.Tests.csproj -c Release --filter RuleMetadataValidationTests`: PASS. 4 passed, 0 failed.
- `dotnet build .\CodeCheck.sln -c Release --no-restore`: PASS. 0 warnings, 0 errors.
- `dotnet test .\tests\CodeCheck.Tests\CodeCheck.Tests.csproj -c Release --no-build`: PASS. 41 passed, 0 failed.
- `.\scripts\check-source-hygiene.ps1`: PASS. Warns that this is not a git repository and that historical generated directories still exist locally.
- `.\src\CodeCheck.Cli\bin\Release\net8.0\CodeCheck.Cli.exe --version`: PASS. Outputs `CodeCheck.Cli 1.0.0`.
