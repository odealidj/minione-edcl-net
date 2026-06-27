# Dokumen Teknis: Detail Pengiriman (Kanban Scanning System)

Dokumen ini memaparkan spesifikasi teknis dan interaksi arsitektural yang terjadi pada layar **Detail Pengiriman** atau *Kanban Scanner*.

## 1. Spesifikasi Teknis (Endpoint Requirement)

Karena pada tahap sebelumnya aplikasi Mobile hanya menerima agregasi total Kanban, layar Detail Pengiriman memerlukan **Endpoint Baru** untuk menarik rincian baris demi baris per Manifest.

### A. Endpoint Baru: Get Manifests by Stop
- **URL:** `GET /api/v1/jobs/stops/{stopId}/manifests`
- **Tujuan:** Menampilkan tabel rincian manifest (Manifest No, Type, Total SKID, Dock Code, No of Kanban, Status).
- **Behavior:** Backend harus menghitung `scannedKanban` secara *real-time* dengan membandingkan jumlah `PickupOrderKanban` yang berelasi dengan `PickupOrderManifest` tersebut. Jika `scannedKanban == totalKanban`, status di UI akan menjadi hijau (Complete).

### B. Endpoint Pemindaian (Sudah Ada)
- **URL:** `POST /api/v1/jobs/stops/{stopId}/manifests/{manifestId}/kanban`
- **Tujuan:** Menerima *string* barcode (`KanbanCode`) dari kamera.
- **Behavior:** Backend memvalidasi eksistensi manifest, kemudian melakukan *Insert* ke tabel `PickupOrderKanban` dengan *timestamp* saat ini (`ScannedAt = DateTime.UtcNow`).

### C. Endpoint Penguncian / Selesai (Sudah Ada)
- **URL:** `POST /api/v1/jobs/stops/{stopId}/complete`
- **Tujuan:** Dieksekusi dari *slider* "Geser Selesai Pengambilan".
- **Behavior:** Backend memvalidasi apakah jumlah `scannedKanban` di semua manifest milik *Stop* tersebut sudah menyentuh angka `totalKanban`.

## 2. Diagram Sekuensial (Scanner Flow)

```mermaid
sequenceDiagram
    autonumber
    actor Driver as Driver (Mobile)
    participant Client as Aplikasi Mobile
    participant JobAPI as EDCL.Module.Job (Backend)
    participant DB as SQL Server Database

    %% Fase Memuat Detail
    rect rgb(240, 248, 255)
        Note over Driver,DB: Fase 1: Membuka Daftar Manifest
        Driver->>Client: Ketuk arah panah ➡ (di Info Pengiriman)
        Client->>JobAPI: GET /stops/{stopId}/manifests (NEW API)
        JobAPI->>DB: Query tabel [job].[pickup_order_manifests]
        DB-->>JobAPI: Return List Manifest + Current Scanned
        JobAPI-->>Client: Response Data
        Client-->>Driver: Render Layar (Detail Pengiriman)
    end

    %% Fase Pemindaian Berulang
    rect rgb(255, 250, 240)
        Note over Driver,DB: Fase 2: Pemindaian (Scanning Loop)
        Driver->>Client: Ketuk "Mulai Scan" & Arahkan ke Barcode
        Client->>Client: Capture string (misal: "KBN-101")
        Client->>JobAPI: POST /stops/{id}/manifests/{mId}/kanban
        JobAPI->>DB: Cek validitas & Insert ke [job].[pickup_order_kanbans]
        DB-->>JobAPI: Insert Sukses
        JobAPI-->>Client: Return Status OK
        Client-->>Client: Update UI Counter & Centang Hijau jika Penuh
        Client-->>Driver: Suara Beep Sukses
    end

    %% Fase Penguncian (Complete Stop)
    rect rgb(255, 240, 245)
        Note over Driver,DB: Fase 3: Selesai Pengambilan (Swipe)
        Driver->>Client: Geser "Selesai Pengambilan"
        Client->>Client: Dapatkan Lokasi GPS (Lat, Long)
        Client->>JobAPI: POST /stops/{stopId}/complete
        JobAPI->>DB: Validasi total scanned vs total required
        DB-->>JobAPI: (Status Match/OK)
        JobAPI->>DB: Update Stop Status -> "COMPLETED"
        JobAPI-->>Client: Response (Success)
        Client-->>Driver: Tutup Layar Scanner, kembali ke Info Pengiriman
    end
```

## 3. Best Practices Implementasi Pemindaian

1. **Optimistic UI Updates (Resiliensi UX)**
   - Karena *scanning* terjadi dengan cepat (Driver dapat menembak 5 barcode dalam 3 detik), memanggil *endpoint* secara sinkron (menunggu *loading spin* per *scan*) akan menghambat pergerakan operasional.
   - **Rekomendasi:** Mobile app dapat mengantre (*queue*) payload *scan* di latar belakang dan merender UI secara optimis (langsung memperbarui *counter* lokal). Jika panggilan HTTP *Failed*, *counter* ditarik kembali (*rollback*) dan memunculkan *toast error*.

2. **Slider/Swipe untuk Aksi Destruktif**
   - Penggunaan tombol "Geser Selesai Pengambilan" (Swipe to Complete) merupakan *best practice* dalam aplikasi *Supply Chain* untuk mencegah ketidaksengajaan mengetuk (*accidental tap*) yang dapat mengakibatkan rute terkunci sebelum barang sungguhan naik ke truk.

3. **Debouncing Camera Scan**
   - Aplikasi klien (Mobile) harus mengimplementasikan *debouncing* atau jeda singkat (sekitar 1-2 detik) pada pembacaan kamera setelah satu barcode berhasil terbaca. Hal ini mencegah pengiriman 10 request API sekaligus untuk satu *barcode* yang sama karena tangan Driver belum beranjak.
   
4. **Validasi Strict Manifest di Backend**
   - API `POST .../kanban` tidak boleh hanya sekadar memasukkan data (*insert blind*). Ia wajib mengecek ulang tabel database apakah `ScannedKanban` sudah melebihi batas `TotalKanban`. Jika lebih, API harus menolak dengan HTTP 400 (Bad Request).
