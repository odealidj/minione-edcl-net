# IDCS Seeder

IDCS Seeder adalah sebuah proyek simulasi (Console Application .NET) yang bertujuan untuk meniru _Behavior_ sistem IDCS secara langsung dengan cara melakukan *Data Manipulation (Insert)* ke dalam tabel-tabel di Database IDCS.

Proyek ini sangat penting untuk melakukan **End-to-End (E2E) Testing** bagi mekanisme **Change Data Capture (CDC)** menggunakan **Debezium** yang terhubung dengan **MassTransit (RabbitMQ)** di backend `EDCL.Api`.

## 🛠 Prerequisites
- Docker & Docker Compose
- .NET 8 SDK

## 🚀 Panduan Eksekusi
Anda dapat menjalankan seluruh perintah di bawah ini melalui terminal di dalam folder `idcs-seeder` (menggunakan `Makefile`):

```bash
cd idcs-seeder
```

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

### 4. Simulasi Out-Of-Order & Retry (Race Condition Testing)

Dalam kenyataan, ketika sebuah sistem *legacy* menyisipkan (Insert) Manifest dan Part dalam waktu yang nyaris bersamaan (misal 1 milidetik), *Message Broker* bisa saja mengirimkan data Part mendahului Manifest-nya. Untuk mensimulasikan **kondisi balapan (Race Condition)** secara nyata dan terukur tanpa mengandalkan kebetulan, kami menggunakan dua skenario khusus:

#### A. Out-of-Order Permanen (Fault)
```bash
make seed-out-of-order
```
Skenario ini dengan sengaja menyisipkan Part dengan `ManifestId` fiktif yang tidak pernah ada di dalam EDCL. 
**Tujuan:** Memvalidasi bahwa jika Exponential Backoff Retry gagal berulang kali, pesan akan dibuang ke *Fault Consumer*, lalu dilempar sebagai notifikasi *real-time* (Server-Sent Events) ke Web Client.

#### B. Race Condition Aktual (Retry Success)
```bash
make seed-race-condition
```
Skenario ini melakukan langkah-langkah presisi berikut:
1. Menentukan ID Manifest fiktif untuk masa depan.
2. Melakukan *Insert* Part menggunakan ID masa depan tersebut.
3. Sengaja menunda eksekusi selama **3 detik** (Dalam masa ini, MassTransit di EDCL akan menangkap pesan, gagal mencari *Parent*-nya, dan mulai menghitung waktu Retry).
4. Setelah jeda, skrip akan melakukan *Insert* Manifest dengan ID masa depan tersebut secara paksa (`IDENTITY_INSERT`).
**Tujuan:** Membuktikan bahwa MassTransit EDCL tidak langsung menggugurkan pesan Part, melainkan berhasil **Sukses pada Retry Berikutnya** setelah Parent-nya akhirnya terdaftar.

---
**Catatan**: Proyek ini sengaja dibuat sejajar (*root level*) dengan *backend* (`be`), *frontend* (`fe`), dan `docs` agar tidak merancukan _test suite_ internal dari API EDCL.
