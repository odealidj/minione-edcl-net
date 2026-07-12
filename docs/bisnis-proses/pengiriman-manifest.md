# Dokumen Proses Bisnis: Pengiriman Manifest (IDCS ke EDCL)

Dokumen ini menjelaskan alur proses bisnis end-to-end dari pembuatan Manifest di sistem IDCS hingga pengiriman selesai oleh Driver melalui sistem EDCL. Dokumen disusun dalam beberapa tingkatan (level) untuk memudahkan pemahaman bagi berbagai peran di dalam tim.

---

## 1. Glosarium (Kamus Istilah)
- **IDCS (Inventory & Delivery Control System):** Sistem legacy pusat tempat pesanan (manifest, part, kanban) diproduksi dan diterbitkan. Bertindak sebagai *Source of Truth* awal.
- **EDCL (Electronic Delivery Control Log):** Sistem logistik modern berbasis web dan mobile untuk pelacakan, penugasan, dan pengontrolan pengiriman secara elektronik.
- **Manifest:** Dokumen surat jalan yang berisi daftar muatan barang (part, kanban, skid) yang harus diambil dari *Supplier* dan diantar ke pabrik.
- **Logistic Partner:** Perusahaan vendor logistik pihak ketiga (contoh: Hikari Logistics) yang menyediakan armada truk dan supir.
- **CDC (Change Data Capture):** Mekanisme *real-time* di belakang layar yang memantau perubahan data di IDCS dan mengirimkannya ke EDCL tanpa mengganggu kinerja IDCS.

---

## 2. Level 1: Alur Bisnis Konseptual (High-Level)
*Diperuntukkan bagi Manajemen & Stakeholder.*

Alur logistik secara konseptual sangat sederhana: Sistem IDCS menerbitkan perintah ambil barang, EDCL menugaskan tim lapangan, dan tim lapangan mengeksekusinya.

```mermaid
flowchart LR
    A[1. IDCS: Terbitkan\nManifest] --> B[2. Sinkronisasi\nOtomatis ke EDCL]
    B --> C[3. EDCL: Logistic Partner\nMenugaskan Driver]
    C --> D[4. Driver:\nAmbil & Antar Barang]
    D --> E[5. EDCL: Laporan\nSelesai ke IDCS]
    
    style A fill:#f9f,stroke:#333,stroke-width:2px
    style E fill:#f9f,stroke:#333,stroke-width:2px
    style B fill:#bbf,stroke:#333,stroke-width:2px
    style C fill:#bbf,stroke:#333,stroke-width:2px
    style D fill:#bbf,stroke:#333,stroke-width:2px
```

---

## 3. Level 2: User Journey & Alur Operasional (Swimlane)
*Diperuntukkan bagi Tim Operasional, Vendor (Logistic Partner), dan UI/UX.*

Bagian ini memetakan interaksi manusia dengan layar aplikasi (UI) serta perpindahan tanggung jawab (*hand-off*) antar departemen.

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Admin IDCS
    actor Vendor as Logistic Partner (Web)
    actor Driver as Driver (Mobile App)
    participant EDCL as Sistem EDCL

    Admin->>Admin: Membuat Manifest Baru di IDCS
    note right of Admin: Data otomatis mengalir ke EDCL<br/>tanpa input manual.
    EDCL-->>Vendor: Muncul di Dashboard EDCL (Status: Pending)
    
    Vendor->>EDCL: Assign (Pilih) Driver & Truck untuk Manifest
    EDCL-->>Driver: Kirim Notifikasi Push (FCM) ke HP Driver
    
    Driver->>EDCL: Buka Aplikasi, klik "Mulai Perjalanan"
    EDCL-->>Vendor: Status berubah menjadi "On The Way"
    
    Driver->>Driver: Tiba di Supplier, muat barang, jalan ke Pabrik
    
    Driver->>EDCL: Klik "Selesai / Delivered"
    EDCL-->>Vendor: Status berubah menjadi "Delivered"
    note left of EDCL: EDCL akan sinkronisasi balik (write-back)<br/>status ke sistem IDCS.
```

---

## 4. Level 3: Urutan Teknis Integrasi (Technical Sequence)
*Diperuntukkan bagi Developer & Arsitek Sistem.*

Menjelaskan secara presisi bagaimana data mengalir (streaming) di bawah kap mesin (under the hood) menggunakan arsitektur Event-Driven.

```mermaid
sequenceDiagram
    autonumber
    participant SQL_IDCS as SQL Server (IDCS)
    participant DBZ as Debezium (CDC)
    participant RMQ as RabbitMQ
    participant Ingestion as Ingestion Worker (EDCL)
    participant SQL_EDCL as SQL Server (EDCL)

    SQL_IDCS->>SQL_IDCS: INSERT INTO manifests
    SQL_IDCS->>DBZ: SQL Server Transaction Log (CDC)
    DBZ->>RMQ: Publish Event (Topic: idcs.manifests)
    RMQ->>Ingestion: Consume Event (Manifest Created)
    
    Ingestion->>Ingestion: Validasi & Transformasi Data
    
    alt Jika Data Valid
        Ingestion->>SQL_EDCL: MERGE INTO edcl.manifests (Upsert)
        SQL_EDCL-->>Ingestion: OK
        Ingestion->>RMQ: ACK (Pesan dihapus dari antrean)
    else Jika Data Gagal / Ketergantungan Hilang
        Ingestion->>SQL_EDCL: Catat ke tabel ingestion_errors
        Ingestion->>RMQ: NACK (Pesan masuk antrean Retry/Dead Letter)
    end
```

---

## 5. Skenario Pengecualian Bisnis (Edge Cases)

Sistem telah dirancang untuk tahan banting (resilient) terhadap kondisi tidak ideal di lapangan:

1. **Bagaimana jika IDCS menerbitkan part barang sebelum surat Manifest-nya dibuat? (Out of Order Data)**
   - **Solusi Sistem:** EDCL Worker akan menangkap error ketidaklengkapan relasi. Pesan tersebut tidak dibuang, melainkan dimasukkan ke antrean *Retry* selama beberapa detik. Begitu induk Manifest tiba, sistem akan memproses ulang barang tersebut secara otomatis hingga sukses.

2. **Bagaimana jika Driver lupa kata sandi (PIN)?**
   - **Solusi Sistem:** Saat pendaftaran awal, Admin EDCL membuatkan PIN Default (123456). Jika supir lupa PIN barunya nanti, Admin dapat mereset *driver* ke kondisi PIN default kembali, dan Driver wajib menggantinya saat masuk berikutnya.

3. **Bagaimana jika aplikasi Mobile Driver kehilangan sinyal (Offline) saat "Delivered"?**
   - **Solusi Sistem:** (*Future Enhancement*) Aplikasi Mobile menyimpan status "Delivered" secara lokal (SQLite/Room). Begitu sinyal internet kembali (Online), aplikasi otomatis melakukan sinkronisasi dengan EDCL Backend.
