# RemoteWebRTC

## MVP plumbing added

### Windows Host (`RemoteWebRTC.Host/`)
- .NET 8 Windows console host scaffold.
- `Windows.Graphics.Capture` frame loop wired (`Direct3D11CaptureFramePool`).
- Captured `IDirect3DSurface` passed to a `DummyNvencBridge` that represents zero-copy DX texture -> NVENC handoff.
- Encoded sample callback is wired to `SIPSorcery.Net.RTCPeerConnection.SendVideo` using an H.264 video track.
- STUN fallback is configured with `stun:stun.l.google.com:19302`.

### Flutter Client (`RemoteWebRTC.Client/flutter/`)
- `RemoteStreamView` widget initializes `RTCVideoRenderer` and binds a remote `MediaStream`.
- Widget updates renderer source on stream changes and disposes renderer correctly.
- Viewer now supports explicit UX states and overlays for loading, reconnecting, no signal, permission denied, and generic errors.

## Validation performed
- `dotnet build -p:EnableWindowsTargeting=true` in `RemoteWebRTC.Host` succeeded in a prior environment snapshot; re-validation depends on local .NET SDK availability.
- Flutter tooling is not installed in this environment (`flutter`/`dart` not found), so runtime validation for the Flutter widget could not be executed here.


## Progress update (2026-05-07)

Implemented first-pass P1 stability work in the host:
- Replaced timer-driven sample emission with a bounded channel pipeline (`Channel<RawFrame>`) and dedicated producer/consumer loops.
- Added idempotent lifecycle controls and cancellation-aware shutdown (`SemaphoreSlim`, linked `CancellationTokenSource`, atomic started state).
- Added timestamped host log output for operational visibility without exposing SDP/ICE details.

Implemented P2/P3 documentation and UX updates:
- Added user-facing stream-state overlays in Flutter for loading/reconnecting/no-signal/permission-denied/error with optional retry affordance.
- Documented initial security operations expectations for coordinated vulnerability disclosure and regular penetration testing cadence (formal threat model still pending).

## Security operations baseline (P3 in progress)

This MVP now tracks an initial security operations baseline pending formal policy docs:
- **Coordinated vulnerability disclosure**: accept vulnerability reports via a dedicated security contact channel, acknowledge receipt quickly, triage severity, and communicate remediation timelines.
- **Penetration testing cadence**: plan for recurring external penetration testing and post-remediation validation before enterprise production rollout.
- **Threat model publication**: create and publish a versioned threat model covering host, signaling, and client trust boundaries and abuse cases.

See `TODO.md` for completed and in-progress checklist status and running log.
