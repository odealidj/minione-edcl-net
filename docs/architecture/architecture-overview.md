# Architecture Overview (EDCL Mini)

Dokumen ini menjelaskan arsitektur tingkat tinggi dari **EDCL Mini** (Electronic Delivery Check List). Sistem ini dibangun menggunakan arsitektur **Modular Monolith** dengan pola **Hexagonal Architecture** dan **CQRS** (Command Query Responsibility Segregation). 

Pendekatan ini dipilih agar sistem mudah di-maintenance, memiliki batasan (*boundaries*) yang tegas antar domain, namun tetap siap (*ready*) jika di masa depan perlu dipecah menjadi **Microservices**.

---

## 1. Topologi Sistem & Diagram Arsitektur

Berikut adalah gambaran besar bagaimana komponen-komponen dalam EDCL Mini berinteraksi satu sama lain:

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

### A. API Gateway (YARP)
- Bertindak sebagai **Single Point of Entry** bagi seluruh klien luar (Web & Mobile).
- Mengisolasi arsitektur internal; klien tidak perlu tahu port internal masing-masing modul/worker.
- Menangani sanitasi *forwarded headers* (`X-Forwarded-For`, `X-Forwarded-Proto`).

### B. Modular Monolith & Domain Isolation
Aplikasi inti yang membungkus beberapa modul independen (`Auth`, `Job`, `Cargo`, `Driver`, `Notification`).
- **Strict Boundaries**: Modul tidak boleh me-*reference* modul lain secara langsung. Komunikasi silang (*cross-domain*) dilakukan secara elegan melalui interface/port di *Shared Kernel* (contoh: `IDriverPort`, `ISupplierPort`). Di fase monolith, ini dieksekusi secara *in-memory* via *Dependency Injection*. Saat migrasi ke *microservices*, port tersebut cukup di-inject dengan *HTTP/gRPC Client* tanpa mengubah *business logic* dari modul pemanggil.
- **CQRS**: Menggunakan `MediatR` untuk memisahkan *Command* (operasi tulis/ubah data) dan *Query* (operasi baca data). Hal ini mempercepat performa *read* dan mengamankan *write*.
- **Database Schema per Module (Microservices Ready)**: Meski secara fisik menggunakan 1 Database (`EDCLMini`), setiap modul memiliki skema (*schema*) SQL Server yang terpisah (contoh: `auth`, `job`, `cargo`, `driver`, `notification`, `ingestion`). Yang paling krusial, **tidak ada Foreign Key constraint antar skema** (misal: Modul Job hanya menyimpan `long DriverId` bukan navigasi objek relasional ke skema Auth). Hal ini membuat migrasi ke *microservices* semudah mengekspor skema ke database fisik terpisah.

### C. Infrastruktur Pendukung
- **SQL Server 2022**: Relational Database Management System utama.
- **Redis 7.2**: Digunakan untuk *Distributed Caching* (mempercepat pengambilan data statis) dan menyimpan status *Idempotency Key* (mencegah *request* duplikat jika aplikasi klien kehilangan koneksi jaringan).
- **RabbitMQ 3.13**: *Message Broker* andalan kita untuk komunikasi *Asynchronous* antar layanan, menggunakan pustaka `MassTransit` dengan *5-Stage Delayed Retry* dan *Dead Letter Exchange (DLX)*.

### D. Background Workers
Pemisahan beban kerja berat agar tidak memblokir antarmuka API klien:
1. **Worker.Ingestion**: Ujung tombak integrasi data dari sistem *Legacy* (IDCS) via **Change Data Capture (Debezium CDC)**.
2. **Worker.Outbox**: Menjamin **Eventual Consistency** melalui *Transactional Outbox Pattern*.
3. **Worker.Reporter**: Mengolah *events* yang dilempar oleh Outbox untuk membangun data laporan (*Read Models*).
4. **Worker.GpsTracker**: Mengelola tracking GPS armada, simulasi OSRM, geofencing, dan scheduler Hangfire periodik.

