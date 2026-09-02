# Arsitektur Integrasi IDCS ↔ EDCL

Dokumen ini menjelaskan alur pergerakan data dari **Sistem Legacy (IDCS)** ke **Sistem Baru (EDCL)**, serta bagaimana EDCL melaporkan kembali (Write-Back) status pengiriman ke IDCS secara *asynchronous* dan *real-time* tanpa membebani performa database.

---

## 🏗 Diagram Arsitektur

Berikut adalah diagram alur data keseluruhan (menggunakan pendekatan *Event-Driven Architecture* & *Change Data Capture*):

```mermaid
flowchart TD
    %% Define Styles
    classDef idcsSystem fill:#f9d0c4,stroke:#333,stroke-width:2px;
    classDef edclSystem fill:#d4e1f9,stroke:#333,stroke-width:2px;
    classDef broker fill:#d4f9d4,stroke:#333,stroke-width:2px;

    %% --------------------------------
    %% 1. IDCS BOUNDARY
    %% --------------------------------
    subgraph IDCS ["Sistem IDCS (Legacy)"]
        idcs_db[("Database IDCS\nSQL Server")]
        tbl_manifest["Tabel:\n- manifests\n- parts\n- kanbans"]
        tbl_delivery["Tabel:\n- edcl_delivery_status"]
        
        idcs_db --- tbl_manifest
        idcs_db --- tbl_delivery
    end
    class IDCS idcsSystem;

    %% --------------------------------
    %% 2. MESSAGE BROKER BOUNDARY
    %% --------------------------------
    subgraph Middleware ["Infrastruktur Pesan"]
        debezium(("Debezium\nCDC"))
        rabbitmq{"RabbitMQ"}
        
        debezium -- Membaca Trans. Log (Insert/Update) --> tbl_manifest
        debezium -- Publish Raw JSON --> rabbitmq
    end
    class Middleware broker;

    %% --------------------------------
    %% 3. EDCL BOUNDARY
    %% --------------------------------
    subgraph EDCL ["Sistem EDCL (Modern)"]
        worker_ingestion["EDCL.Worker.Ingestion\n(Native RabbitMQ Client)"]
        edcl_api["EDCL.Api\n(MassTransit Fault Consumer)"]
        edcl_db[("Database EDCL\nSQL Server 2022")]
        edcl_ui("Web / Mobile Client")
        
        worker_reporter["EDCL.Worker.Reporter\n(MassTransit Consumer)"]
        
        %% Ingestion Flow
        rabbitmq -- Consume CDC Event --> worker_ingestion
        worker_ingestion -- Insert / EF Core --> edcl_db
        worker_ingestion -- Publish DLQ Event\n(Jika Gagal 5x) --> rabbitmq
        rabbitmq -- Consume DLQ Event --> edcl_api
        edcl_api -- SSE (Server-Sent Events) --> edcl_ui
        
        %% Write-Back Flow
        edcl_api -- Publish\nManifestDeliveredEvent --> rabbitmq
        rabbitmq -- Consume Delivery Event --> worker_reporter
    end
    class EDCL edclSystem;

    %% --------------------------------
    %% WRITE-BACK CONNECTION
    %% --------------------------------
    worker_reporter -. "Raw Dapper INSERT" .-> tbl_delivery
```

---

## 📖 Narasi Alur Integrasi

Integrasi ini dibagi menjadi dua fase utama: **Fase Ingestion** (Masuk ke EDCL) dan **Fase Write-Back** (Kembali ke IDCS).

### A. Fase Ingestion (IDCS ➔ EDCL)

**Tantangan:** Data manifest beserta komponen-komponen anakannya (Parts, Kanbans, Skids) harus disinkronkan ke EDCL secara terus-menerus (real-time) tanpa membuat database IDCS kewalahan akibat proses *polling* (seperti yang terjadi pada Talend di masa lalu).

