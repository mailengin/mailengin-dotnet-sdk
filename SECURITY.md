# MailEngin SDK Security Policy

## Supported Versions

Security fixes are released for the latest published SDK version. Upgrade to the newest patch before reporting an issue that may already be fixed.

## Report a Vulnerability

Do not open a public GitHub issue or discussion for a suspected vulnerability. Email [security@mailengin.app](mailto:security@mailengin.app) with the subject `MailEngin SDK security report`.

Include:

- Affected SDK and version
- Runtime and operating-system versions
- Vulnerability impact and realistic attack scenario
- Minimal reproduction steps or proof of concept
- Whether the issue is already public or actively exploited
- A safe way to contact you for follow-up

Never send an active API key, customer address, email content, credential, private repository URL, or production log. Replace sensitive values with clearly marked test data.

## What Happens Next

MailEngin will acknowledge the report, validate the issue, assess affected versions, and coordinate remediation and disclosure with the reporter. Please allow time for a fix before publishing technical details.

When appropriate, MailEngin will publish a corrected release and security advisory describing impact, affected versions, upgrade guidance, and mitigations.

## Scope

This policy covers the SDK source, published package, build and release configuration, authentication handling, request serialization, response parsing, and documented integration patterns.

MailEngin account or API-service vulnerabilities should still be sent to the same private security address, with the affected service identified clearly.

## Research Guidelines

- Use test accounts and data you control.
- Avoid sending unsolicited email or affecting other customers.
- Do not access, modify, retain, or disclose data belonging to others.
- Stop testing and report immediately if sensitive data is encountered.
- Do not use denial-of-service, social-engineering, or destructive techniques.
