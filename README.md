# Nevma

Voice-first planning and collaboration platform.

## Services

- `Nevma.Gateway`: the single mobile API entry point and reverse proxy.
- `Nevma.Identity.Api`: users, devices, sessions, contacts, and permissions.
- `Nevma.Planning.Api`: calendar, meeting invitations, tasks, and reminders.
- `Nevma.Messaging.Api`: conversations and SignalR realtime messaging.
- `Nevma.Notifications.Api`: notification inbox, protected push devices, and delivery workers.
- `Nevma.Files.Api`: private file storage, validation, scanning, and access grants.
- `Nevma.Commands.Api`: voice/text command previews, confirmation, execution, audit, and undo.
- `Nevma.Contracts`: versioned integration contracts shared between services.
- `Nevma.ServiceDefaults`: shared HTTP error handling, correlation IDs, security headers,
  health checks, and reusable result primitives. It contains no business rules.

The authenticated Gateway endpoint `GET /api/home` composes the mobile Home view from service
APIs in parallel. It returns the next task, urgent tasks, upcoming calendar events, pending
connections, and unread notifications without reading another service's database.

## Local development

The repository is pinned to .NET SDK 10.0.301.

Docker Desktop provides PostgreSQL, RabbitMQ, Redis, and Jaeger for local development. The setup
script creates an ignored `.env` with random credentials, starts the containers, builds the
solution, and applies all six service migrations:

```powershell
.\scripts\setup-local.ps1
.\scripts\start-local.ps1 -SkipSetup
```

The Gateway is then available at `http://localhost:5033`. RabbitMQ management is at
`http://localhost:15672` and Jaeger tracing is at `http://localhost:16686`. The RabbitMQ username
and password are stored in the local `.env` file. Stop the APIs, or the APIs and containers, with:

```powershell
.\scripts\stop-local.ps1
.\scripts\stop-local.ps1 -Infrastructure
```

Docker ports bind to `127.0.0.1` only. Messaging uses Redis for the authenticated SignalR
backplane and shared presence state; PostgreSQL remains the source of truth for conversations
and messages. Connected clients should invoke the `Heartbeat` hub method at least once per minute.

Run the live first-milestone test against the started backend with:

```powershell
$env:NEVMA_RUN_E2E="true"
dotnet test tests/Nevma.EndToEndTests
```

It exercises registration, OAuth authorization code with PKCE, contacts, meeting acceptance,
calendar persistence, RabbitMQ notifications, and conversation integration. The test is skipped
in the normal unit-test run when `NEVMA_RUN_E2E` is not enabled.

```powershell
dotnet restore Nevma.slnx
dotnet build Nevma.slnx
dotnet test Nevma.slnx
```

Run the APIs in separate terminals:

```powershell
dotnet run --project src/Services/Identity/Nevma.Identity.Api
dotnet run --project src/Services/Planning/Nevma.Planning.Api
dotnet run --project src/Services/Messaging/Nevma.Messaging.Api
dotnet run --project src/Services/Notifications/Nevma.Notifications.Api
dotnet run --project src/Services/Files/Nevma.Files.Api
dotnet run --project src/Services/Commands/Nevma.Commands.Api
dotnet run --project src/Gateway/Nevma.Gateway
```

The Identity service now owns a PostgreSQL schema named `identity`. Supply its password and
other environment-specific values outside source control, for example:

```powershell
$env:ConnectionStrings__IdentityDatabase="Host=localhost;Port=5432;Database=nevma_identity;Username=nevma_identity;Password=<secret>"
dotnet ef database update --project src/Services/Identity/Nevma.Identity.Api
```

Identity uses ASP.NET Core Identity for password hashing and OpenIddict for OAuth 2.0/OpenID
Connect. The mobile client uses authorization code flow with PKCE through `/connect/authorize`
and `/connect/token`; there is no endpoint that directly turns a password into an access token.
Development seeds the public `nevma-mobile` client. Non-development environments must provide
signing and encryption PFX certificates through the `Authentication__*CertificatePath` and
`Authentication__*CertificatePassword` configuration keys.

The Planning service owns a separate PostgreSQL database/schema and validates `nevma_api`
access tokens issued by Identity. Configure and migrate it independently:

```powershell
$env:ConnectionStrings__PlanningDatabase="Host=localhost;Port=5432;Database=nevma_planning;Username=nevma_planning;Password=<secret>"
$env:Authentication__Issuer="https://identity.example.com/"
dotnet ef database update --project src/Services/Planning/Nevma.Planning.Api
```

