# Nevma

Voice-first planning and collaboration platform.

## Services

- `Nevma.Gateway`: the single mobile API entry point and reverse proxy.
- `Nevma.Identity.Api`: users, devices, sessions, contacts, and permissions.
- `Nevma.Planning.Api`: calendar, meeting invitations, tasks, and reminders.
- `Nevma.Messaging.Api`: conversations and SignalR realtime messaging.
- `Nevma.Contracts`: versioned integration contracts shared between services.
- `Nevma.ServiceDefaults`: shared HTTP error handling, correlation IDs, security headers,
  health checks, and reusable result primitives. It contains no business rules.

## Local development

The repository is pinned to .NET SDK 10.0.301.

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

Redis SignalR scale-out remains a later infrastructure slice; PostgreSQL is the source of truth
for conversations and messages.

Planning records integration events in its PostgreSQL transactional outbox. RabbitMQ delivery
is opt-in locally and requires the broker URI to come from environment configuration:

```powershell
$env:MessageBroker__Enabled="true"
$env:MessageBroker__Uri="amqps://<user>:<password>@<host>/<vhost>"
```

Publisher confirms are enabled, and failed deliveries remain in the outbox for retry. No broker
credentials are stored in source control. Messaging consumes these events through a durable
quorum queue, records processed event IDs in its inbox, and dead-letters malformed payloads.
