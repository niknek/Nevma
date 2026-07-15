#!/usr/bin/env python3
import json
import ssl
import sys
import urllib.request
from pathlib import Path


CONTRACTS = {
    "gateway": ("http://localhost:5033", {
        "/api/home": {"get"},
    }),
    "identity": ("https://localhost:7293", {
        "/api/auth/register": {"post"},
        "/api/auth/mfa": {"get"},
        "/api/auth/mfa/setup": {"post"},
        "/api/auth/mfa/enable": {"post"},
        "/api/auth/mfa/recovery-codes": {"post"},
        "/api/auth/mfa/disable": {"post"},
        "/api/auth/security-events": {"get"},
        "/api/auth/sessions": {"get"},
        "/api/auth/sessions/{id}": {"delete"},
        "/api/connections": {"get", "post"},
        "/api/users/me": {"get", "put"},
        "/connect/authorize": {"get"},
    }),
    "planning": ("http://localhost:5276", {
        "/api/tasks": {"get", "post"},
        "/api/tasks/{id}/complete": {"post"},
        "/api/calendar": {"get"},
        "/api/meeting-invitations": {"post"},
        "/api/meeting-invitations/{id}/accept": {"post"},
        "/api/meeting-invitations/{id}/counter-propose": {"post"},
    }),
    "messaging": ("http://localhost:5085", {
        "/api/conversations": {"get", "post"},
        "/api/conversations/{conversationId}/messages": {"get", "post"},
        "/api/conversations/{conversationId}/messages/{messageId}/receipts": {"post"},
        "/api/conversations/presence/{userId}": {"get"},
    }),
    "notifications": ("http://localhost:5095", {
        "/api/notifications": {"get"},
        "/api/notifications/{id}/read": {"post"},
        "/api/notifications/preferences": {"get", "put"},
        "/api/push-devices": {"post"},
    }),
    "files": ("http://localhost:5106", {
        "/api/files": {"post"},
        "/api/files/{id}": {"get", "delete"},
        "/api/files/{id}/download-url": {"post"},
        "/api/files/{id}/grants": {"post"},
    }),
    "commands": ("http://localhost:5116", {
        "/api/commands/preview": {"post"},
        "/api/commands/transcribe": {"post"},
        "/api/commands/{id}/confirm": {"post"},
        "/api/commands/{id}/undo": {"post"},
    }),
}


def main() -> int:
    output = Path(".ci-openapi")
    output.mkdir(exist_ok=True)
    tls = ssl.create_default_context()
    tls.check_hostname = False
    tls.verify_mode = ssl.CERT_NONE
    failures = []

    for service, (base_url, required_paths) in CONTRACTS.items():
        with urllib.request.urlopen(f"{base_url}/openapi/v1.json", context=tls, timeout=10) as response:
            document = json.load(response)
        (output / f"{service}.json").write_text(
            json.dumps(document, indent=2, sort_keys=True),
            encoding="utf-8")

        paths = document.get("paths", {})
        for path, required_methods in required_paths.items():
            actual_methods = {method.lower() for method in paths.get(path, {})}
            missing_methods = required_methods - actual_methods
            if missing_methods:
                failures.append(
                    f"{service}: {path} is missing {', '.join(sorted(missing_methods))}")

    if failures:
        print("API contract verification failed:", file=sys.stderr)
        for failure in failures:
            print(f"- {failure}", file=sys.stderr)
        return 1

    print(f"Verified {len(CONTRACTS)} OpenAPI contracts.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
