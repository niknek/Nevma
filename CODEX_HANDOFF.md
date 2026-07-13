# Nevma - Codex Project Handoff

Last updated: 2026-07-13

## Instructions for the next Codex

- Communicate with the user in Greek unless the user requests another language.
- Read this file and the repository before changing code.
- Preserve the service boundaries and security-first direction described below.
- Do not replace the current architecture with a monolith.
- Do not install or introduce Docker unless the user explicitly approves it.
- Explain important classes and patterns in plain Greek as they are introduced; the user wants to understand the architecture while building it.
- Before editing, describe the files and responsibilities that will change.
- Keep changes in focused commits with natural engineering commit messages.
- The user prefers current stable technology, but production stability and security are more important than novelty.

## Repository

- GitHub: https://github.com/niknek/Nevma
- Active branch: `development`
- Initial commit: `b0188c7 Initialize backend service architecture`
- Local solution: `Nevma.slnx`
- Target framework: `.NET 10`
- SDK pinned in `global.json`: `10.0.301`
- EF CLI installed on the original PC: `dotnet-ef 10.0.9`
- Visual Studio used on the original PC: Visual Studio Community 2026

After cloning on another PC:

```powershell
git clone https://github.com/niknek/Nevma.git
cd Nevma
git switch development
dotnet restore Nevma.slnx
dotnet build Nevma.slnx
dotnet test Nevma.slnx
```

## Product Vision

Nevma is a mobile-first, voice-first personal planning and collaboration platform.
It is intended to grow into a large product with many users and high realtime traffic.

The user must be able to perform every important operation in two ways:

1. By natural live voice commands.
2. Manually through normal mobile UI controls.

Voice must not blindly press UI controls. Voice input is converted into a structured command, validated by the backend, previewed to the user when necessary, authorized, confirmed for sensitive actions, executed, and audited.

The product was originally called `Nima` / Greek `Nima`, but was renamed to `Nevma`. The Greek concept is `Nεύμα`: a signal, intention, or subtle command. Use `Nevma` as the product and codebase name.

## User Experience Decisions

The product will be mobile first. A web application may be added later without rewriting business logic.

Current navigation concept:

```text
Home | Schedule | Voice | Conversations | Files
```

The profile and security settings are opened from the avatar, not from a permanent bottom navigation item.

Home should answer one question immediately: "What should I do now?"

Home content should include:

- One dominant next action.
- A compact route to urgent items.
- Important requests or live communication requiring action.
- A short preview of what comes next.

Schedule should include:

- Day agenda.
- Calendar week/month views.
- Tasks with filters such as urgent, today, upcoming, and completed.

Conversations should include:

- Personal and group conversations.
- Contacts.
- Connection and access requests.
- Typing indicators, delivery/read states, and presence.
- Voice messages, files, photos, and meeting proposals.

The visual direction is minimal and premium:

- Warm off-white surfaces.
- Graphite text and controls.
- A restrained coral/vermilion accent for voice, urgency, and unread states.
- Avoid teal/turquoise banking aesthetics, gradients, glassmorphism, excessive cards, and Threads-like woven logos.

## Core Product Capabilities

### Planning and tasks

- Personal tasks and obligations.
- Deadlines, priorities, reminders, and filters.
- Daily agenda and calendar views.
- Manual and voice creation/editing.

### User-to-user meeting proposals

Example: User A says, "Ask B to go for coffee tomorrow at 8."

Expected workflow:

1. The system parses participant, title, time, duration, and optional location.
2. User A sees a command preview and confirms it.
3. User B receives a realtime meeting proposal.
4. User B can accept, decline, or counter-propose another time.
5. On acceptance, a shared calendar event is created for both users.
6. Both users receive live updates and reminders.
7. Rescheduling or cancellation informs every participant.

Pending invitations may appear as tentative events. Accepted invitations become confirmed calendar events. Time zones and conflicts must be handled explicitly.

### Realtime communication

- SignalR is the current realtime technology.
- Realtime data must be persisted before it is broadcast.
- Database state is the source of truth.
- SignalR groups will eventually be based on authenticated users and conversations.
- Do not trust a user ID provided by the mobile client; derive identity from the authenticated principal.

### Files and photos

