# Dokumen Teknis: Sistem Info Pengiriman (Route Execution)

Spesifikasi teknis interaksi antarmuka mobile dan backend saat driver mengeksekusi rute perhentian (*Route Stops*).

---

## 1. Spesifikasi Endpoint

| Endpoint | Method | Deskripsi |
|---|---|---|
| `GET /api/v1/mobile/driver/jobs/{id}/route-stops` | `GET` | Daftar perhentian rute dan progres pemindaian kanban. |
| `POST /api/v1/mobile/driver/jobs/stops/{stopId}/complete` | `POST` | Menyelesaikan perhentian (*Picked Up*). |
| `POST /api/v1/mobile/driver/jobs/{id}/end` | `POST` | Mengakhiri seluruh tugas pengiriman dengan koordinat GPS. |

---

## 2. Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    actor Driver as Driver (Mobile)
    participant Client as Aplikasi Mobile
    participant JobAPI as EDCL.Module.Job (Backend)
    participant DB as SQL Server Database

    %% Fase Inisialisasi Layar
    rect rgb(240, 248, 255)
        Note over Driver,DB: Fase 1: Memuat Data Rute
        Driver->>Client: Buka "Info Pengiriman"
        Client->>JobAPI: GET /api/v1/jobs/{id}/route-stops
        JobAPI->>DB: Query RouteStops & Progress
        DB-->>JobAPI: Data Kanban (Original vs Scanned)
        JobAPI-->>Client: Kembalikan List Supplier
        Client-->>Driver: Tampilkan UI Info Pengiriman
    end

    %% Fase Eksekusi Supplier
    rect rgb(255, 250, 240)
        Note over Driver,DB: Fase 2: Selesaikan Pemberhentian (Skenario Sukses Pemindaian)
        Driver->>Client: Selesai Scan & Klik "Selesaikan Titik Ini"
        Client->>JobAPI: POST /stops/{stopId}/complete
        JobAPI->>JobAPI: Validasi: Apakah semua Kanban wajib sudah dipindai?
        JobAPI->>DB: Ubah status Stop ke "COMPLETED"
        DB-->>JobAPI: Sukses
        JobAPI-->>Client: Response (Success)
        Client-->>Driver: Ubah UI menjadi "✓ Picked Up"
    end

    %% Fase Penyelesaian Tugas
    rect rgb(255, 240, 245)
        Note over Driver,DB: Fase 3: Mengakhiri Pekerjaan Seluruh Rute
        Driver->>Client: Klik tombol "END JOB"
        Client->>Client: Dapatkan GPS Location (Lat, Long)
        Client->>JobAPI: POST /jobs/{id}/end (Body: Lat, Long)
        JobAPI->>JobAPI: Validasi: Apakah semua Stop sudah COMPLETED?
        JobAPI->>JobAPI: Validasi Geofencing (Jarak GPS vs Titik Tujuan)
        JobAPI->>DB: Ubah status Order menjadi "COMPLETED"
        DB-->>JobAPI: Sukses
        JobAPI-->>Client: Response (Success)
        Client-->>Driver: Redirect ke Dashboard/Beranda
    end
```

---

## 3. Aturan Bisnis & Validasi
- **State Validation**: `POST /end` ditolak jika masih ada stop yang belum berstatus `COMPLETED`.
- **Geofence Audit**: Memvalidasi radius lokasi penyelesaian terhadap plant tujuan.
- **Idempotency**: Mencegah mutasi ganda pada retries jaringan seluler.
