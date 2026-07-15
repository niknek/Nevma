# Nevma security baseline

This document tracks the backend against the OWASP Application Security Verification Standard
(ASVS) 5.0 categories. It is an engineering baseline, not a certification or a claim of full
ASVS compliance. Every release should keep the automated evidence below green and review the
remaining operational controls before production deployment.

## Implemented and automatically checked

| Area | Nevma control | Evidence |
| --- | --- | --- |
| Authentication | Unique accounts, strong password policy, lockout, OAuth authorization code with PKCE, short access tokens, reference refresh tokens | Identity integration and end-to-end tests |
| Multi-factor authentication | TOTP setup and verification, one-time recovery codes, MFA login challenge, token revocation after MFA changes | `MultiFactorAuthenticationTests` and OpenAPI contract gate |
| Session management | Non-persistent secure host cookies, explicit device sessions, per-session and global revocation | Identity and end-to-end tests |
| Access control | Bearer authentication by default on user resources and ownership checks in service application layers | Service tests and live workflow test |
| Input and API safety | Request-size limits, upload allow-list, fail-closed malware scanning, bounded image decoding and metadata-free normalization, command confirmation, idempotency and concurrency controls | Unit, contract, load and end-to-end tests |
| Cryptography | External production signing/encryption certificates, encrypted data-protection keys, protected push tokens | Production configuration validation and startup failure on missing certificates |
| Logging and monitoring | Correlation IDs, OpenTelemetry, user-visible security events, Prometheus alerts and Grafana dashboard | Service-default tests and observability configuration |
| HTTP security | HSTS outside Development, restrictive CSP, no sniffing, frame denial, referrer and permissions policies | API integration tests |
| Supply chain | Locked action revisions, NuGet vulnerability audit, container vulnerability scan, Dependabot and full-history secret scan | Backend CI |

## Required production operations

- Store database, broker, Redis, SMTP, Grafana and certificate passwords in the deployment secret
  manager; never copy `.env.production.example` values into a live environment.
- Terminate TLS only at an approved proxy, preserve the original scheme securely, and restrict all
  database, broker, telemetry and dashboard ports to private networks.
- Rotate signing, encryption and data-protection certificates with an overlap period that keeps
  existing tokens and protected data readable until their documented expiry.
- Send alerts to an owned on-call destination and define response procedures for account takeover,
  leaked credentials, repeated MFA failures and opened dependency circuit breakers.
- Back up PostgreSQL and object storage, test restores, define security-event retention, and document
  deletion/export handling for privacy requests.
- Run an authenticated dynamic security assessment against staging before a public release and
  repeat it after material authentication, authorization or upload changes.

## Remaining verification work

- Complete a route-by-route authorization matrix for every role and resource relationship.
- Add an independent penetration test and ASVS control review before claiming a verification level.
- Keep complex document formats outside the upload allow-list until they have a dedicated sandboxed
  parser and content-disarm process.
- Validate proxy trust configuration and certificate rotation in the real hosting environment.
- Define organization-level incident response, audit retention and privacy policies.
