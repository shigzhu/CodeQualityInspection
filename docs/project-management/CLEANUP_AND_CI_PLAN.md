# CodeCheck Cleanup And CI Plan

## Goal

Create a clean source-of-truth workspace and a stable CI pipeline before broad V1 feature work.

## Principles

- Do not delete user-important history without review.
- Keep source, docs, configs, tests, and scripts under version control.
- Treat build outputs, reports, temp files, and copied release packages as generated artifacts.
- CI should verify the same commands a developer runs locally.
- CI must not require bundled third-party analyzer binaries until the tool-bundling step is complete.

## Cleanup Scope

### Keep As Source

- `CodeCheck.sln`
- `src/**`
- `tests/**`, excluding `bin` and `obj`
- `configs/**`
- `rules/**`
- `samples/**`
- `scripts/**`
- `docs/**`
- `.github/copilot-instructions.md`
- `C++Languagelawer.pdf` if it is the intended source reference for the rule set.
- `baseline/**` only if baseline fixtures are intentionally used for tests.

### Treat As Generated

- `.vs/**`
- `src/**/bin/**`
- `src/**/obj/**`
- `tests/**/bin/**`
- `tests/**/obj/**`
- `release/**`
- `reports/**`
- `temp/**`
- `logs/**`
- `suppressions/**`
- `src-decrypted/**`

### Needs User Confirmation Before Removal

- Historical `reports/**`
- Existing `release/**`
- `src-decrypted/**`
- Existing `baseline/**`
- Existing `suppressions/**`

These can be moved to an archive folder outside the repo before deletion if preservation is needed.

## `.gitignore` Policy

Add or update `.gitignore` to exclude:

```gitignore
.vs/
**/bin/
**/obj/
release/
reports/
temp/
logs/
suppressions/
src-decrypted/
*.user
*.suo
TestResults/
coverage/
```

If `baseline` is used for test fixtures, keep it. If it is runtime data, ignore it and move fixtures under `tests/CodeCheck.Tests/TestData`.

## Local Verification Commands

After Visual Studio 2022 and .NET SDK are installed:

```powershell
dotnet --info
dotnet restore .\CodeCheck.sln
dotnet build .\CodeCheck.sln -c Release --no-restore
dotnet test .\tests\CodeCheck.Tests\CodeCheck.Tests.csproj -c Release --no-build --logger "trx;LogFileName=tests.trx"
.\scripts\check-release.ps1 -ReleaseRoot .\release
```

Until third-party tools are bundled, release checks should mark missing tools as warnings or separate expected failures.

## CI Workflow Design

Create `.github/workflows/ci.yml` with:

1. `checkout`
2. `setup-dotnet` for .NET 8
3. restore
4. build Release
5. test Release
6. upload test results on failure
7. run source hygiene checks:
   - no committed `bin/obj`
   - rule count is 100
   - rule detection method is one of the allowed values
   - no empty `ruleId`
8. optional release package check after self-contained release is implemented

## CI Phases

### Phase 1: Source CI

Runs on every push/PR:

- restore
- build
- unit tests
- rule metadata validation
- generated artifact guard

### Phase 2: Release CI

Runs after release script is fixed:

- `scripts/build-release.ps1 -SelfContained`
- `scripts/check-release.ps1`
- CLI `--version`
- CLI `validate` against a release config with bundled tools

### Phase 3: E2E CI

Runs after bundled tools and sample projects are complete:

- scan `samples/c-demo`
- scan `samples/cpp-demo`
- scan explicit header
- verify report JSON/HTML/SARIF/CSV exist
- verify expected issue rule IDs appear

## Risks

| Risk | Mitigation |
|---|---|
| CI cannot run WPF Desktop on hosted Linux. | Use `windows-latest` and build WPF there. |
| Third-party tools are large or license-sensitive. | Keep them out of source; stage them from a local `third_party_tools` folder or release asset process. |
| Historical reports are useful for comparison. | Archive before cleanup. |
| Generated files hide real source changes. | Add artifact guard and `.gitignore` before feature work. |

## Acceptance Criteria

- Working tree source-of-truth is clear.
- `.gitignore` prevents generated artifacts from returning.
- `dotnet build` and `dotnet test` pass locally.
- CI can build and test on `windows-latest`.
- Rule metadata checks run in CI.
- Release and E2E jobs are added once tool bundling is complete.

## Execution Notes

- Cleanup policy applied on: 2026-07-02.
- Generated artifacts are now ignored by `.gitignore`.
- Current workspace is not a git repository, so tracked-file hygiene is skipped locally and will run in CI once this project is under git.
- Existing historical/generated directories are still present locally and require user confirmation before archive/delete:
  - `.vs`
  - `release`
  - `reports`
  - `temp`
  - `suppressions`
  - `src-decrypted`
- Added `scripts/check-source-hygiene.ps1` as a stable source/rule metadata gate.
- Added `.github/workflows/ci.yml` for Windows build/test/source-hygiene CI.