### E. Observability & SRE Golden Signals
Sistem mengimplementasikan instrumentasi terpadu:
- **OpenTelemetry .NET SDK**: Menyediakan OTLP provider untuk Tracing dan Metrics.
- **Jaeger (`:16686`)**: Visualisasi *distributed trace waterfall* melacak alur HTTP $\to$ MediatR $\to$ SQL $\to$ Redis $\to$ RabbitMQ.
- **Prometheus (`:9090`)**: Scraper otomatis terhadap endpoint `/metrics` untuk metrik runtime dan domain logistik.
- **System Observability Dashboard (`/admin/system-observability`)**: Menampilkan live resource metrics, Service Memory Breakdown Donut Chart, dan Full-Stack Capacity Sizing Guide.

### F. Testing Architecture (Piramida Pengujian)
- **Unit Tests**: 31 Tests (xUnit, FluentAssertions, Moq) untuk domain logic murni.
- **Integration Tests (Testcontainers)**: Menjalankan kontainer Docker nyata (SQL Server, Redis, RabbitMQ) saat pengujian endpoint API.
- **End-to-End Tests**: `DriverJourney_E2ETest.cs` menguji alur login, start job, arrive, scan kanban, complete stop, hingga finish trip.
- **Load Testing (k6)**: Benchmark skenario konkurensi supir dengan latensi P95 48.15 ms dan 0% failure rate.

---

## 3. Narasi Alur Eksekusi (Contoh Kasus: Complete Stop)

Untuk memahami bagaimana arsitektur ini bekerja dari perspektif *request* klien hingga *database* dan *message broker*, berikut adalah urutan eksekusinya:

### Skenario: Driver menyelesaikan rute pemberhentian (Complete Stop)

1. **Inisiasi Klien (Mobile):** Driver menekan tombol "Selesai" di aplikasi. Aplikasi klien mengirim HTTP POST `/api/v1/jobs/stops/1/complete` yang dilengkapi *JWT Token* dan `X-Idempotency-Key` ke API Gateway (Port `5293`).
2. **Routing (Gateway):** Gateway menerima *request* tersebut dan meneruskannya ke `EDCL.Api` (Port internal `5140`).
3. **Validasi Idempotency & Auth:** Pipeline `EDCL.Api` mengecek JWT dan mengecek `X-Idempotency-Key` di **Redis**. Jika *request* ini duplikat (misal user memencet tombol 2 kali secara cepat), API langsung membalas sukses tanpa mengeksekusi ulang kode di bawahnya.
4. **Command Execution (CQRS):** Request dipetakan menjadi `CompleteStopCommand` lalu ditangkap oleh *Handler* di Modul `Job`.
5. **Database Transaction (Unit of Work):** 
   - *Handler* mengubah status `Stop` menjadi `Completed` di memori.
   - *Handler* membuat `StopCompletedEvent` dan menambahkannya ke entitas.
   - Entity Framework menyimpannya ke SQL Server (Schema `job`). Bersamaan dengan itu (dalam 1 Transaksi Database yang sama), *Domain Event* tadi di-serialize menjadi JSON dan dimasukkan ke dalam tabel **Outbox**.
   - Ini memastikan prinsip ACID: Jika perubahan status gagal, *Event* tidak akan masuk Outbox.
6. **Respons Sinkron (Instan):** Setelah *commit* ke DB selesai, `EDCL.Api` segera mengembalikan HTTP `200 OK` ke klien.
7. **Relay Asinkron (Worker Outbox):** Di *background*, `Worker.Outbox` yang melakukan *polling* ke SQL Server menyadari ada pesan baru di tabel Outbox. Worker ini mengambil pesan tersebut dan mem-*publish*-nya ke **RabbitMQ**. Jika berhasil ter-*publish*, pesannya ditandai sebagai *Processed* di database.
8. **Reaksi Lanjutan (Worker Reporter / Notif):** Pesan di RabbitMQ didengarkan oleh `Worker.Reporter` atau Modul `Notification`. Mereka secara asinkron (tanpa mengganggu *driver*) memproses pesan tersebut untuk membuat rekap laporan *delay* atau men-*trigger* Push Notification ke sistem pusat.
