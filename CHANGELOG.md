# Changelog

All notable changes to this project will be documented in this file.

## [1.1.0] - 2026-01-29

### Added
- **Advanced Table System**: Complete overhaul of table rendering engine.
- **Text Wrapping**: Cells now support automatic text wrapping with dynamic row height calculation.
- **Column Spanning**: Added `colspan` support for merging cells.
- **Advanced Styling**: Added customizable border colors (`borderColor`), line styles (`Solid`/`Dashed`), and per-cell background colors (`backgroundColor`).
- **Granular Positioning**: Added `verticalAlignment` (Top/Middle/Bottom) and precise `offsetX`/`offsetY` controls for cell text.
- **New Inspector UI**: Added "Advanced Cells" mode to the Table inspector for deep customization.

### Fixed
- Fixed background color rendering for header rows to properly respect per-cell overrides.
- Fixed issue where shapes were invalidly drawn inside text blocks in some PDF viewers.
- Fixed initialization logic for new table rows/columns to prevent "invisible" cells (default `colspan` is now 1).

## [1.0.0] - 2026-01-28

### Added
- Initial release as a Unity Package.
- Core PDF generation utility (`PdfGenerator`).
- High-level modular report API (`PdfReport`).
- Editor window for quick exporting (`PdfExporterWindow`).
- Assembly definitions for Runtime and Editor.
- Documentation and package manifest.