- Support photos, PDFs, and other attachments.
- Do not send large binary files through SignalR.
- Upload to a file service, persist metadata, then publish an event containing safe metadata.
- Production storage should be private object storage through an abstraction such as `IFileStorage`.
- Validate extension, MIME type, file signature, size, and authorization.
- Add malware scanning, EXIF removal, thumbnails, signed URLs, and audit logging.

### Voice and AI

- AI provider must be replaceable through interfaces/adapters.
- Initial command parsing can be rule-based; an AI adapter can be added later.
- AI proposes structured commands. The backend remains the authority.
- Sensitive commands require confirmation.
- Commands require authorization, idempotency, audit, and eventually undo where feasible.
- Protect the system from prompt injection and untrusted document content.

## Architecture Decision

Nevma is not a single monolith and should not start with dozens of tiny microservices. It uses a small service-oriented architecture with clear bounded contexts.

Planned deployable services:

```text
Mobile App
    |
Nevma.Gateway (mobile BFF / reverse proxy)
    |
    +-- Identity Service
    +-- Planning Service
    +-- Messaging Service
    +-- File Service (planned)
    +-- Notification Workers (planned)
    +-- Command / AI Service (planned)
```

Each service owns its data. A service must never query another service's database directly.

Communication model:

- HTTP or gRPC for immediate synchronous operations.
- Integration events through a message broker for asynchronous work.
- SignalR for realtime client updates.
- Transactional outbox/inbox for reliable event processing.

## Current Solution Structure

```text
Nevma.slnx
src/
  BuildingBlocks/
    Nevma.Contracts/
  Gateway/
    Nevma.Gateway/
  Services/
    Identity/
      Nevma.Identity.Api/
    Planning/
      Nevma.Planning.Api/
    Messaging/
      Nevma.Messaging.Api/
tests/
  Nevma.ArchitectureTests/
```

Within each existing service:

```text
Service
  Domain/          Business objects and rules
  Application/     Use cases, orchestration, and ports/interfaces
  Infrastructure/  Technical adapters and repository implementations
  Endpoints/       HTTP transport mapping
  DependencyInjection.cs
  Program.cs       Composition and middleware pipeline only
```

Dependency direction:

```text
Endpoints -> Application -> Domain
Infrastructure -> Application interfaces
Program/DI composes the implementations
```

The current folders are logical layers inside one project per service. They can later become separate class-library projects if stronger compile-time boundaries are needed. Do not split them mechanically before there is enough complexity to justify it.

## Current Implemented Code

### Shared contracts

`Nevma.Contracts` contains transport/integration DTOs, not database entities.

Examples:

- `CreateUserRequest`
- `UserSummary`
- `CreateMeetingInvitationRequest`
- `MeetingInvitationResponse`
- `CalendarEventResponse`
- `SendMessageRequest`
- `MessageResponse`

Records are used because these objects represent immutable data transferred between boundaries. Do not expose internal entities directly as API responses.

### Gateway

`Nevma.Gateway` uses YARP reverse proxy.

Current local routes:

```text
/identity/*  -> http://localhost:5016
/planning/*  -> http://localhost:5276
/messaging/* -> http://localhost:5085
```

The Gateway currently has OpenAPI, health checks, HTTPS redirection, and reverse proxy mapping. Authentication, authorization, rate limiting, correlation IDs, security headers, tracing, and production routing are not implemented yet.

### Identity Service

Implemented as an educational in-memory slice:

- Domain `User`.
- `IUserRepository` application port.
- `UserService` application logic.
- `InMemoryUserRepository` infrastructure adapter.
- User create/get endpoints.

This is not production authentication. There is no password handling, JWT, session management, database, or resource authorization yet.

### Planning Service

Implemented:

- Domain `MeetingInvitation`.
- Domain `CalendarEvent`.
- `IPlanningRepository`.
- `PlanningService`.
- In-memory planning repository.
- Create/get/accept meeting invitation endpoints.
- Calendar query endpoint.
- Result pattern for accepted, not found, forbidden, and already handled outcomes.

The current `AcceptInvitationResult` is an abstract record used as a discriminated result hierarchy. It avoids exceptions for expected business outcomes and lets `Accepted` carry the created `CalendarEventResponse`.

Current limitations:

- In-memory concurrency control only.
- No database transaction.
- No event outbox.
- No decline/counter-proposal/reschedule/cancel flows.
- No conflict detection or recurrence.
- User IDs are currently accepted from the request and are not authenticated.

