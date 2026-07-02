# CodeCheck V1 Requirements Trace

This document maps the first-version design document and the confirmed takeover decisions into trackable implementation and verification items.

Source of truth:

- Design document: `docs/CodeCheck_Desktop_第一版方案设计.md`
- Confirmed takeover decisions:
  - V1 must be self-contained for .NET.
  - V1 must bundle `clang-tidy`, `cppcheck`, and `lizard`.
  - V1 GUI must implement the full multi-page workflow.
  - V1 does not include XLSX.
  - Every rule must declare one detection method: `builtin`, `clang-tidy`, `cppcheck`, `lizard`, or `manual`.
  - Cleanup and CI happen before broad feature work.

## Status Legend

- `Done`: implemented and verified in the current workspace.
- `Partial`: some code/data exists, but the design requirement is not complete or not verified.
- `Not Started`: no meaningful implementation found.
- `Deferred`: explicitly out of V1.
- `Needs Verification`: implementation likely exists, but current environment cannot verify it yet.

## Product And Packaging

| ID | Requirement | Status | Evidence | V1 Action |
|---|---|---:|---|---|
| V1-PKG-001 | Runs on Windows 10. | Needs Verification | Target is `net8.0-windows`; current machine lacks .NET runtime. | Verify on Windows after VS/.NET install and release build. |
| V1-PKG-002 | `release` directory is copy-and-run. | Partial | `release` exists but app is framework-dependent and tools are missing. | Build self-contained `win-x64` release and validate after moving directory. |
| V1-PKG-003 | Does not require administrator rights. | Needs Verification | No installer or service dependency found. | Add release smoke test from normal user path. |
| V1-PKG-004 | Supports Chinese and space-containing paths. | Partial | Some path handling uses `ProcessStartInfo.ArgumentList`; historical reports show old absolute paths. | Add automated path smoke tests for copied release. |
| V1-PKG-005 | Bundles third-party tools under `release/tools`. | Not Started | `release/tools` missing required executables. | Add tool acquisition, version manifest, copy, license, and health checks. |
| V1-PKG-006 | Self-contained .NET runtime. | Not Started | Current `release\CodeCheck.Cli.exe` fails without runtime. | Publish CLI/Desktop with `--self-contained true -r win-x64`. |
| V1-PKG-007 | WebView2 availability is checked. | Not Started | WPF app does not use WebView2 yet. | Add health check and report preview fallback. |

## CLI And Scan Flow

| ID | Requirement | Status | Evidence | V1 Action |
|---|---|---:|---|---|
| V1-CLI-001 | `CodeCheck.Cli --version`. | Needs Verification | `Program.cs` implements `--version`; runtime missing. | Verify after .NET install. |
| V1-CLI-002 | `validate --config`. | Needs Verification | `ScanOrchestrator.ValidateAsync` exists. | Verify with valid and invalid configs. |
| V1-CLI-003 | `scan --config`. | Needs Verification | `ScanOrchestrator.ScanAsync` exists. | Verify against all sample configs. |
| V1-CLI-004 | Scan directory input. | Partial | `FileDiscoveryService` supports directories; sample coverage is thin. | Add tests and sample config. |
| V1-CLI-005 | Scan single file input. | Partial | Explicit file path supported. | Add CLI-level test. |
| V1-CLI-006 | Scan file-list input. | Partial | `file-list` mode reads `Input.FileList`; missing invalid-file behavior clarity. | Add tests for missing entries and report behavior. |
| V1-CLI-007 | Explicit scan of `.h/.hpp/.hh/.hxx`. | Partial | File discovery supports explicit headers. | Add CLI sample test for C and C++ header modes. |
| V1-CLI-008 | Pause, resume, cancel. | Partial | Control file service exists; engines are not per-file cancellable. | Add status semantics and E2E cancel verification. |
| V1-CLI-009 | JSON Lines progress events. | Partial | `CliProgressReporter` exists but events are minimal. | Align event schema with design doc. |
| V1-CLI-010 | Exit codes per design. | Partial | Current code returns 0, 2, 6. | Document and test all intended codes. |
| V1-CLI-011 | Minimum failed report on cancel/config error. | Partial | Cancel report exists; config-error report not implemented. | Generate minimal failure report for config/dependency failures. |

## Configuration And File Discovery

