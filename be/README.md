# EDCL Mini - Backend

EDCL Mini adalah proyek simulasi logistik (*Electronic Delivery Check List*) dengan pendekatan arsitektur tingkat lanjut (**Modular Monolith** & **Hexagonal Architecture**) yang dirancang agar siap bermigrasi ke **Microservices** kapan saja.

## 🏗️ Architecture & Stack

Sistem ini dirancang menggunakan standar *best practice* industri untuk .NET:
- **Framework**: .NET 10
- **Architecture**: Modular Monolith (Clean/Hexagonal Architecture per modul)
- **Pattern**: CQRS (menggunakan `MediatR`)
- **API Gateway**: YARP (Yet Another Reverse Proxy)
- **Database**: SQL Server (Entity Framework Core dengan skema terpisah per modul)
- **Cache**: Redis
- **Message Broker**: RabbitMQ (MassTransit)
- **Containerization**: Docker & Docker Compose (Multi-stage build, Alpine)

## 📂 Project Structure

Struktur kode sangat ketat untuk memastikan tidak ada *tight coupling* antar modul:

```text
be/
├── src/
│   ├── Gateway/               # API Gateway (YARP) - Single Point of Entry
│   ├── Host/                  # Main Web API (.NET Host)
│   ├── Modules/               # Fitur Utama (Auth, Job, Cargo, Driver, Notification)
│   └── Shared/                # Kernel, Interfaces, & Infrastructure
├── workers/                   # Background Services terpisah
│   ├── EDCL.Worker.Ingestion/ # Worker untuk konsumsi data awal
│   ├── EDCL.Worker.Outbox/    # Worker untuk pola Transactional Outbox
│   └── EDCL.Worker.Reporter/  # Worker untuk pelaporan / agregasi data
├── tests/                     # Tests (Unit, Integration, E2E) menggunakan xUnit, Moq, dan k6
├── docker-compose.yml         # Konfigurasi container service
├── Makefile                   # Shortcut eksekusi environment
└── edcl-api-requests.http     # Koleksi request untuk testing API via REST Client
```

> **Catatan Penting:** Modul hanya boleh berkomunikasi satu sama lain melalui kontrak *interface* yang ada di `Shared.Kernel` atau melalui *Event/Message* di RabbitMQ.

## 🚀 Getting Started

### Prerequisites
- Docker & Docker Compose (atau Podman)
- .NET 10 SDK (jika ingin *running* atau *debugging* manual tanpa docker)
- VS Code dengan REST Client (opsional, untuk *testing* API)

### Environment & Ports
Kredensial dan port diatur melalui file `.env` di folder `be/`. Daftar port yang dialokasikan:

| Service | Container Name | Host Port | Container Port | Deskripsi |
|---|---|---|---|---|
| **API Gateway** | `edcl_gateway` | **`5293`** | `8080` | Single Point of Entry (YARP Reverse Proxy) |
| **Backend API Host** | `edcl_api` | **`5140`** | `8080` | Modular Monolith Web API & SignalR |
| **SQL Server (EDCL)** | `edcl_sqlserver` | **`1444`** | `1433` | Database utama EDCL (skema terpisah per modul) |
| **Redis** | `edcl_redis` | **`6399`** | `6379` | Distributed Cache & Idempotency Lock |
| **RabbitMQ Broker** | `edcl_rabbitmq` | **`5672`** | `5672` | AMQP Message Broker & Delayed Retry |
| **RabbitMQ UI** | `edcl_rabbitmq` | **`15672`** | `15672` | Web Dashboard Management RabbitMQ |
| **Jaeger UI** | `edcl_jaeger` | **`16686`** | `16686` | Distributed Tracing UI Waterfall |
| **Prometheus** | `edcl_prometheus` | **`9090`** | `9090` | Metrik & Telemetri Scraper (`/metrics`) |
| **Debezium Server** | `edcl_debezium_server` | - | `8080` | CDC Engine (Membaca tx-log IDCS) |
| **Workers** | `edcl_worker_*` | - | - | Ingestion, Outbox, & Reporter Workers |

> 💡 **Integrasi Sistem Eksternal IDCS**: Database IDCS disimulasikan secara terpisah di port **`1466`** melalui folder `idcs-seeder/` (eksekusi `make idcs-up`). Backend EDCL terhubung ke IDCS via `host.docker.internal:1466` pada lingkungan lokal dan via connection string DNS/IP server IDCS pada lingkungan produksi.

### 🛠️ Makefile Commands (Quick Start)

Untuk mempermudah *development*, gunakan perintah `make` berikut di terminal dari dalam folder `be/` (atau gunakan target `make be-*` dari root direktori):

| Command | Description |
|---|---|
| `make up` | Membangun dan menjalankan seluruh stack backend (11 kontainer: DB, Redis, RabbitMQ, Gateway, API, Workers, Jaeger, Prometheus, Debezium). |
| `make down` | Menghentikan dan membersihkan semua kontainer backend EDCL. |
| `make infra-up` | **Hanya** menjalankan *Infrastructure* (SQL Server, Redis, RabbitMQ, Jaeger, Prometheus). Gunakan ini jika Anda ingin menjalankan `.NET API` & Workers secara native via IDE / terminal. |
| `make infra-down` | Menghentikan kontainer *Infrastructure*. |
| `make logs` | Menampilkan *live logs* dari semua service Docker. |

---

## 📡 API Endpoints & Health Checks

API Gateway berfungsi sebagai **Single Point of Entry** (Pintu Tunggal). Klien eksternal (Web Admin & Mobile Driver) dapat mengakses sistem melalui Gateway di `http://localhost:5293` atau langsung ke API di `http://localhost:5140`.

Endpoint penting yang dapat diakses:
- **Health Check**: `GET http://localhost:5293/health` atau `http://localhost:5140/health`
- **Scalar API Docs**: `GET http://localhost:5293/scalar/v1` atau `http://localhost:5140/scalar/v1`
- **Hangfire Dashboard**: `http://localhost:5293/hangfire` atau `http://localhost:5140/hangfire`
- **Prometheus Metrics**: `GET http://localhost:5140/metrics`
- **Jaeger Distributed Tracing UI**: `http://localhost:16686`
- **RabbitMQ Management Dashboard**: `http://localhost:15672` (User: `guest`, Pass: `guest`)

---

## 🧪 Testing

Proyek ini dilengkapi dengan *suite testing* yang komprehensif menggunakan **xUnit**, **Moq**, **Testcontainers**, dan **k6**.

### Menjalankan Test

- **Unit Test**: 
  ```bash
  dotnet test tests/EDCL.UnitTests/
  ```
- **Integration Test** (Menggunakan Testcontainers):
  ```bash
  dotnet test tests/EDCL.IntegrationTests/
  ```
- **End-to-End (E2E) Test**:
  ```bash
  dotnet test tests/EDCL.E2ETests/
  ```
- **Performance / Load Test**: Skrip uji beban dan performa berada di folder `tests/k6/` (dijalankan menggunakan *tool* k6).
