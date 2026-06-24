# EDCL Mini - Backend

EDCL Mini adalah proyek simulasi logistik (*Express Delivery Cargo Logistic*) dengan pendekatan arsitektur tingkat lanjut (**Modular Monolith** & **Hexagonal Architecture**) yang dirancang agar siap bermigrasi ke **Microservices** kapan saja.

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
│   ├── Modules/               # Fitur Utama (Auth, Job, Cargo, Notification)
│   └── Shared/                # Kernel, Interfaces, & Infrastructure
├── workers/                   # Background Services terpisah
│   ├── EDCL.Worker.Ingestion/ # Worker untuk konsumsi data awal
│   ├── EDCL.Worker.Outbox/    # Worker untuk pola Transactional Outbox
│   └── EDCL.Worker.Reporter/  # Worker untuk pelaporan / agregasi data
├── tests/                     # Unit Tests menggunakan xUnit & Moq
├── docker-compose.yml         # Konfigurasi container service
└── Makefile                   # Shortcut eksekusi environment
```

> **Catatan Penting:** Modul hanya boleh berkomunikasi satu sama lain melalui kontrak *interface* yang ada di `Shared.Kernel` atau melalui *Event/Message* di RabbitMQ.

## 🚀 Getting Started

### Prerequisites
- Docker & Docker Compose (atau Podman)
- .NET 10 SDK (jika ingin *running* atau *debugging* manual tanpa docker)
- VS Code dengan REST Client (opsional, untuk *testing* API)

### Environment
Kredensial dan port diatur melalui file `.env`. Pastikan Anda sudah membuat salinan dari template jika ada, atau pastikan file `.env` berada di folder `be/` dengan isi seperti ini:
```env
SA_PASSWORD=YourStrong@Passw0rd
SQL_SERVER_PORT=1444
REDIS_PORT=6399
RABBITMQ_PORT=5672
RABBITMQ_UI_PORT=15672
API_PORT=5140
GATEWAY_PORT=5293
```

### 🛠️ Makefile Commands (Quick Start)

Untuk mempermudah *development*, gunakan perintah `make` berikut di terminal dari dalam folder `be/`:

| Command | Description |
|---|---|
| `make up` | Membangun dan menjalankan seluruh *environment* (Database, Redis, RabbitMQ, Gateway, API, dan Workers). |
| `make down` | Menghentikan dan menghapus semua container. |
| `make infra-up` | **Hanya** menjalankan *Infrastructure* (SQL Server, Redis, RabbitMQ). Gunakan ini jika Anda ingin melakukan *debugging* `.NET API` secara manual via IDE. |
| `make infra-down` | Menghentikan container *Infrastructure*. |
| `make logs` | Menampilkan *live logs* dari semua service Docker. |

---

## 📡 API Endpoints & Health Checks

API Gateway berfungsi sebagai **Single Point of Entry** (Pintu Tunggal). Jangan pernah mengakses Host API secara langsung.
Secara default, Gateway berjalan di `http://localhost:5293`.

Anda dapat mengecek kesehatan seluruh komponen sistem (API, Database, Redis, RabbitMQ) dengan mengakses *endpoint* berikut melalui Gateway:
```http
GET http://localhost:5293/health
```

Untuk daftar lengkap API Request yang didukung, silakan lihat file `EDCL.Gateway.http` di dalam folder `src/Gateway/EDCL.Gateway/`.

---

## 🧪 Testing

Proyek ini dilengkapi dengan Unit Test menggunakan **xUnit**. Untuk menjalankan test:
```bash
dotnet test tests/EDCL.UnitTests/
```
