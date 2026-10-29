# Changelog

All notable changes to the **HS TikTok MiniGame SDK** will be documented in this file.

## [1.0.4] - 2026-05-21

### Fixed
- **Stability:** Added `15` second timeout (`req.timeout = 15;`) to all SDK HTTP requests to prevent indefinite hangs on poor networks.
- **WebGL:** Wrapped JavaScript SDK `pay` calls in `try/catch` blocks so synchronous browser exceptions don't permanently hang the Unity purchase state.
- **Auth:** Backend now properly stringifies `profile_data` so Unity's `JsonUtility` can correctly map it into the response string.

## [1.0.3] - 2026-05-21

### Added
- **`TikTokIAP.GetOrders(callback, status?)`** — public API for games to manually query player orders from the backend. Supports optional status filter (`PAID`, `REFUND_PENDING`, `REFUND_APPROVED`, etc.).
- **`TikTokOrder`** — new public model exposing order data (order_id, product_id, status, amount_beans, refund info, rewards). Includes helper properties: `IsRefunded`, `IsRefundPending`, `IsTestOrder`.
- **`SampleOrdersHandler.cs`** — sample showing manual order querying for all orders, refunded orders, purchase history, and pending refunds.
- **Backend `POST /orders`** — new player-facing endpoint for querying own orders with optional status filter. Scoped by `game_id`.

## [1.0.2] - 2026-05-20

### Fixed
- **Auth: Login response handling** — SDK now processes `pending_rewards` and `refund_orders` inline from the login response instead of dropping them. Eliminates redundant `POST /pending_rewards` HTTP call after login.
- **Auth: Refund delivery** — `refund_orders` from the login response are now delivered to the game via `OnRefundNotified` event and `ITikTokRewardHandler.OnRefundNotified()`. Previously these were silently lost.

### Added
- `OnRefundNotified` event on `TikTokIAP` — fired when the backend reports a refund.
- `ITikTokRefundHandler` — new **optional** interface for refund notifications. Games opt-in by implementing it alongside `ITikTokRewardHandler`. Games that don't implement it still work fine.
- `RefundOrder` model for refund data deserialization.
- `DeliverPendingRewards()` and `DeliverRefundOrders()` internal methods on `TikTokIAP` for inline delivery from login.

## [1.0.1] - 2026-05-19

### Fixed
- **IAP: HTTP error body parsing** — `CreateOrderRoutine` now reads the JSON response body on HTTP 4xx/5xx errors (e.g. 404 "Product not found or inactive") instead of only reporting the generic `req.error` string. `OnPurchaseFailed` now delivers the backend's specific error message.
- **Build: DLL compilation** — `build-dll.sh` now references the TikTok `ttsdk.dll` to resolve `TTSDK` namespace dependencies in Mission scripts.

## [1.0.0] - 2026-05-14

### Added
- Core SDK singleton (`TikTokSDK`) with Inspector-configurable backend URL, Game ID, and Test Mode.
- Authentication module (`TikTokAuth`) — login via TikTok auth code or Editor mock login.
- In-App Purchase module (`TikTokIAP`) — order creation, TikTok payment UI, backend polling, crash recovery.
- Cloud Sync module (`TikTokSync`) — offline-first background sync with configurable interval.
- Security module (`TikTokSecurity`) — AES-256 encryption with random IV, HMAC-SHA256 hash verification.
- Interface-based integration — `ITikTokSyncProvider` and `ITikTokRewardHandler` for zero-coupling with game code.
- Editor tools — one-click SDK setup, automatic HTTP dev settings.
- Assembly Definitions for compilation isolation.
- UPM package support (`package.json`).
- Sample integration code.