| ID | Requirement | Status | Evidence | V1 Action |
|---|---|---:|---|---|
| V1-CONFIG-001 | Default `codecheck.json` schema fields. | Partial | `default-codecheck.json` now declares language standards and V1 report formats. More Qt/settings fields remain for later GUI/release work. | Continue Qt/settings reconciliation in GUI phase. |
| V1-CONFIG-002 | C11 and C++14 standards. | Done | `BuildConfig` exposes `cStandard` and `cppStandard`; `CompileContextBuilder` uses config values; tests cover custom standards. | Surface these fields in GUI config page. |
| V1-CONFIG-003 | Include paths and macros. | Partial | Config and parsers support includes/defines. | Add CLI and GUI tests. |
| V1-CONFIG-004 | `.vcxproj` parsing. | Partial | Parser exists and unit tests exist. | Verify with realistic Visual Studio sample. |
| V1-CONFIG-005 | `.pro` parsing. | Partial | Parser exists and unit tests exist. | Expand `qt-demo` and verify. |
| V1-CONFIG-006 | Qt root and modules. | Not Started | Config model lacks full Qt settings. | Add Qt config model and GUI page fields. |
| V1-CONFIG-007 | Default excluded directories/files. | Partial | Config and file discovery exclude directories and file wildcards. | Add tests for `excludePatterns` and generated Qt files. |
| V1-CONFIG-008 | Require compile context unless degraded scan allowed. | Partial | Validator checks manual include/project files only before auto discovery. | Reconcile validation with auto project discovery. |

## Rules

| ID | Requirement | Status | Evidence | V1 Action |
|---|---|---:|---|---|
| V1-RULE-001 | 100 total rules. | Done | Rule files contain 22 + 38 + 20 + 20 rules. | Keep count gate in tests. |
| V1-RULE-002 | Default profile can load all enabled rules. | Partial | `rule-profiles.json` lists 95 enabled; 5 manual/default disabled. | Confirm intended enabled count and report manual rules clearly. |
| V1-RULE-003 | Every rule declares detection method. | Done | Detection methods present across rule files. | Add validation for allowed values. |
| V1-RULE-004 | Detection methods limited to `builtin`, `clang-tidy`, `cppcheck`, `lizard`, `manual`. | Needs Verification | Current values appear in this set. | Add automated rule metadata test. |
| V1-RULE-005 | Rule mapping file drives engine-to-rule normalization. | Not Started | `rule-mapping.json` is empty and mappings are hard-coded. | Implement mapping model or explicitly document hard-coded v1 behavior. |
| V1-RULE-006 | Rule close/disable requires reason. | Not Started | No CLI/GUI rule-disable workflow found. | Add disabled rules config and GUI validation. |
| V1-RULE-007 | High-risk rule disable requires risk confirmation. | Not Started | No workflow found. | Add severity-based confirmation. |

## Analysis Engines

| ID | Requirement | Status | Evidence | V1 Action |
|---|---|---:|---|---|
| V1-ENG-001 | clang-tidy runner. | Partial | Runner and parser exist. | Verify with bundled tool and sample issues. |
| V1-ENG-002 | cppcheck runner. | Partial | Runner and parser exist. | Verify with bundled tool and sample issues. |
| V1-ENG-003 | lizard runner. | Partial | Runner and parser exist. | Verify complexity and function length issues. |
| V1-ENG-004 | builtin runner. | Partial | Detects 5 concrete patterns. | Align builtin coverage with `builtin` rule metadata or mark unsupported. |
| V1-ENG-005 | Single engine/file failure does not stop full scan. | Partial | Runner failures are collected; no per-file third-party failure matrix yet. | Add integration tests. |
| V1-ENG-006 | Engine version fixed. | Not Started | No `third-party-versions.json`. | Add version manifest and CI/release check. |

## Reports

| ID | Requirement | Status | Evidence | V1 Action |
|---|---|---:|---|---|
| V1-RPT-001 | `report.json`. | Partial | Writer exists; report schema now includes `metrics`, `disabledRules`, `baseline`, `suppression`, `outputs`, and failed files. Metrics aggregation and disabled-rule workflows still need implementation. | Populate metrics and disabled rules from scan/rule-management workflows. |
| V1-RPT-002 | `report.html`. | Partial | Basic HTML exists, not full design presentation. | Expand summary, failed files, suppressed issues, disabled rules, metrics. |
| V1-RPT-003 | `report.sarif`. | Partial | Basic SARIF exists. | Validate with SARIF schema where possible. |
| V1-RPT-004 | `report.csv`. | Done | CSV writer exists; V1 accepts CSV instead of XLSX. | Keep tests. |
| V1-RPT-005 | `report.xlsx`. | Deferred | User confirmed XLSX not required in V1; config validation rejects `xlsx` in `report.formats`. | Keep GUI/export XLSX absent or disabled for V1. |
| V1-RPT-006 | Failed files shown in reports. | Partial | Report and HTML include failed files. | Add E2E failed-file test. |
| V1-RPT-007 | Complexity metrics and top lists. | Not Started | No metrics model found in report. | Add lizard metrics aggregation or mark scope explicitly. |
| V1-RPT-008 | Quality score 100-point system. | Done | `QualityScoreService` exists and tests cover scoring. | Verify Chinese labels after encoding cleanup. |

## Baseline And Suppression

