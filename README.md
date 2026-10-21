<p align="center">
  <img src="assets/mf_purge_icon.png" width="150" alt="MF Purge SketchUp icon">
</p>

# MF Purge SketchUp

[![Version](https://img.shields.io/badge/version-1.0.0-d9292f)](https://github.com/marcalfelipe/mf-purge-sketchup/releases)
[![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078d4)](#requirements)
[![License: MIT](https://img.shields.io/badge/source-MIT-34a853)](LICENSE)

A standalone Windows utility that removes unused components, groups, materials
and textures from SketchUp models **before opening SketchUp**. It is designed
for models that are too heavy to load reliably in the desktop application.

The original file is never overwritten. A new `*_PURGADO.skp` file is created
beside it and validated by reopening it through the SketchUp C API.

## Download

Open the [latest release](https://github.com/marcalfelipe/mf-purge-sketchup/releases/latest)
and choose either:

- `MF-Purge-SketchUp-v1.0.0-Windows-x64.exe` — portable single executable.
- `MF-Purge-SketchUp-v1.0.0-Windows-x64.zip` — the same portable app in a ZIP.

No installer is required. Windows SmartScreen may warn about an unsigned
community executable; review this repository and the published SHA-256 checksum
before running it.

## Features

- Opens `.skp` files directly without launching SketchUp.
- Drag and drop or standard file selection.
- Removes unused component/group definitions, materials and their textures.
- Shows a live log, progress percentage, elapsed time, CPU and memory use.
- Preserves definitions referenced by scenes and active section planes.
- Runs the native operation in an isolated worker process.
- Reopens and validates the generated model before reporting success.
- Includes English, Portuguese, Spanish, German, French, Russian, Vietnamese
  and Simplified Chinese. English is the default.
- Distributed as a self-contained Windows x64 executable.

## Requirements

- Windows 10 or Windows 11, 64-bit.
- Enough free disk space for the input model, temporary output and final copy.
- SketchUp itself is **not** required for the portable release.

The bundled v1.0.0 runtime is based on the SketchUp 2026 C API. Newer model
formats may require a future release using a newer SDK.

## Safe behavior

- The input model is opened read-only and never selected as the output path.
- Output is first written to a temporary partial file.
- A failed or interrupted purge does not replace the original.
- Existing purge outputs are not overwritten; numbered names are used instead.
- The final output must reopen successfully through the API.

Even with these safeguards, keep backups of important professional models.

## For developers

The complete C# interface/localization source and Python purge worker are in
this repository. See [Building from source](docs/BUILDING.md) for the toolchain,
SDK files and packaging command.

The repository intentionally excludes test models, local caches, compiled
workers and proprietary SketchUp SDK binaries. Release executables include the
runtime files required by the official C API release guidance.

Contributions and translations are welcome. Please read
[CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## Limitations

- Only Windows x64 is currently supported.
- Purging a very large model can temporarily require substantial RAM because
  the SketchUp API loads the model as a whole.
- The progress percentage represents processing stages and item scans; native
  file loading and saving do not expose byte-level progress.
- Always inspect the cleaned model before deleting any older copy.

## License and trademarks

Original source code is available under the [MIT License](LICENSE). SketchUp
SDK/runtime components are not covered by MIT and remain subject to Trimble's
terms; see [Third-party notices](THIRD_PARTY_NOTICES.md).

SketchUp is a trademark of Trimble Inc. MF Purge SketchUp is an independent
community project and is not endorsed by or affiliated with Trimble.

---

## Português

O MF Purge SketchUp limpa componentes, grupos, materiais e texturas não usados
diretamente de um arquivo `.skp`, antes de abrir o SketchUp. O original nunca é
sobrescrito: o programa cria e valida uma nova cópia `*_PURGADO.skp`.

Baixe o executável portátil ou o ZIP na página de
[Releases](https://github.com/marcalfelipe/mf-purge-sketchup/releases/latest).
O SketchUp não precisa estar instalado. O código-fonte completo está disponível
para estudo e colaboração, respeitando os termos separados da API da Trimble.
