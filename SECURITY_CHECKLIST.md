# SECURITY_CHECKLIST

## Build/Dependency hygiene
- [ ] Enable Dependabot/Renovate.
- [ ] Generate SBOM on every build.
- [ ] Fail CI on critical CVEs.
- [ ] Replace unmaintained graphics dependencies.

## Identity and access
- [ ] OIDC authentication.
- [ ] RBAC/ABAC authorization per session.
- [ ] Short-lived credentials and rotation.

## Transport and media security
- [ ] Harden DTLS-SRTP defaults.
- [ ] TURN with ephemeral credentials.
- [ ] Signaling channel TLS + replay protection.

## Host hardening
- [ ] Explicit capture consent UX.
- [ ] Always-on recording/capture indicator.
- [ ] Least-privilege runtime account.

## Logging/privacy/compliance
- [ ] Redact SDP/token/IP where required.
- [ ] Retention defaults to minimum.
- [ ] Data handling docs + incident response runbook.

## Verification
- [ ] Threat model reviewed quarterly.
- [ ] Automated SAST/DAST in CI.
- [ ] Annual independent penetration test.
