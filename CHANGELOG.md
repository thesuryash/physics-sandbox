# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-07-01

### Added
- Initial release as a Unity Package Manager (UPM) package installable via git URL.
- Runtime systems: mass & bodies, aerodynamic drag with baked directional lookup,
  free body diagrams, springs, inclined planes, 3D vector fields (Burst), charge
  trail visualization, paths, and JSON-driven environment material config.
- Editor tooling: no-code Physics Sandbox dashboard, scene import/export,
  lesson importer, and component editors.
- `Demo Scenes` sample (importable from the Package Manager) with example lesson
  scenes and their supporting assets.
- Optional energy-graph support gated behind the `XCHARTS_PRESENT` define, enabled
  automatically when the XCharts package (`com.monitor1394.xcharts`) is installed.

### Changed
- Physics material config (`physics_config.json`) now loads from `Resources`
  (`Resources.Load<TextAsset>("physics_config")`) instead of `StreamingAssets`,
  so it works when installed as a package.
