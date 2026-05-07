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
- `dotnet build -p:EnableWindowsTargeting=true` in `RemoteWebRTC.Host` succeeded.
- Flutter tooling is not installed in this environment (`flutter`/`dart` not found), so runtime validation for the Flutter widget could not be executed here.