The Messaging service owns the `messaging` schema and validates the same Identity access
tokens. Messages are committed to PostgreSQL before SignalR publishes them. Configure and
migrate this database independently as well:

```powershell
$env:ConnectionStrings__MessagingDatabase="Host=localhost;Port=5432;Database=nevma_messaging;Username=nevma_messaging;Password=<secret>"
$env:Authentication__Issuer="https://identity.example.com/"
dotnet ef database update --project src/Services/Messaging/Nevma.Messaging.Api
```

Notifications owns another isolated PostgreSQL schema. Push tokens are protected with ASP.NET
Core Data Protection and are never returned by the API:

```powershell
$env:ConnectionStrings__NotificationsDatabase="Host=localhost;Port=5432;Database=nevma_notifications;Username=nevma_notifications;Password=<secret>"
$env:Authentication__Issuer="https://identity.example.com/"
dotnet ef database update --project src/Services/Notifications/Nevma.Notifications.Api
```

Production deployments must persist the Data Protection key ring in a shared protected store;
losing or changing that key ring makes existing protected push tokens unreadable.

FCM delivery is disabled by default. Enable it only after configuring Application Default
Credentials with the minimum Firebase Cloud Messaging role and setting the target project:

```powershell
$env:GOOGLE_APPLICATION_CREDENTIALS="C:\secure\nevma-fcm-service-account.json"
$env:PushDelivery__Enabled="true"
$env:PushDelivery__ProjectId="<firebase-project-id>"
```

The credential file must stay outside the repository. Delivery attempts are persisted before
sending, retry transient failures with bounded backoff, and revoke devices after permanent
provider errors such as an unregistered target.

Redis SignalR scale-out remains a later infrastructure slice; PostgreSQL is the source of truth
for conversations and messages.

Files and Commands own the `files` and `commands` schemas respectively. Their credentials must
also be supplied outside source control before applying migrations:

```powershell
$env:ConnectionStrings__FilesDatabase="Host=localhost;Database=nevma_files;Username=nevma_files;Password=<secret>"
$env:ConnectionStrings__CommandsDatabase="Host=localhost;Database=nevma_commands;Username=nevma_commands;Password=<secret>"
dotnet ef database update --project src/Services/Files/Nevma.Files.Api
dotnet ef database update --project src/Services/Commands/Nevma.Commands.Api
```

The default file adapter stores content under the service's private `App_Data/files` directory;
production should configure `FileStorage__RootPath` to an encrypted private volume or replace
`IFileStorage` with private object storage. Uploads are capped at 25 MB and validated by extension,
MIME type, file signature, SHA-256, authorization, and the configured scanner adapter.

Commands never execute directly from raw voice text. `/api/commands/preview` creates a durable
structured plan, `/confirm` executes it with the authenticated user's token, and `/undo` reverses
supported operations. Raw transcripts are not persisted; the audit record stores their hash.

## Gateway and observability

The Gateway applies strict configured CORS, per-IP rate limiting, request-size limits, correlation
IDs, security headers, and reverse-proxy routing. Replace `Cors__AllowedOrigins` in every deployed
environment; do not use wildcard origins with credentials.

All deployables collect ASP.NET Core, HTTP client, runtime, trace, metric, and structured log
telemetry. OTLP export is disabled by default and can be enabled without code changes:

```powershell
$env:OpenTelemetry__Otlp__Enabled="true"
$env:OpenTelemetry__Otlp__Endpoint="https://otel-collector.example.com:4317"
```

Run database migrations as a controlled deployment step before starting each service. Back up each
service database independently, persist Data Protection keys, rotate secrets, and alert on health,
outbox backlog, dead-letter queues, push failures, elevated 401/403/429 rates, and command failures.

Planning records integration events in its PostgreSQL transactional outbox. RabbitMQ delivery
is opt-in locally and requires the broker URI to come from environment configuration:

```powershell
$env:MessageBroker__Enabled="true"
$env:MessageBroker__Uri="amqps://<user>:<password>@<host>/<vhost>"
```

Publisher confirms are enabled, and failed deliveries remain in the outbox for retry. No broker
credentials are stored in source control. Messaging consumes these events through a durable
quorum queue, records processed event IDs in its inbox, and dead-letters malformed payloads.
