# Architecture Overview (EDCL Mini)

Arsitektur sistem **EDCL Mini** berbasis **Modular Monolith**, **Hexagonal Architecture (Ports & Adapters)**, dan **CQRS**, siap dimigrasikan ke **Microservices**.

---

## 1. Topologi Sistem & Diagram Arsitektur

```mermaid
graph TD
    DriverClient(["📱 Mobile Client (Driver)"])
    WebClient(["💻 Web Client (Admin/AppUser)"])
    Gateway["🚪 API Gateway (YARP)\n:5293\n─────────────────\nRouting & Reverse Proxy\nSingle Point of Entry"]

    subgraph Host ["EDCL.Api (Host App) - Modular Monolith (:5140)"]
        direction TB
        AuthModule["🔒 Auth Module\n(JWT, Users, Roles, Driver Auth)"]
        JobModule["📦 Job Module\n(PickupOrder, Stops, Assignment, Observability)"]
        CargoModule["📄 Cargo Module\n(Manifest, Kanban, Parts)"]
        NotificationModule["🔔 Notification Module\n(FCM Alerts, History Logs)"]
        DriverModule["🚚 Driver & Master Module\n(Suppliers, Trucks, Routes)"]
    end

    subgraph Workers ["Background Workers"]
        direction TB
        WorkerIngestion["📥 Ingestion Worker\n(Debezium CDC Consumer)"]
        WorkerOutbox["📤 Outbox Worker\n(Reliable Event Relay)"]
        WorkerReporter["📊 Reporter Worker\n(Read-Model Aggregator)"]
        WorkerGpsTracker["🛰️ GpsTracker Worker\n(Hangfire, GPS Adapters, OSRM)"]
    end

    subgraph Observability ["Observability Ecosystem"]
        direction TB
        Jaeger["📊 Jaeger Tracing (:16686)\n(Distributed OTLP Waterfall)"]
        Prometheus["📈 Prometheus (:9090)\n(PromQL Scraper /metrics)"]
    end

    subgraph Infrastructure ["Data & Messaging Layer"]
        direction TB
        SQLServer[("🗄️ SQL Server 2022 (:1444)\n──────────────────\nSchema: auth, job, cargo,\ndriver, notification, ingestion")]
        Redis[("🔴 Redis 7.2 (:6379)\n──────────────────\nDistributed Cache\nIdempotency Key Lock")]
        RabbitMQ[("🐰 RabbitMQ 3.13 (:5672)\n──────────────────\nMassTransit Event Bus\nDelayed Retry DLX")]
        Debezium["🔄 Debezium Engine\n(CDC from Legacy IDCS DB)"]
    end

    DriverClient -->|HTTP/REST| Gateway
    WebClient -->|HTTP/REST / SignalR / SSE| Gateway
    Gateway -->|Forward| Host

    Host -->|Reads/Writes| SQLServer
    Host -->|Cache & Locks| Redis
    Host -->|Publish Events| RabbitMQ
    Host -.->|OTLP Traces & Metrics| Jaeger & Prometheus

    Debezium -->|CDC Events| RabbitMQ
    RabbitMQ -->|Consume| WorkerIngestion
    WorkerIngestion -->|Upsert Data| SQLServer

    WorkerOutbox -->|Poll Outbox Table| SQLServer
    WorkerOutbox -->|Publish Messages| RabbitMQ

    RabbitMQ -->|Consume Events| WorkerReporter
    WorkerReporter -->|Save Reports| SQLServer

    WorkerGpsTracker -->|Sync Coordinates & OSRM| SQLServer
```

---

## 2. Prinsip & Pola Desain Kunci

- **API Gateway (Microsoft YARP)**: Single entry point pada port `5293`. Menangani reverse proxy, path routing, sanitasi header (`X-Forwarded-*`), dan load balancing internal.
- **Domain Isolation & Modular Monolith**: 5 modul terpisah (`Auth`, `Job`, `Cargo`, `Driver`, `Notification`) tanpa foreign key lintas skema fisik database (`auth.*`, `job.*`, `cargo.*`, dll.). Komunikasi lintas modul melalui interface contract di `Shared.Kernel` atau asynchronous events via RabbitMQ.
- **CQRS & MediatR Pipeline**: Pemisahan Command (Write Model) dan Query (Read Model). Didukung pipeline behavior untuk validasi, logging, dan idempotency locking.
- **Infrastruktur Terdistribusi**:
  - **SQL Server 2022**: Skema terisolasi per modul dengan Optimistic Concurrency Control (`RowVersion`).
  - **Redis 7.2**: Distributed cache dan lock idempotency (`X-Idempotency-Key`).
  - **RabbitMQ 3.13 (MassTransit)**: Message broker dengan 5-Stage Delayed Retry Queue dan Dead-Letter Exchange (DLX).
  - **Debezium 2.5 CDC**: Log tailing SQL Server IDCS untuk streaming perubahan data transaksi manifes.
- **Asynchronous Workers**:
  1. `Worker.Ingestion`: Konsumsi dan pemrosesan stream CDC Debezium.
  2. `Worker.Outbox`: Menjamin *At-Least-Once Delivery* melalui Transactional Outbox Pattern.
  3. `Worker.Reporter`: Agregasi data laporan read-model.
  4. `Worker.GpsTracker`: Sinkronisasi telemetri GPS multi-vendor dan geofencing via Hangfire.
- **Observability**: OpenTelemetry OTLP tracing ke Jaeger (`:16686`) dan scraping metrik Prometheus (`:9090`).

---

## 3. Alur Transaksi End-to-End (`Complete Stop`)

1. **Client Request**: Driver mengirim `POST /api/v1/jobs/stops/{id}/complete` via YARP Gateway (`:5293`) dengan header JWT dan `X-Idempotency-Key`.
2. **Idempotency Check**: Middleware memvalidasi key di Redis. Jika duplikat, kembalikan cached response seketika.
3. **Command Handling (CQRS)**: `CompleteStopCommand` diproses oleh handler modul `Job`.
4. **Transactional Outbox (Unit of Work)**: Status stop diperbarui ke `COMPLETED`, dan `StopCompletedEvent` disimpan ke tabel Outbox dalam transaksi database yang sama (ACID).
5. **Fast Response**: API mengembalikan respons `200 OK` ke klien mobile.
6. **Async Relay (Worker Outbox)**: `Worker.Outbox` membaca event dari tabel Outbox dan mem-publish-nya ke RabbitMQ.
7. **Async Reaction**: Modul `Notification` dan `Worker.Reporter` mengonsumsi event untuk memicu push notification FCM dan memperbarui laporan.