### Messaging Service

Implemented:

- Domain `Message`.
- `IMessageRepository`.
- `MessageService`.
- In-memory message repository.
- Conversation message endpoints.
- SignalR `ChatHub` with join, leave, and typing operations.
- `message.received` realtime event.

Current limitations:

- No authenticated SignalR connections.
- No conversation membership authorization.
- No durable database.
- No delivery/read receipts, presence store, reactions, attachments, or pagination.

### Tests

An architecture test checks that services do not reference each other's projects directly.

The latest verified state before this handoff was:

```text
Build: succeeded with 0 warnings and 0 errors
Architecture tests: passed
NuGet vulnerability audit: no known vulnerable packages
```

Re-run these checks on the new PC rather than assuming the result is still current.

## Patterns to Use

Use these patterns where they solve a concrete problem:

- Vertical Slice Architecture for use cases.
- CQRS: commands mutate state, queries read state.
- Result pattern for expected business outcomes.
- Aggregate roots for transactional consistency boundaries.
- Repository only around aggregate roots.
- EF Core `DbContext` as Unit of Work.
- Domain events within a service.
- Integration events between services.
- Transactional Outbox for reliable publication.
- Inbox/idempotent consumers for duplicate messages.
- Idempotency keys for mobile command retries.
- Optimistic concurrency for conflicting updates.
- Process Manager/Saga for workflows spanning services.
- Adapter/Strategy interfaces for AI, speech, push notifications, and file storage.
- API Gateway/BFF for the mobile client.

Avoid introducing abstractions that do not yet remove real complexity. In particular, do not create a generic repository for every entity and do not use exceptions as normal flow control.

## Planned Foundation

The next shared project should be `Nevma.ServiceDefaults` or an equivalent narrowly scoped building block.

Planned cross-cutting components:

```text
Errors/
  Error.cs
  Result.cs
  ResultOfT.cs
Middleware/
  CorrelationIdMiddleware.cs
  SecurityHeadersMiddleware.cs
Extensions/
  ServiceCollectionExtensions.cs
  ApplicationBuilderExtensions.cs
```

Planned defaults:

- RFC Problem Details.
- Global exception handling.
- Correlation/trace IDs.
- OpenTelemetry logs, metrics, and traces.
- Health checks.
- Rate limiting policies.
- Secure JSON and HTTP defaults.
- Authentication/authorization defaults when identity is introduced.

Do not put business authorization such as "can this user accept this invitation?" into generic middleware. That belongs in application/domain logic. Middleware handles cross-cutting HTTP concerns.

## Planned Domain Work

### Identity

Planned domain concepts:

- `User`
- `DeviceSession`
- `RefreshToken`
- `ContactConnection`
- `BlockedUser`
- `UserSettings`
- Value objects such as `EmailAddress`, `DisplayName`, and `DeviceId`

Planned use cases:

- Register and login.
- Refresh-token rotation.
- Logout one/all devices.
- Current profile and profile update.
- Contact request/accept/reject.
- Block/report user.

Use a proven OAuth/OIDC and identity solution. Do not invent password hashing, token formats, or cryptographic protocols.

### Planning

Planned domain concepts:

- `MeetingInvitation` aggregate.
- `MeetingParticipant`.
- `CalendarEvent`.
- `TaskItem`.
- `Reminder`.
- `RecurrenceRule`.
- Event attachment references.

Planned use cases:

- Create, accept, decline, and counter-propose meeting invitations.
- Reschedule and cancel meetings.
- Calendar day/week/month queries.
- Conflict detection.
- Create, prioritize, complete, and filter tasks.
- Schedule reminders.

### Messaging

Planned domain concepts:

- `Conversation` aggregate.
- `ConversationParticipant`.
- `Message`.
- `MessageReceipt`.
- `MessageReaction`.
- `AttachmentReference`.

Planned use cases:

- Create personal/group conversation.
- Send and paginate messages.
- Mark delivered/read.
- React, reply, edit, and delete according to policy.
- Authorize membership before every operation.
- Presence and typing through Redis-backed realtime infrastructure later.

### Files

Planned concepts:

- `FileAsset`
- `UploadSession`
- `FileAccessGrant`
- `FileAttachment`
- `MalwareScanResult`
- `Thumbnail`

