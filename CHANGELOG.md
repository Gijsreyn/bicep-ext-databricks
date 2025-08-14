<!-- markdownlint-disable MD024 -->
# Changelog

## [0.1.7] - 2025-08-14

### Changed

- Restructured the whole solution to leverage interfaces.

## [0.1.6] - 2025-08-10

### Added

- Added new prerelease version for `CatalogHandler` to create a Unity Catalog.

## [0.1.5] - 2025-08-08

### Added

- Added new `GitCredentialHandler` to create a Git credential for repository.

## [0.1.4] - 2025-08-08

### Added

- Added new `RepoHandler` to create repository.

## [0.1.3] - 2025-08-07

### Added

- Add tracing to `ClusterHandler`.

### Fixed

- Improved error handling on `ClusterHandler`.

## [0.1.2] - 2025-08-07

### Fixed

- Update `ClusterHandler` to support update scenario.

## [0.1.1] - 2025-08-07

### Added

- Added `ClusterHandler` for managing Databricks clusters via the `Cluster` Bicep
  resource.

## [0.1.0] - 2025-08-05

### Added

- Initial release.
- Added `SecretHandler` for managing Databricks secrets via the `Secret` Bicep resource.
- Added `WorkspaceHandler` for managing Databricks workspaces via the `Workspace`
  Bicep resource.
