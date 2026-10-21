# MF Purge SketchUp v1.0.0

First public community release.

## Download choices

- The `.exe` is the portable, self-contained Windows x64 application.
- The `.zip` contains the same executable plus the license, notices and
  checksum file.
- Developers can clone the repository or use GitHub's automatically generated
  source archives.

## Verification

Compare the downloaded executable against `SHA256SUMS.txt`:

```powershell
Get-FileHash '.\MF-Purge-SketchUp-v1.0.0-Windows-x64.exe' -Algorithm SHA256
```

## Important

The application creates a separate purged copy and does not overwrite the
input model. Keep backups and inspect the generated model before removing any
older copy. Windows may show a SmartScreen warning because this community build
is not code-signed.
