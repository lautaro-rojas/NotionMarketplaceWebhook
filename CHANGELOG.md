# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Differentiation between customer types (normal or corporate) based on email

### Changed

### Removed

<!--
## [v0.2.X] - 2025-10-2X
This section documents all changes prepared for version v0.2.X.

### Added

### Fixed

### Changed
-->

## [v1.1.1] - 2025-01-19
This section documents all changes prepared for version v1.1.1.

### Added

### Fixed

### Changed

### Removed
- Removed root folder

## [v1.1.0] - 2025-01-19
This section documents all changes prepared for version v1.1.0.

### Added
- Asynchronous task handling (Task.Run) in the Webhook Controller to prevent timeouts.
- UseStaticFiles middleware in Program.cs to serve public assets like images.
- HTML email signature template.
- [JsonPropertyName] attributes to the NotionPayload model for precise JSON mapping.
- Helper logic to convert Unix Timestamp (long) to DateTime objects.

### Fixed
- Critical: HTTP 502 (Bad Gateway) / Timeout error by decoupling the business logic execution from the HTTP response.
- Critical: HTTP 400 (Bad Request) error caused by incorrect data type binding (changed time field from string to long).

### Changed
- Updated NotionPayload.Time property type from string to long to correctly deserialize Notion's Unix timestamp format.
- Refactored the Webhook endpoint to return 200 OK immediately (Fire-and-Forget pattern).

### Removed
- Blocking await calls on the main thread during webhook reception.

## [v1.0.0] - 2025-01-15
This section documents all changes prepared for version v1.0.0.

### Added
- Attributes added to NotionPayload (owner's email).
- New features implemented (general).
- Email sending to clients completed (release 1.0.0).

### Fixed
- Empty mail sending arrangement

### Changed
- Email signature details updated.
- Email sending for production was commented out earlier and later completed.
- Miscellaneous server testing commits.

### Removed
- PNG URL in the sign.

## [v0.1.0] - 2025-01-14
This section documents all changes prepared for version v0.1.0.

### Added
- **IBackgroundTaskQueue.cs:** interface for queuing background work items.
- **BackgroundTaskQueue.cs:** Channel-based bounded queue implementation.
- **QueuedHostedService.cs:** BackgroundService that dequeues and executes queued work items.

### Fixed
- **WebhookController.cs:** prevented webhook timeouts by moving long-running work (email sending) to a background queue so the endpoint can respond immediately with 200 OK.

### Changed
- **WebhookController.cs:** Injects the background queue and enqueues email-sending tasks instead of executing them synchronously.
Program.cs: registers BackgroundTaskQueue and QueuedHostedService in DI.

### Removed

[unreleased]: https://github.com/lautaro-rojas/NotionMarketplaceWebhook

[v1.1.1]: https://github.com/lautaro-rojas/NotionMarketplaceWebhook/compare/v1.1.0...v1.1.1
[v1.1.0]: https://github.com/lautaro-rojas/NotionMarketplaceWebhook/compare/v1.0.0...v1.1.0
[v1.0.0]: https://github.com/lautaro-rojas/NotionMarketplaceWebhook/compare/v0.1.0...v1.0.0
[v0.1.0]: https://github.com/lautaro-rojas/NotionMarketplaceWebhook/releases/tag/v0.1.0