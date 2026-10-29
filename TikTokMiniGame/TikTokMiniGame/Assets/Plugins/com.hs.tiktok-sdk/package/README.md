# HS TikTok MiniGame SDK

Complete Unity SDK for TikTok MiniGame development — Authentication, In-App Purchases, Cloud Sync, and Anti-Cheat.

## Features

- **Authentication** — Login via TikTok auth code with automatic Editor test mode
- **In-App Purchases** — Server-authoritative IAP with TikTok payment UI integration
- **Cloud Sync** — Offline-first background save sync (configurable interval)
- **Crash Recovery** — Automatic recovery of paid-but-undelivered purchases
- **Anti-Cheat** — AES-256 save encryption + HMAC-SHA256 tamper detection
- **Zero Coupling** — Interface-based integration, no game-code dependencies

## Installation

### Via Unity Package Manager (Git URL)

```
Window > Package Manager > + > Add package from git URL...
→ https://github.com/your-org/hs-tiktok-sdk.git
```

### Manual

Copy the `HS.TikTokSDK` folder into your Unity project's `Assets/` directory.

## Quick Start

1. **Menu:** `HS TikTok SDK > Setup SDK in Scene`
2. **Configure:** Set Backend URL and Game ID in the Inspector
3. **Implement:** `ITikTokSyncProvider` and `ITikTokRewardHandler` in your game code
4. **Register:** Call `TikTokSDK.Instance.RegisterSyncProvider()` and `RegisterRewardHandler()`

See `Documentation~/integration_guide.md` for the full guide.

## Requirements

- Unity 2021.3+
- Node.js backend (see backend API contract in docs)
- TikTok Developer Portal app registration
