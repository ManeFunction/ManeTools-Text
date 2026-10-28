# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.0.0-preview.5] - 2026-10-28
### Added
- Added dependency to [ManeTools-Unity](https://github.com/ManeFunction/ManeTools-Unity.git).

### Changed
- `OptionalSizeField` replaced with universal `LimitedIntField` from the dependent package.
- Outline and shadow are now two `bool` fields with `FeatureToggle`, and their inspector blocks are built from it. `Effect`, `IsOutlineActive` and the rest of the public API work as before. Labels saved with the old effects mask are converted on load.

### Fixed
- A label decided which material it could destroy from the material's name. It now keeps its own per-instance material and destroys only that. This also fixes an opaque label keeping the previous font's material after a font change.
- The text shader was left out of player builds, because only `Shader.Find` referenced it, so labels rendered nothing. It now lives in the package's `Resources` folder.

## [2.0.0-preview.4] - 2026-09-02
### Added
- Added missed entries for the latest releases.

## [2.0.0-preview.3] - 2026-09-02
### Changed
- Removed auto-referencing from the Editor asmdef.

## [2.0.0-preview.2]
### Added
- Added 'Usage' block to the `README` file.

## [2.0.0-preview.1] - 2026-08-28

Initial release of the extracted Unity tool for in-game text rendering. It was moved and refactored out of the legacy Unity-coupled module. Versioning starts at 2.0.0 to mark that split; this is not a new project, it is just a fresh start.

Changes below are compared with the old version form the legacy `ManeTools`.

### Added
- Added public `CharacterSize` and `EffectsShiftZ` properties.
- Added a scrollbar to the text field and capped it at 10 rows.
- Added `Undefined` display for Max Width and Max Height when the value is 0.
- Added NUnit tests for wrapping and public property setters.

### Changed
- Editor UI was changed from `IMGUI` to `UI Toolkit`.
- Namespace was changed to `Mane.Unity.Text`.
- Code was refactored.

### Fixed
- Fixed an off-by-one in max-height line clipping.
- Fixed `Size` returning a stale value after layout properties change.
