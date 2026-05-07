# QUICK_CONTEXT (for low-token coding assistants)

## What this repo currently is
- MVP plumbing only; not production-ready.
- Host: skeleton Windows capture-to-WebRTC path with fake encoded bytes.
- Client: basic Flutter widget that renders a provided remote stream.

## Where to look first
1. `RemoteWebRTC.Host/Program.cs` — capture + peer connection lifecycle + dummy encoder bridge.
2. `RemoteWebRTC.Client/flutter/lib/remote_stream_view.dart` — renderer lifecycle.
3. `TODO.md` — prioritized implementation roadmap.
4. `REVIEW.md` — multidisciplinary risk and improvement analysis.

## Critical constraints to remember
- No authenticated signaling flow exists yet.
- No real GPU capture device or capture item creation is implemented.
- No real NVENC integration is implemented.

## Next best tasks (highest leverage)
1. Implement signaling/auth/session model.
2. Replace dummy encoder with real pipeline + tests.
3. Add TURN + observability + CI security gates.
