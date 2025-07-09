# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

<!-- Headers should be listed in this order: Added, Changed, Deprecated, Removed, Fixed, Security -->

## [0.2.1] - 2025-07-09

### Added
- Added scriptable object `XRCKey` to be used for storing third party API keys. All created keys should then be stored in a location ignored by git.

## [0.2.0] - 2024-10-31

### Changed
- Updated XR Interaction Toolkit to 3.0.
- Changed rendering pipeline for sample to URP.

## [0.1.4] - 2024-02-15

### Added
- Added `EditObjectProvider` for edit tools, such as XRC Scale Tool, XRC Mesh Tool, and XRC Color Tool.
- Added `IRunnable` and `IEditTool` interfaces.

## [0.1.3] - 2023-10-12

### Changed
- Updated documentation.
- Changed scale of PoseMarker prefab.

## [0.1.2] - 2023-07-26

### Added
- Added dependency on XR Interaction Toolkit. Samples currently depend on XRI, and runtime scripts will also have a dependency on XRI.

### Changed 
- Changed sample folder name and sample scene name.
- Removed Event System from sample scene and rearranged scene hierarchy.

## [0.1.1] - 2023-07-24

### Added 
- Added PoseMarker prefab from OpenXR Plugin. https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.8/manual/index.html .


## [0.1.0] - 2023-07-20

### Added 
- Basic Patterns, including Command pattern, Singleton pattern, and Observer pattern.