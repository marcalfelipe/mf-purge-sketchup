# MF Purge SketchUp v1.1.0

This release brings the real Windows interface in line with the visual preview
published in the project README.

## What changed

- New landscape layout with rounded header and content cards.
- High-resolution app artwork in the window header.
- Large red purge action, smooth progress bar and clearer percentage display.
- Dedicated processing-log card.
- Live CPU and RAM percentage bars.
- Existing purge behavior, output validation and eight languages are preserved.

## Download choices

- The `.exe` is the portable, self-contained Windows x64 application.
- The `.zip` contains the same executable plus the license, notices and
  checksum file.
- Developers can clone the repository or use GitHub's automatically generated
  source archives.

## Verification

Compare the downloaded executable against `SHA256SUMS.txt`:

```powershell
Get-FileHash '.\MF-Purge-SketchUp-v1.1.0-Windows-x64.exe' -Algorithm SHA256
```

## Important

The application creates a separate purged copy and does not overwrite the
input model. Keep backups and inspect the generated model before removing any
older copy. Windows may show a SmartScreen warning because this community build
is not code-signed.
