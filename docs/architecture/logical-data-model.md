# Logical Data Model (EDCL Mini)

Dokumen ini mendeskripsikan model data logis dari aplikasi **EDCL Mini**. Model ini dikelompokkan ke dalam beberapa *domain* berdasarkan tanggung jawab bisnisnya.

## 1. Entity-Relationship Diagram (ERD) Inti

Diagram di bawah menggambarkan relasi antar entitas utama menggunakan standar *Crow's foot notation*.

```mermaid
erDiagram
    %% =======================
    %% MASTER DATA DOMAIN
    %% =======================
    TRANSPORTER ||--o{ DRIVER : "memiliki"
    TRANSPORTER {
        bigint id PK
        string name
    }

    TRUCK {
        bigint id PK
        string plate_number
        string vehicle_type
        boolean is_active
    }

    SUPPLIER {
        bigint id PK
        string supplier_code
        string name
        string address
        float latitude
        float longitude
        int geofence_radius_meters
        boolean is_active
    }

    %% =======================
    %% AUTH & USER DOMAIN
    %% =======================
    ROLE ||--o{ APP_USER : "memiliki"
    ROLE {
        bigint id PK
        string code
        string name
        string description
        boolean is_active
    }

    APP_USER {
        bigint id PK
        string name
        string email
        string password_hash
        bigint role_id FK
        boolean is_active
    }

    DRIVER ||--o{ DRIVER_PHONE_HISTORY : "mencatat histori"
    DRIVER ||--o{ DRIVER_NOTIFICATION : "menerima"
    DRIVER {
        bigint id PK
        string nik
        string name
        string phone_number
        string pin_hash
        boolean must_change_pin
        string fcm_token
        string photo_url
        boolean is_active
        bigint transporter_id FK
    }

    %% =======================
    %% INGESTION DOMAIN (Data Eksternal)
    %% =======================
    SUPPLIER ||--o{ MANIFEST : "sumber dari"
    MANIFEST ||--o{ MANIFEST_PART : "terdiri dari"
    MANIFEST ||--o{ MANIFEST_SKID : "memiliki"
    MANIFEST ||--o{ MANIFEST_KANBAN : "memiliki"
    
    MANIFEST {
        bigint id PK
        string manifest_no
        string supplier_code FK
        string supplier_name
        int sequence
        string order_type
        string order_no
        string dock_code
        string p_lane_no
        datetime pick_date
        string cycle
        string status
    }

    MANIFEST_PART {
        bigint id PK
        bigint manifest_id FK
        string part_no
        string part_name
        int qty
        string kanban_no
        string uniq_no
        string box_type
        string status
    }

    MANIFEST_SKID {
        bigint id PK
        bigint manifest_id FK
        string skid_no
    }

    MANIFEST_KANBAN {
        bigint id PK
        bigint manifest_id FK
        string part_no
        string kanban_cd
    }

    INGESTION_ERROR {
        bigint id PK
        string event_type
        string payload
        string error_message
        string stack_trace
        datetime occurred_at
        boolean is_resolved
    }

    %% =======================
    %% NOTIFICATION DOMAIN
    %% =======================
    DRIVER_NOTIFICATION {
        bigint id PK
        bigint driver_id FK
        string title
        string message
        boolean is_read
        string type
        bigint pickup_order_id
    }

    %% =======================
    %% TRANSACTIONAL DOMAIN (Delivery)
    %% =======================
    DRIVER ||--o{ PICKUP_ORDER : "ditugaskan ke"
    TRUCK ||--o{ PICKUP_ORDER : "menggunakan"
    
    PICKUP_ORDER ||--o{ PICKUP_ORDER_DETAIL : "mempunyai rute"
    PICKUP_ORDER {
        bigint id PK
        string po_no
        date pickup_date
        string route_code
        string cycle_code
        time estimated_departure_time
        datetime started_at
        datetime completed_at
        string status
        string hangfire_job_id_h1
        string hangfire_job_id_h30
        bigint driver_id FK
        bigint truck_id FK
    }

    SUPPLIER ||--o{ PICKUP_ORDER_DETAIL : "merupakan titik jemput"
    PICKUP_ORDER_DETAIL ||--o{ PICKUP_ORDER_MANIFEST : "menjemput manifest"
    PICKUP_ORDER_MANIFEST ||--o{ PICKUP_ORDER_KANBAN : "menjemput kanban"
    
    PICKUP_ORDER_DETAIL {
        bigint id PK
        bigint pickup_order_id FK
        bigint supplier_id FK
        int sequence
        datetime arrived_at
        datetime picked_up_at
        string status
    }

    PICKUP_ORDER_MANIFEST {
        bigint id PK
        bigint pickup_order_detail_id FK
        string manifest_no
        string order_type
        int total_skid
        string dock_code
        int total_kanban
        int scanned_kanban
        string status
    }

    PICKUP_ORDER_KANBAN {
        bigint id PK
        bigint pickup_order_manifest_id FK
        string kanban_code
        datetime scanned_at
        string status
    }
```