Planned provider interfaces:

- `IFileStorage`
- `IFileScanner`
- `IThumbnailGenerator`
- `ISignedUrlGenerator`

### Notifications

Planned concepts:

- `Notification`
- `NotificationPreference`
- `PushDevice`
- `DeliveryAttempt`
- `NotificationTemplate`

Planned provider interfaces:

- `IPushNotificationSender`
- `IEmailSender`
- `ILiveNotificationPublisher`

### Command and AI

Planned concepts:

- `CommandRequest`
- `ParsedIntent`
- `CommandPlan`
- `CommandStep`
- `CommandConfirmation`
- `CommandExecution`
- `UndoOperation`

Planned provider interfaces:

- `ISpeechToTextProvider`
- `IIntentParser`
- `ICommandValidator`
- `ICommandExecutor`
- `IAiProvider`

## Security Requirements

The user explicitly requires very strong security. Never describe the product as unhackable. Build defense in depth.

Required principles:

- Short-lived access tokens.
- Rotating, hashed refresh tokens.
- Session and device tracking.
- Resource-level authorization in every service.
- Authenticated SignalR; derive user identity from claims.
- Strict input and upload validation.
- Rate limiting and abuse controls.
- Idempotency and replay protection.
- HTTPS/HSTS and strict CORS.
- Secrets outside source control.
- Audit important state changes without logging sensitive content.
- Encryption in transit and at rest.
- Dependency and container scanning.
- Backups, monitoring, alerting, and incident-ready logs.
- Confirmation for sensitive voice/AI actions.
- No sensitive information in push notification previews by default.

## Infrastructure Direction

Planned production stack:

- ASP.NET Core 10 / C# / EF Core 10.
- PostgreSQL as the primary relational database.
- One database or strongly isolated schema ownership per service.
- Redis for caching, presence, rate limiting support, and SignalR scale-out.
- RabbitMQ or a managed message broker for asynchronous integration events.
- Private S3-compatible or Azure Blob storage for files.
- OpenTelemetry for observability.
- Docker/containers later, only after explicit user approval.

## Testing Strategy

Planned test layers:

- Domain unit tests.
- Application handler/service tests.
- Integration tests against real infrastructure when Docker is approved.
- Architecture tests.
- API contract tests.
- End-to-end workflow tests.

High-risk scenarios to test:

- Two concurrent accept attempts for the same invitation.
- Unauthorized user accepting another user's invitation.
- Duplicate mobile command or broker event.
- Refresh-token replay and rotation.
- Conversation membership violations.
- File access and malicious upload attempts.
- Event publication failure after database commit.
- Time zone and daylight-saving transitions.

## Recommended Implementation Sequence

Use small, reviewable commits in this order:

1. Add service defaults, Problem Details, correlation IDs, and common Result/Error primitives.
2. Add Identity persistence and a proven authentication foundation.
3. Implement contact connection workflow and resource authorization.
4. Add Planning persistence and the full meeting-invitation lifecycle.
5. Add calendar queries, conflict detection, and reminders.
6. Add durable Messaging persistence and authenticated SignalR.
7. Integrate meeting invitations with conversations using outbox/integration events.
8. Add notification workers and push delivery.
9. Add secure file upload/download flow.
10. Add voice command preview, confirmation, execution, audit, and undo foundations.

The first end-to-end milestone should be:

```text
Register/login
-> connect two users
-> create meeting proposal
-> persist invitation
-> deliver realtime notification
-> accept invitation
-> create shared calendar event
-> notify both users
```

## Current Important Caveats

- The existing in-memory repositories are temporary educational adapters.
- The APIs are not secure yet and must not be deployed publicly.
- Client-provided user IDs are placeholders until authentication exists.
- There is no durable event broker or outbox yet.
- There is no file service, notification service, or AI service yet.
- The UI mockups are conceptual and are not implemented in code.
- Check the current Git working tree before editing; another Codex task may have changed files.

## Suggested First Prompt on the Other PC

Give the other Codex this repository and say:

```text
Read CODEX_HANDOFF.md and inspect the repository. Continue from the development branch.
Before editing, explain in Greek the next focused slice and the classes it will add.
Start with the shared service defaults and error-handling foundation, preserve all
current service boundaries, run build/tests/package audit, and do not add Docker yet.
```
