# RemoteWebRTC Engineering Review (MVP)

## 1) Executive summary

This repository is a useful plumbing prototype, but it is **not production-safe yet**. The current implementation sends fake H.264 payload bytes and lacks signaling/auth/session-control components, making it unsuitable for real-world remote streaming and security-sensitive environments.

Primary risks:
- Missing authenticated signaling and session governance.
- Placeholder capture/encode path with no verified media correctness.
- Potential lifecycle/concurrency issues and weak observability.
- Limited UX affordances for consent, errors, and trust.

---

## 2) Code-level findings

## Host (`RemoteWebRTC.Host/Program.cs`)

### Security and protocol
- `RTCPeerConnection` exists, but there is no visible signaling handshake or access control model.
- STUN is configured, but TURN fallback and hardened connectivity policy are absent.
- Fake NAL emission (`DummyNvencBridge`) creates false confidence about media path readiness.

### Memory and lifecycle
- `Timer`-driven dummy loop may hide real backpressure requirements; production needs bounded queues and explicit drop policies.
- `StartAsync`/`StopAsync` are effectively synchronous and do not guard against re-entry race conditions.
- Event subscription is detached in `StopInternal`, good baseline, but disposal order and nulling references can be tightened.

### Performance and optimization
- Fixed 33ms interval and fixed dummy RTP duration ignore actual capture timestamps.
- No adaptation using network feedback (loss/RTT/jitter), no encoder tuning path.
- No instrumentation for queue/latency/drops.

### Dependency and platform risk
- `Microsoft.Windows.SDK.NET` preview package and SharpDX usage raise long-term support concerns.
- Capture factories are TODO/null; current behavior always falls back to dummy data.

## Client (`RemoteWebRTC.Client/flutter/lib/remote_stream_view.dart`)

### UX
- Renderer initialization and disposal are clean for MVP.
- Missing user-facing states: initializing, disconnected, failed stream, retry action.

### Robustness
- Stream swap logic compares by `id`; good basic check, but no handling of ended tracks or muted/paused tracks.
- Aspect ratio is fixed to 16:9; can distort non-16:9 capture sources.

---

## 3) Security/penetration tester perspective

### Attack surface highlights
- **Session hijack risk** if signaling/auth are not strongly bound to identity and authorization scope.
- **Credential leakage risk** if SDP/candidates/tokens are logged in plaintext.
- **Unauthorized capture risk** without explicit capture consent UX and active-capture indicators.
- **NAT traversal dependency risk** without TURN hardening and rate controls.

### Recommended controls
1. OIDC-backed identity with short-lived access tokens + refresh rotation.
2. Strict authorization (who can initiate/view/control each host).
3. End-to-end audit logs with tamper resistance.
4. Secrets management (no static credentials), key rotation, SBOM + CVE gates.
5. Regular threat modeling and red-team exercises.

---

## 4) UX design perspective

- Add a **trust-focused onboarding**: “what is captured, when, and why”.
- Add clear status model: Connecting → Live → Degraded → Reconnecting → Stopped.
- Provide graceful error language and one-click remediation.
- Add accessibility basics (high contrast, keyboard nav, screen-reader labels).

---

## 5) Founder/executive/market perspective

- Reliability and trust are the product moat in remote-streaming tools; prioritize security posture and observability before feature breadth.
- Define ICP early (consumer support, enterprise IT, medical, education) because compliance and controls differ dramatically.
- Consider managed TURN + global edge footprint to reduce operational drag.

---

## 6) Legal/privacy/ethics perspective

- Establish lawful basis and explicit consent workflows for screen capture.
- Minimize data collection by default; no recording unless explicitly enabled.
- Provide data subject rights workflow where applicable (access/deletion).
- Publish transparent acceptable-use policy to reduce abuse.

---

## 7) Suggested architecture upgrades

- Add dedicated signaling service (WebSocket/gRPC) with authN/authZ middleware.
- Add TURN infrastructure (self-hosted coturn or managed provider) with ephemeral credentials.
- Move encode/capture to bounded producer-consumer pipeline.
- Add telemetry stack (OpenTelemetry traces/metrics/logs).

