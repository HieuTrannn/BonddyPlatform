# Bonddy Platform — Backend API

[![CI / Deploy](https://github.com/HieuTrannn/BonddyPlatform/actions/workflows/deploy.yml/badge.svg)](https://github.com/HieuTrannn/BonddyPlatform/actions/workflows/deploy.yml)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![PostgreSQL 16](https://img.shields.io/badge/PostgreSQL-16-336791)
[![FIP Youth Startup 2025 – Top 10](https://img.shields.io/badge/FIP%20Youth%20Startup%202025-Top%2010-FFD700)](https://drive.google.com/file/d/1kR691GAnZEBqFW_23VC28czJ5IwHHYpX/view?usp=drive_link)

**Bonddy** connects travelers with **local buddies**: residents who show visitors around their city.
A traveler posts a trip, buddies send requests, the two chat in real time, the buddy makes an offer (a booking),
and the traveler pays online. Buddies pay a monthly subscription for extra visibility and lower commission.

This repository is the backend REST + real-time API. It started as the team project for the
**EXE101 (Experiential Entrepreneurship) course at FPT University**.

> **Demo status:** the production server (`bonddy.vn`) is currently **offline**. To try the API, run it
> locally with Docker Compose (see [Run locally](#run-locally)) and open Swagger.

## 🏆 Achievements

- **Top 10 — FIP Youth Startup 2025**, organized by FPT Innovation Path Club (FIP), FPT University
  (November 2025), team **Nova**, with Bonddy — a platform connecting travelers with local buddies.

Bonddy started as an EXE101 (Experiential Entrepreneurship) course project and was developed further for the competition.

## My role

I, **Trần Trung Hiếu**, built **the whole backend**: database design, the REST API, real-time chat
and notifications, PayOS/PayPal payment integration, the background status job, Docker/Nginx deployment and CI.
The frontend was built by other team members in a separate repository.

## Features

| Area | What it does |
|---|---|
| **Auth** | Email + password login with JWT access/refresh tokens, Google sign-in via Firebase ID tokens, OTP email verification, forgot/reset/change password, BCrypt hashing |
| **Roles** | `User` (traveler), `Buddy`, `CS` (customer service), `Admin`, enforced with `[Authorize(Roles = ...)]` |
| **Trips & requests** | Travelers post trips (city, date, activities, languages); buddies send trip requests; a request opens a chat |
| **Real-time chat** | SignalR `ChatHub` with rooms, online presence and read/unread state per user |
| **Bookings** | Buddy creates an offer from the chat, traveler confirms and pays; status rules enforced by a state machine |
| **Payments** | PayOS (VietQR) and PayPal checkout, escrow-style payment status (`Pending → Held → Released / Refunded`), signed webhooks |
| **Buddy subscriptions** | Monthly packages with slot limits, prorated **upgrades** and end-of-cycle **downgrades** |
| **Background job** | `StatusExpiryJob` expires stale trips/bookings and moves bookings to `InProgress` / `Completed` every 5 minutes |
| **Notifications** | Stored notifications + SignalR `NotificationHub` push, unread counters |
| **Reviews & incidents** | Travelers review completed bookings; incidents can be reported and resolved by CS/Admin |
| **Admin dashboard** | 17 read-only analytics endpoints (KPIs, revenue, booking mix, buddy performance, risk alerts) |
| **Media & email** | Avatar upload to Firebase Storage, HTML emails over SMTP (MailKit) |

## Architecture

Classic three-layer architecture: controllers stay thin, business rules live in services, data access goes through
repositories and a Unit of Work.

```mermaid
flowchart LR
    FE["Web frontend<br/>(separate repo)"]

    subgraph API["BonddyPlatform.API (ASP.NET Core 8)"]
        CTRL["15 REST controllers"]
        HUBS["SignalR hubs<br/>ChatHub · NotificationHub"]
        JOB["StatusExpiryJob<br/>(BackgroundService)"]
    end

    subgraph SVC["BonddyPlatform.Services"]
        BS["Business services<br/>Booking · Payment · ServicePackage · Chat ..."]
        INT["Integrations<br/>PayOS · PayPal · Firebase · SMTP · JWT"]
    end

    subgraph REPO["BonddyPlatform.Repositories"]
        UOW["Unit of Work +<br/>generic repositories"]
        DB_CTX["EF Core DbContext<br/>+ migrations"]
    end

    PG[("PostgreSQL 16")]
    EXT["PayOS · PayPal<br/>Firebase · Gmail SMTP"]

    FE -- "HTTPS / JWT" --> CTRL
    FE -- "WebSocket" --> HUBS
    CTRL --> BS
    HUBS --> BS
    JOB --> DB_CTX
    BS --> UOW --> DB_CTX --> PG
    BS --> INT --> EXT
    EXT -. webhooks .-> CTRL
```

## Booking & payment lifecycle

Values are exactly those of `BookingStatus` in
[`Booking.cs`](BonddyPlatform.Repositories/Models/Booking.cs). Allowed manual transitions are enforced in
[`BookingService`](BonddyPlatform.Services/Implements/BookingService.cs); time-based transitions are done by
[`StatusExpiryJob`](BonddyPlatform.API/BackgroundJobs/StatusExpiryJob.cs).

```mermaid
stateDiagram-v2
    [*] --> PendingCustomerConfirm: Buddy creates booking (offer in chat)

    PendingCustomerConfirm --> PendingPayment: Traveler confirms<br/>(24h payment deadline set)
    PendingCustomerConfirm --> Cancelled: Cancelled by either side
    PendingCustomerConfirm --> Expired: Job: start time passed

    PendingPayment --> Confirmed: PayOS / PayPal webhook or capture<br/>(payment Pending → Held)
    PendingPayment --> CancelledByTimeout: Not paid in time
    PendingPayment --> Cancelled: Cancelled by either side
    PendingPayment --> Expired: Job: start time passed

    Confirmed --> InProgress: Job: tour start time reached
    Confirmed --> Cancelled: Cancelled

    InProgress --> Completed: Job: start + duration passed
    InProgress --> Cancelled: Cancelled

    Completed --> [*]: Traveler can review
    Cancelled --> [*]
    CancelledByTimeout --> [*]
    Expired --> [*]
```

Payment status follows its own escrow state machine in
[`PaymentService`](BonddyPlatform.Services/Implements/PaymentService.cs):
`Pending → Held → Released`, with `Refunded` reachable from `Pending` or `Held`.

A full sequence (API contracts, webhook payloads) is in
[docs/booking-payment-end-to-end.md](docs/booking-payment-end-to-end.md).

## Tech stack

| Layer | Technology |
|---|---|
| Runtime / framework | .NET 8, ASP.NET Core Web API, SignalR |
| Data | PostgreSQL 16, Entity Framework Core 8 (Npgsql), code-first migrations |
| Auth | JWT bearer tokens + refresh tokens, Firebase Admin SDK (Google sign-in), BCrypt |
| Payments | PayOS (VietQR, HMAC-SHA256 signed webhooks), PayPal Orders v2 + webhook verification |
| Integrations | Firebase Storage (avatars), MailKit (SMTP email) |
| API docs | Swagger / OpenAPI (Swashbuckle) with annotations |
| Testing | xUnit, NSubstitute, EF Core InMemory |
| DevOps | Docker, Docker Compose, Nginx reverse proxy, GitHub Actions (build → test → deploy over SSH) |

## Project structure

```text
BonddyPlatform/
├── BonddyPlatform.API/            # Entry point: controllers, SignalR hubs, background job, Program.cs
│   ├── BackgroundJobs/            #   StatusExpiryJob
│   ├── Controllers/               #   15 REST controllers
│   ├── Hubs/                      #   ChatHub, NotificationHub
│   └── Realtime/                  #   Presence tracking, SignalR notifier
├── BonddyPlatform.Services/       # Business logic
│   ├── DTOs/                      #   Request/response models
│   ├── Helpers/                   #   Query helpers, proration, Firebase credential loader
│   ├── Implements/ · Interfaces/  #   Services (Booking, Payment, PayOS, ServicePackage, Auth, ...)
│   └── Options/                   #   Strongly typed settings (Jwt, Smtp, PayOS, PayPal, Firebase)
├── BonddyPlatform.Repositories/   # Data access
│   ├── Models/                    #   Entities + enums (BookingStatus, PaymentStatus, ...)
│   ├── Persistences/              #   BonddyDbContext
│   ├── Implements/ · Interfaces/  #   Generic + specific repositories, UnitOfWork
│   └── Migrations/                #   EF Core migrations
├── BonddyPlatform.Tests/          # xUnit tests
├── docs/                          # Business flows, FE integration guides, deployment
├── scripts/                       # Maintenance scripts (git history secret purge)
├── docker-compose.yml             # API + PostgreSQL
└── .env.example                   # All configuration variables
```

## Run locally

Requirements: Docker Desktop. (.NET 8 SDK only if you want to run tests or the API outside Docker.)

```bash
git clone https://github.com/HieuTrannn/BonddyPlatform.git
cd BonddyPlatform
cp .env.example .env
```

Edit `.env` and set at least `POSTGRES_PASSWORD` and `JWT_SECRET` (32+ characters, e.g. `openssl rand -base64 48`).
Payment, email and Firebase keys are optional: those features stay disabled (or return an error) until configured.

```bash
docker compose up -d --build
```

- Swagger UI: <http://localhost:8080/swagger>
- Database migrations are applied automatically on startup.
- SignalR hubs: `/hubs/chat`, `/hubs/notifications` (pass the JWT as `access_token` query string).

Run the tests:

```bash
dotnet test
```

### Environment variables

| Variable | Required | Description |
|---|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | password: yes | PostgreSQL container credentials (also used to build the API connection string) |
| `JWT_SECRET` | yes | HMAC key for signing JWTs, at least 32 bytes. The API refuses to start without it |
| `JWT_ISSUER` / `JWT_AUDIENCE` | no | Default `BonddyPlatform` |
| `JWT_ACCESS_TOKEN_EXPIRY_MINUTES` | no | Default `60` |
| `SMTP_USERNAME` / `SMTP_PASSWORD` / `SMTP_FROM_EMAIL` | for email | Gmail account + App Password used to send OTP and notification emails |
| `FIREBASE_CREDENTIALS_JSON` | for Google login / avatars | Firebase service-account JSON (raw or base64). Never commit the key file |
| `PAYOS_CLIENT_ID` / `PAYOS_API_KEY` / `PAYOS_CHECKSUM_KEY` | for PayOS | PayOS merchant keys. Webhooks are rejected while the checksum key is empty |
| `PAYOS_RETURN_URL` / `PAYOS_CANCEL_URL` | no | Frontend redirect pages after PayOS checkout |
| `PAYPAL_BASE_URL` | for PayPal | `https://api-m.sandbox.paypal.com` (sandbox) or `https://api-m.paypal.com` |
| `PAYPAL_CLIENT_ID` / `PAYPAL_SECRET` | for PayPal | PayPal REST app credentials |
| `PAYPAL_WEBHOOK_ID` | for PayPal webhooks | Webhook id used to verify incoming events |
| `PAYPAL_RETURN_URL` / `PAYPAL_CANCEL_URL` | for PayPal | Frontend redirect pages after PayPal approval |
| `ENABLE_SWAGGER` | no | `true` to expose Swagger outside Development (set in docker-compose) |
| `RUN_MIGRATIONS` | no | `true` to auto-migrate outside Production |
| `DATABASE_URL` | no | Alternative `postgres://user:pass@host:port/db` URL (for PaaS hosting) |

Outside Docker, the same settings can be provided with `dotnet user-secrets` or the standard
`Section__Key` environment variables (for example `ConnectionStrings__DefaultConnection`).

## API overview

Counted from the controller source: **135 HTTP routes** served by **132 action methods** in **15 controllers**
(3 PayPal/PayOS actions are exposed under two routes for backward compatibility), plus **2 SignalR hubs**.

| Controller | Base route | Routes | Main responsibilities |
|---|---|---:|---|
| `AuthController` | `/api/Auth` | 9 | Login, register, OTP, refresh token, Google login, password reset/change |
| `UsersController` | `/api/Users` | 9 | User CRUD/search, own profile, avatar upload |
| `BuddiesController` | `/api/Buddies` | 10 | Buddy profiles, register as buddy, pending applicants |
| `TripsController` | `/api/Trips` | 7 | Create/update/delete trips, my trips, open trips |
| `TripRequestsController` | `/api/TripRequests` | 5 | Buddy requests to join a trip, start chat |
| `ChatController` | `/api/Chat` | 7 | Rooms, message history, send message, unread summary, mark read |
| `BookingsController` | `/api/Bookings` | 10 | Create offer, traveler confirm (+ create PayPal/PayOS order), status changes, my bookings |
| `PaymentsController` | `/api/Payments` | 12 | Booking payments, PayPal create/capture, PayOS create, PayPal & PayOS webhooks, escrow transitions |
| `ServicePackagesController` | `/api/ServicePackages` | 11 | Package CRUD, subscribe / upgrade / downgrade, capture, verify PayOS, cancel |
| `ReviewsController` | `/api/Bookings/{id}/review`, `/api/Buddies/{id}/reviews` | 3 | Review a completed booking, list a buddy's reviews |
| `IncidentsController` | `/api/Incidents` | 4 | Report and resolve incidents on bookings |
| `NotificationsController` | `/api/Notifications` | 4 | List, unread count, mark read / read all |
| `ContactsController` | `/api/Contacts` | 6 | Lead/contact list (name, email, phone): CRUD + search |
| `AdminController` | `/api/admin` | 21 | Manage users, buddies (approve/toggle), trips, bookings, incidents |
| `AdminOverviewController` | `/api/admin/overview` | 17 | Dashboard analytics: KPIs, revenue, trends, buddy performance |
| **Total** | | **135** | |

| SignalR hub | Path | Client → server methods |
|---|---|---|
| `ChatHub` | `/hubs/chat` | `JoinRoom`, `LeaveRoom`, `SendMessage` |
| `NotificationHub` | `/hubs/notifications` | server push only |

## Testing

`BonddyPlatform.Tests` focuses on the parts where a bug costs money or corrupts data:

- **PayOS webhook signature**: canonical HMAC-SHA256 string, tampered amount, wrong key, and a regression test
  for forged signatures when the checksum key is not configured.
- **PayPal webhooks**: events change nothing unless PayPal's verify API returns `SUCCESS`; missing headers
  or missing webhook id are rejected without any network call.
- **`StatusExpiryJob`**: every booking/trip time transition, boundaries (start time == now), terminal states,
  idempotency (EF Core InMemory database, fixed clock).
- **Subscription upgrade/downgrade**: proration math, upgrades keep the billing cycle and charge the difference,
  downgrades are scheduled for the end of the cycle without charging.

GitHub Actions runs `dotnet build` and `dotnet test` on every push and pull request; deployment to the VPS
only runs on `main` after both succeed.

## Documentation

| Document | Content |
|---|---|
| [docs/booking-flow.md](docs/booking-flow.md) | Trip → request → chat → booking business flow (Vietnamese) |
| [docs/booking-payment-end-to-end.md](docs/booking-payment-end-to-end.md) | Booking to payment API contract and webhook handling (Vietnamese) |
| [docs/chat-realtime-fe.md](docs/chat-realtime-fe.md) | How the frontend connects to the SignalR chat hub (Vietnamese) |
| [docs/DEPLOY_VPS_DOMAIN.md](docs/DEPLOY_VPS_DOMAIN.md) | VPS, domain, Nginx and HTTPS setup (Vietnamese) |
| [docs/nginx-bonddy.conf.example](docs/nginx-bonddy.conf.example) | Nginx reverse-proxy config incl. WebSocket upgrade |
| [docs/bonddy_backend_training_doc.md](docs/bonddy_backend_training_doc.md) | Original product and database specification |

## Known limitations / next steps

- `PaymentDeadline` (24h) is recorded but not yet enforced automatically; `CancelledByTimeout` is set manually.
- Scheduled downgrades and end-of-cycle cancellations of subscriptions are stored but not yet applied by a job.
- Integration tests against a real PostgreSQL container (Testcontainers) would complement the unit tests.
