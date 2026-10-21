# Building from source

## Requirements

- Windows x64
- .NET 8 SDK
- Python 3.11 or newer
- Official SketchUp C SDK/runtime files obtained under the applicable Trimble
  API terms

The required runtime folder must contain:

- `SketchUpAPI.dll`
- `SketchUpCommonPreferences.dll`
- `MSVCP140.dll`
- `MSVCP140_CODECVT_IDS.dll`
- `VCRUNTIME140.dll`
- `VCRUNTIME140_1.dll`

These proprietary runtime files are deliberately not stored in the source
repository.

## Build

From a PowerShell prompt at the repository root:

```powershell
.\scripts\Build-Release.ps1 -SketchUpRuntimeDirectory "C:\path\to\runtime"
```

The script packages the Python worker, embeds the runtime, publishes a
self-contained single-file Windows executable, and writes its SHA-256 checksum.
Build outputs are created in `dist\release`.

## Architecture

The WinForms process owns the user interface, localization, progress reporting
and resource monitoring. Purge work runs in a separate Python process through
the SketchUp C API. Keeping the native API isolated prevents a native crash or
out-of-memory failure from taking down the UI or modifying the input model.

The worker writes to a temporary partial output. The application accepts the
result only after it can reopen the output through the same API and confirm it
is distinct from the input file.
