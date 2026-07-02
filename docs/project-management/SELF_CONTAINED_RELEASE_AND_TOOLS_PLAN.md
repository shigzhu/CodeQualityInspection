# CodeCheck Self-Contained Release And Tools Plan

## Goal

Produce a V1 `release` directory that can be copied to a Windows 10 machine and run offline without preinstalled .NET, clang-tidy, cppcheck, or lizard.

## Release Shape

```text
release/
  CodeCheck.Desktop.exe
  CodeCheck.Cli.exe
  CodeCheck.Core.dll
  CodeCheck.Reporting.dll
  configs/
  rules/
  samples/
  docs/
  tools/
    llvm/
      bin/
        clang-tidy.exe
        clang.exe
        clang++.exe
    cppcheck/
      cppcheck.exe
      cfg/
      platform/
    lizard/
      lizard.exe
  licenses/
    llvm/
    cppcheck/
    lizard/
  reports/
  baseline/
  suppressions/
  logs/
  temp/
```

## Version Manifest

Create `third_party_tools/third-party-versions.json` or `tools/third-party-versions.json`:

```json
{
  "llvm": {
    "version": "fixed-v1-version",
    "requiredFiles": [
      "bin/clang-tidy.exe"
    ]
  },
  "cppcheck": {
    "version": "fixed-v1-version",
    "requiredFiles": [
      "cppcheck.exe"
    ]
  },
  "lizard": {
    "version": "fixed-v1-version",
    "requiredFiles": [
      "lizard.exe"
    ]
  }
}
```

The exact versions should be filled after the tools are selected and placed locally.

## Build Script Changes

Update `scripts/build-release.ps1` so V1 release defaults to:

```powershell
dotnet publish "src/CodeCheck.Cli/CodeCheck.Cli.csproj" -c Release -r win-x64 --self-contained true -o release
dotnet publish "src/CodeCheck.Desktop/CodeCheck.Desktop.csproj" -c Release -r win-x64 --self-contained true -o release
```

Keep an opt-out switch only for development:

```powershell
-FrameworkDependent
```

Tool copying should fail the build unless `-SkipTools` is explicitly passed.

## Check Script Requirements

Update `scripts/check-release.ps1` to validate:

- `CodeCheck.Cli.exe`
- `CodeCheck.Desktop.exe`
- `configs/default-codecheck.json`
- all rule files
- `tools/llvm/bin/clang-tidy.exe`
- `tools/cppcheck/cppcheck.exe`
- `tools/lizard/lizard.exe`
- license directories
- runtime folders: `reports`, `baseline`, `suppressions`, `logs`, `temp`

It should also run:

```powershell
.\release\CodeCheck.Cli.exe --version
.\release\CodeCheck.Cli.exe validate --config .\release\configs\default-codecheck.json
```

## Runtime Path Resolution

The app should resolve tool paths relative to the release root, not the developer workspace.

Priority order:

1. Absolute path from config.
2. Path relative to current working directory.
3. Path relative to executable directory.
4. Tool locator fallback under `tools`.

This avoids failures after moving the `release` directory.

## Tool Health Checks

CLI `validate` and Desktop Home page should report:

- tool exists
- tool can execute `--version` or equivalent
- version matches manifest when known
- missing license files if configured as required

## Offline Policy

V1 must not:

- download tools at runtime
- require tools in PATH
- require global Python or pip for lizard
- require .NET runtime installed globally
- write outside the release/runtime directories except user-selected project/report paths and application settings

## Licensing

Before bundling:

- Capture upstream license files into `licenses`.
- Record exact version and source URL in the manifest or `licenses/THIRD_PARTY_NOTICES.md`.
- Do not modify third-party binaries.

## Acceptance Tests

1. Build release.
2. Copy release to a path with spaces.
3. Run CLI `--version`.
4. Run CLI `validate`.
5. Scan `samples/c-demo`.
6. Copy release to a Chinese path.
7. Repeat CLI `validate`.
8. Confirm no dependency on global `dotnet`, PATH tools, or administrator rights.

## Open Item

Tool versions are not selected yet. After Visual Studio/.NET are installed, choose fixed versions and update this plan with exact filenames and checksums.
