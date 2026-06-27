# IDCS Seeder

IDCS Seeder adalah sebuah proyek simulasi (Console Application .NET) yang bertujuan untuk meniru _Behavior_ sistem IDCS secara langsung dengan cara melakukan *Data Manipulation (Insert)* ke dalam tabel-tabel di Database IDCS.

Proyek ini sangat penting untuk melakukan **End-to-End (E2E) Testing** bagi mekanisme **Change Data Capture (CDC)** menggunakan **Debezium** yang terhubung dengan **MassTransit (RabbitMQ)** di backend `EDCL.Api`.

## 🛠 Prerequisites
- Docker & Docker Compose
- .NET 8 SDK

## 🚀 Panduan Eksekusi
Anda dapat menjalankan seluruh perintah di bawah ini melalui terminal di folder *root* proyek (menggunakan `Makefile`):

### 1. Menjalankan IDCS Database (SQL Server)
```bash
make idcs-up
```
Perintah ini akan menyalakan container `idcs_sqlserver` (menggunakan port `1466`) sesuai konfigurasi di file `docker-compose-idcs.yml`. 
*Note: SQL Server Agent secara otomatis dinyalakan (`MSSQL_AGENT_ENABLED: "True"`) karena kapabilitas ini dibutuhkan oleh Debezium.*

### 2. Inisialisasi Skema Database IDCS
```bash
make seed-init
```
Akan membuat database `IDCS` serta seluruh tabel simulasi yang dibutuhkan (`manifests`, `manifest_parts`, `manifest_kanbans`, `manifest_skids`).

### 3. Simulasi Ingestion (E2E Testing)
- **`make seed-manifest`**: Mengirim data Parent (Manifest).
- **`make seed-part`**: Mengirim data Child (Part) dengan ID parent terbaru.
- **`make seed-kanban`**: Mengirim data Kanban dengan ID parent terbaru.
- **`make seed-skid`**: Mengirim data Skid dengan ID parent terbaru.

### 4. Simulasi Out-Of-Order (Error & Retry Testing)
```bash
make seed-out-of-order
```
Skenario ini akan dengan sengaja menyisipkan Part dengan `ManifestId` fiktif yang tidak ada di dalam EDCL. 
Skenario ini berfungsi untuk memvalidasi:
1. **Exponential Backoff Retry**: MassTransit akan mencoba *retry* selama beberapa detik.
2. **IngestionFaultConsumer & SSE**: Pesan yang tetap gagal di-*retry* akan dibuang ke Fault Consumer, lalu dilempar sebagai notifikasi *realtime* (Server-Sent Events) ke Web Client.

---
**Catatan**: Proyek ini sengaja dibuat sejajar (*root level*) dengan *backend* (`be`), *frontend* (`fe`), dan `docs` agar tidak merancukan _test suite_ internal dari API EDCL.
