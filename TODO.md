# RemoteWebRTC Prioritized TODO

## P0 — Critical security, correctness, and legal/privacy blockers

- [ ] **Implement authenticated signaling and SDP/ICE exchange** (currently absent): add a signaling service with short-lived auth tokens (OIDC/JWT), mTLS between host and signaling backend, and replay protection.
- [ ] **Replace `DummyNvencBridge` fake NAL output with real encoder pipeline** and verify RTP packetization correctness (SPS/PPS cadence, IDR strategy, MTU-safe fragmentation).
- [ ] **Pin and modernize dependencies**: move off preview `Microsoft.Windows.SDK.NET` and unmaintained SharpDX to supported D3D interop stack (e.g., Vortice.Windows / TerraFX / CsWin32-based interop) with SBOM + vulnerability scanning.
- [ ] **Enforce DTLS-SRTP hardening** in WebRTC stack: strong cipher suites only, certificate pinning strategy where possible, and explicit minimum protocol versions.
- [ ] **Design and publish privacy/data handling policy** for remote-screen content (PII exposure risk), retention policy (prefer zero retention), and explicit user consent UX.
- [ ] **Add abuse prevention controls**: session authorization boundaries, allowlist/denylist, rate limiting, lockout, and incident audit trail.

## P1 — Stability, memory, and performance

- [x] Replace `Timer` with backpressure-aware capture/encode loop (`Channel<T>` or bounded queue) to avoid frame pileup and GC pressure under load. ✅ Completed 2026-05-07 (bounded `Channel<RawFrame>` + `PeriodicTimer` dummy producer + single consumer encoder loop).
- [x] Add cancellation tokens and idempotent lifecycle management (`StartAsync`/`StopAsync`) to prevent double-start/double-dispose races. ✅ Completed 2026-05-07 (`SemaphoreSlim` lifecycle gate, linked CTS, atomic started flag, idempotent start/stop).
- [ ] Implement frame-drop policy (latest-frame wins), adaptive bitrate/framerate/resolution based on RTCP feedback.
- [ ] Add end-to-end metrics: encode latency, queue depth, dropped frames, RTT, packet loss, jitter, client render delay.
- [~] Add structured logging + redaction (no SDP secrets, no PII in logs) and centralized correlation IDs. 🚧 Partial 2026-05-07 (timestamped host log lines added; redaction and correlation IDs still pending).
- [ ] Validate GPU memory lifecycle for D3D resources and ensure zero-copy invariants are actually preserved.

## P2 — Product UX and operational maturity

- [ ] Implement host capture source picker with explicit consent prompts and persistent indicator that capture is active.
- [ ] Improve Flutter viewer UX states: loading, reconnecting, no-signal, permission denied, and actionable error banners.
- [ ] Add observability dashboards + SLOs (startup time, stream join success, stall rate, crash-free sessions).
- [ ] Build CI gates: unit tests, integration tests (host+client), dependency/license scans, static analysis, formatting checks.
- [ ] Add TURN (not only STUN) for NAT traversal reliability; evaluate managed TURN services for global performance.

## P3 — Strategic improvements and scale

- [ ] Evaluate codec roadmap (H.264 baseline/main vs AV1/H.265 where licensed/permitted).
- [ ] Add secure remote control channel (if product requires input control) with strict least-privilege and event signing.
- [ ] Introduce role-based tenant isolation and policy engine for enterprise deployments.
- [ ] Publish threat model + periodic penetration testing program + coordinated vulnerability disclosure process.


## Running change log

- 2026-05-07: Completed P1 queue/backpressure foundation in host runtime using bounded channel and latest-frame-wins behavior (drop oldest).
- 2026-05-07: Completed P1 lifecycle hardening with idempotent `StartAsync`/`StopAsync`, linked cancellation, and cleaner shutdown sequencing.
- 2026-05-07: Added baseline timestamped host logging to support future structured logging and correlation work.
