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

Planning and Messaging stores are still intentionally in memory. Redis, durable messaging,
authentication, and the transactional outbox will be introduced in later backend slices.

The Identity service now owns a PostgreSQL schema named `identity`. Supply its password and
other environment-specific values outside source control, for example:

```powershell
$env:ConnectionStrings__IdentityDatabase="Host=localhost;Port=5432;Database=nevma_identity;Username=nevma_identity;Password=<secret>"
dotnet ef database update --project src/Services/Identity/Nevma.Identity.Api
```
