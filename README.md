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

The current stores are intentionally in memory. PostgreSQL, Redis, durable messaging,
authentication, and the transactional outbox will be introduced in the next backend slice.