**Solusi:**
1. **Change Data Capture (CDC):** Kita menggunakan **Debezium** yang menempel langsung pada *Transaction Logs* (SQL Server Agent) di database IDCS. Setiap kali ada query `INSERT`, `UPDATE`, atau `DELETE` pada tabel Manifest/Parts di IDCS, Debezium langsung mengetahuinya secara pasif (tanpa perlu melempar query `SELECT`).
2. **Message Broker (RabbitMQ):** Debezium mengubah perubahan data tersebut menjadi format *Raw JSON* dan melemparkannya ke antrean **RabbitMQ**. RabbitMQ bertindak sebagai "Peredam Kejut" (*Shock Absorber*). Jika EDCL sedang mati atau *overload*, data tidak akan hilang; ia akan antre dengan aman di RabbitMQ.
3. **Dedicated Ingestion Worker:** Karena arus pesan CDC sangat padat, kita membuat *microservice* khusus `EDCL.Worker.Ingestion`. Demi efisiensi dan performa maksimal, worker ini **TIDAK menggunakan MassTransit**. Ia dibangun murni menggunakan `RabbitMQ.Client` *native* untuk mengonsumsi *Raw JSON* dan memprosesnya secara langsung.
4. **Penanganan Out-Of-Order (Delayed Retry & SSE):** Sering kali terjadi *Race Condition* di mana data anak (*Part*) datang sebelum induknya (*Manifest*), memicu *Error Foreign Key Constraint*.
   - **Delayed Retry (TTL + DLX):** Untuk mencegah *Head-of-Line Blocking*, Worker menangani kegagalan ini dengan melempar pesan yang bermasalah ke **Retry Queue** RabbitMQ khusus yang memiliki *Time-To-Live* (TTL). Jeda waktunya menggunakan pola *Exponential Backoff* (2s, 4s, 8s, 16s, 32s). Setelah TTL habis, pesan otomatis dilempar kembali ke antrean utama.
   - **DLQ & SSE:** Jika masih gagal setelah 5 percobaan, pesan dimasukkan ke *Dead Letter Queue* (`edcl_ingestion_faults`). Di sisi `EDCL.Api`, komponen `IngestionFaultConsumer` (yang ini menggunakan MassTransit) akan menangkap pesan gagal tersebut dan seketika menembakkan **Server-Sent Events (SSE)** ke Web Client untuk memperingatkan operator secara *real-time*.

---

### B. Fase Write-Back (EDCL ➔ IDCS)

**Tantangan:** Setelah barang berhasil diproses dan dikirimkan oleh EDCL, status penyelesaian tersebut harus dilaporkan kembali ke IDCS. Kita **tidak diizinkan** melakukan `UPDATE` langsung ke tabel asli IDCS karena berisiko merusak integritas *legacy system*.

**Solusi:**
1. **Tabel Serah-Terima (Integration Table):** Dibuat satu tabel khusus di database IDCS bernama `edcl_delivery_status`. Tabel ini sepenuhnya "dimiliki" oleh EDCL untuk menaruh laporan *Delivery*.
2. **Event Driven:** Saat sebuah pengiriman dinyatakan selesai di EDCL (misal melalui klik tombol "Delivered" oleh supir di aplikasi), API EDCL **tidak langsung** melakukan *query* ke database IDCS. API EDCL hanya akan menerbitkan pesan internal: `ManifestDeliveredIntegrationEvent` ke RabbitMQ.
3. **Dedicated Reporter Worker:** Sebuah *Microservice* kecil bernama **`EDCL.Worker.Reporter`** akan menangkap pesan integrasi tersebut.
4. **Eksekusi Dapper:** Worker inilah yang menyimpan *Secondary Connection String* (`ConnectionStrings:IdcsDb`) menuju database IDCS. Worker ini akan melempar raw SQL (`INSERT INTO edcl_delivery_status`) menggunakan Dapper (karena sangat cepat dan ringan).
5. **Keamanan & Isolasi:** Jika server IDCS sedang *down* saat penulisan balik, EDCL tidak akan *error*. Pesan `ManifestDeliveredIntegrationEvent` akan tetap mengantre di RabbitMQ sampai server IDCS hidup kembali dan `Worker.Reporter` berhasil menuliskan datanya.

---

## 🌐 Topologi Lingkungan: Local Simulation vs Production

Dalam implementasi nyata, IDCS dan EDCL berjalan pada infrastruktur server yang terpisah:

| Lingkungan | Mekanisme IDCS | Konfigurasi `IdcsDb` |
|---|---|---|
| **Local Development** | Disimulasikan menggunakan kontainer mandiri `idcs_sqlserver` di port **`1466`** melalui folder `idcs-seeder/` (`make idcs-up`). | `Server=host.docker.internal,1466;Database=IDCS;User Id=sa;Password=...` |
| **Staging / Production** | Berjalan di server database korporat terpisah (bukan kontainer lokal). Kontainer `idcs-seeder` tidak dijalankan. | `Server=idcs-db.corp.internal,1433;Database=IDCS;User Id=...;Password=...` (Disuntikkan via *Environment Variable* / Secret Manager). |

