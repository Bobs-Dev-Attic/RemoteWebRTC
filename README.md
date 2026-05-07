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

## Validation performed
- `dotnet build -p:EnableWindowsTargeting=true` in `RemoteWebRTC.Host` succeeded in a prior environment snapshot; re-validation depends on local .NET SDK availability.
- Flutter tooling is not installed in this environment (`flutter`/`dart` not found), so runtime validation for the Flutter widget could not be executed here.


## Progress update (2026-05-07)

Implemented first-pass P1 stability work in the host:
- Replaced timer-driven sample emission with a bounded channel pipeline (`Channel<RawFrame>`) and dedicated producer/consumer loops.
- Added idempotent lifecycle controls and cancellation-aware shutdown (`SemaphoreSlim`, linked `CancellationTokenSource`, atomic started state).
- Added timestamped host log output for operational visibility without exposing SDP/ICE details.

See `TODO.md` for completed and in-progress checklist status and running log.