| ID | Requirement | Status | Evidence | V1 Action |
|---|---|---:|---|---|
| V1-BASE-001 | First scan auto-creates baseline. | Partial | Service and tests exist. | Add CLI E2E test. |
| V1-BASE-002 | New/Existing/Fixed/NotCompared. | Partial | Service supports states. | Add report-level verification. |
| V1-BASE-003 | GUI update baseline. | Not Started | No full baseline management page. | Implement in multi-page GUI. |
| V1-SUP-001 | Suppress by fingerprint. | Partial | Service and tests exist. | Add GUI workflow. |
| V1-SUP-002 | Suppress by file-rule/path-rule. | Partial | Service supports scopes and tests exist. | Add GUI workflow. |
| V1-SUP-003 | Disabled suppression records are preserved. | Partial | Status field exists; no disable command/page. | Add disable workflow. |
| V1-SUP-004 | Suppressed issues excluded from score. | Done | Quality score tests cover this. | Keep regression tests. |

## Desktop GUI

| ID | Requirement | Status | Evidence | V1 Action |
|---|---|---:|---|---|
| V1-GUI-001 | Left navigation and content area. | Not Started | Current WPF is single window/panel. | Introduce MVVM navigation shell. |
| V1-GUI-002 | Home/tool health page. | Partial | Current window can run validate but no health dashboard. | Add health check view model/service. |
| V1-GUI-003 | New scan page. | Not Started | Current panel selects workspace/config only. | Add directory/file/file-list workflow. |
| V1-GUI-004 | Scan config page. | Not Started | No include/define/Qt/project-file UI. | Add config editor view. |
| V1-GUI-005 | Scan progress page. | Partial | Current panel reads status file. | Move into progress page and parse JSON Lines/status. |
| V1-GUI-006 | Results page. | Partial | Current grid shows first 100 issues. | Add full filters, details, failed files, suppression actions. |
| V1-GUI-007 | Report preview page using WebView2. | Not Started | No WebView2 package/use found. | Add WebView2 preview and fallback. |
| V1-GUI-008 | Rule management page. | Not Started | No rule page. | Add rule list, filtering, disable reason workflow. |
| V1-GUI-009 | Baseline management page. | Not Started | No page. | Add baseline view/update action. |
| V1-GUI-010 | Suppression management page. | Not Started | No page/dialog. | Add suppress dialog and list. |
| V1-GUI-011 | Settings page. | Partial | Settings service stores basic paths. | Add tool paths, report dirs, Qt defaults. |
| V1-GUI-012 | Export XLSX button. | Deferred | User confirmed no XLSX in V1. | Remove or disable XLSX UI for V1. |

## Samples And Acceptance Tests

| ID | Requirement | Status | Evidence | V1 Action |
|---|---|---:|---|---|
| V1-SAMPLE-001 | Complete `c-demo`. | Partial | Only `main.c`, `unsafe_string.c`, and header exist. | Add memory, complexity, long-function files. |
| V1-SAMPLE-002 | Complete `cpp-demo`. | Partial | Only `main.cpp`, `bad_header.hpp`, and header exist. | Add class, memory, exception, complexity, long-function files. |
| V1-SAMPLE-003 | Complete `qt-demo`. | Not Started | README only. | Add `.pro`, source, include, UI and generated-file exclusion cases. |
| V1-TEST-001 | Unit tests. | Partial | 37 xUnit facts found. | Run and expand after .NET install. |
| V1-TEST-002 | CLI E2E tests. | Not Started | No dedicated E2E harness. | Add release/sample scan tests. |
| V1-TEST-003 | GUI smoke tests. | Not Started | No UI automation. | Add manual checklist first, automated smoke where stable. |
| V1-TEST-004 | Release path movement tests. | Not Started | Not present. | Add PowerShell release verification. |
| V1-TEST-005 | CI build/test/release checks. | Not Started | No `.github/workflows`. | Add CI workflow after cleanup. |

## Explicit V1 Deferrals

| ID | Deferred Item | Reason |
|---|---|---|
| V1-DEF-001 | XLSX generation/export. | User confirmed V1 can ship without XLSX. |
| V1-DEF-002 | Installer. | Release directory copy-and-run is the first-version target. |
| V1-DEF-003 | Auto update. | Out of first-version scope. |
| V1-DEF-004 | Server-side/multi-user workflows. | Out of first-version scope. |
| V1-DEF-005 | SonarQube server integration. | Only concepts are borrowed. |
| V1-DEF-006 | Source-code comment suppression. | Design explicitly excludes this for V1. |

## Immediate Execution Order

1. Clean repository boundaries and add ignore policy.
2. Install/verify Visual Studio 2022 and .NET SDK/runtime.
3. Make build and tests reproducible locally.
4. Add CI workflow that mirrors local verification.
5. Rework release build to be self-contained and tool-bundled.
6. Complete sample projects and E2E test configurations.
7. Reconcile config/report/rule schemas with the design.
8. Implement full multi-page WPF GUI.