---

## 2. Definisi Domain

Untuk mempermudah pemahaman arsitektur *Modular Monolith*, data dibagi ke dalam 4 pilar utama:

### A. Master Data Domain
Berisi entitas dasar yang jarang berubah dan sering menjadi rujukan tabel lain.
- **Transporter**: Perusahaan/Vendor penyedia logistik (misal: Hikari Logistics).
- **Truck**: Data armada fisik beserta plat nomornya (misal: Wingbox B 9607 PXT).
- **Supplier**: Entitas penyuplai barang atau pabrik tujuan.

### B. Auth & User Domain
Menyimpan kredensial dan entitas akses (Identity).
- **AppUser**: Pengguna portal *Web Backoffice* (biasanya Admin atau HRD). Login menggunakan *Email* dan *Password*.
- **Role**: Hak akses untuk `AppUser` (e.g., `ADMIN`, `USER`).
- **Driver**: Supir yang mengoperasikan *Mobile App*. Data ini unik karena mencampur profil (NIK, Nama) dan kredensial akses (No. HP, PIN Hash).
- **DriverPhoneHistory**: *Audit trail* otomatis (biasanya via SQL Trigger) ketika nomor HP Driver diganti.

### C. Ingestion Domain (Sinkronisasi Sistem Luar)
Tabel-tabel di domain ini tidak dimanipulasi secara manual, melainkan diisi (sinkronisasi) via Change Data Capture (Debezium CDC) dari IDCS (sistem eksternal).
- **Manifest**: Lembar pengiriman logistik utama dari IDCS.
- **ManifestPart**: Rincian suku cadang (*Part Number*) dan jumlah (*Qty*) di dalam sebuah Manifest.
- **ManifestSkid**: Data fisik wadah muatan (Palet/Skid).
- **ManifestKanban**: Data spesifik kartu kanban individual yang akan dipindai secara fisik.
- **IngestionError**: *Dead Letter Queue* (DLQ) persistent. Menyimpan data yang gagal disinkronkan akibat anomali seperti *Race Condition* atau duplikasi. Digunakan untuk proses pemulihan (Retry) dan notifikasi UI (Server-Sent Events).

### D. Transactional / Operational Domain
Ini adalah domain yang paling aktif, digunakan saat alur pengiriman berlangsung di lapangan.
- **PickupOrder**: Rencana kerja seorang Driver. Memetakan *Driver* A, menggunakan *Truck* B, pada *Tanggal* C, dengan status siklus keberangkatan (Rute + Cycle Code).
- **PickupOrderDetail**: Mewakili "Titik Singgah" (Stops) di dalam rute `PickupOrder`. Menyimpan urutan singgah (*sequence*), jejak rekam kedatangan/keberangkatan aktual (*arrived_at*, *picked_up_at*), dan menghubungkan dengan *Supplier* mana yang didatangi.
- **PickupOrderManifest**: Ceklis manifest yang harus dipindai/diangkut di suatu *Titik Singgah*. Selain menyimpan status pencapaian (silang merah / centang hijau) dan jumlah kanban, entitas ini juga sengaja menduplikasi (*event-driven data duplication*) beberapa atribut master seperti `order_type`, `total_skid`, dan `dock_code`. Hal ini adalah **Best Practice Microservices** agar modul `Job` dapat menyuplai antarmuka UI secara instan (melalui API tunggal) tanpa melakukan komputasi JOIN yang mahal atau menembakkan panggilan HTTP sinkron lintas layanan ke modul *Master Data*.
- **PickupOrderKanban**: Ceklis kanban individual dari manifest di atas. Ini adalah target utama yang dipindai (di-*scan*) oleh kamera *Mobile App* si Driver.

### E. Notification Domain
- **DriverNotification**: Berfungsi sebagai "Kotak Masuk" (*Inbox*) pesan untuk supir. Pesan seperti penugasan rute baru akan tersimpan di sini agar logo bel di aplikasi berubah menjadi merah jika belum dibaca (`is_read = 0`).

---

## 3. Best Practices yang Diterapkan di Database Ini
> [!TIP]
> 1. **Audit Columns**: Setiap tabel menyertakan kolom jejak rekam (`created_at`, `created_by`, `updated_at`, `deleted_at`) serta mengadopsi mekanisme *Soft-Delete* (`is_deleted` = 1).
> 2. **Global Query Filters**: Pada tingkat Entity Framework Core, data yang ter-*soft-delete* otomatis disembunyikan.
> 3. **Concurrency Control**: Terdapat kolom `row_version` untuk mencegah modifikasi data ganda secara bersamaan (Optimistic Concurrency).
> 4. **No Hard-Deletes for Auth**: Karena `Driver` berelasi kuat ke `PickupOrder`, jika Supir *resign*, statusnya cukup diubah menjadi `IsActive = 0`, sehingga data riwayat kerjanya di masa lalu tetap terjaga dengan utuh.
