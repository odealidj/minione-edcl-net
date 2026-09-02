# Dokumen Teknis: Detail Pengiriman (Kanban Scanning System)

Spesifikasi alur pemindaian barcode Kanban pada perhentian supplier (*Pickup Stop*).

---

## 1. Spesifikasi Endpoint

| Endpoint | HTTP Method | Deskripsi |
|---|---|---|
| `/api/v1/mobile/driver/jobs/stops/{stopId}/manifests` | `GET` | Mengambil daftar manifes dan progres pemindaian kanban per manifes. |
| `/api/v1/mobile/driver/jobs/stops/{stopId}/manifests/{manifestId}/kanban` | `POST` | Merekam barcode kanban yang dipindai (`KanbanCode`). |
| `/api/v1/mobile/driver/jobs/stops/{stopId}/complete` | `POST` | Mengunci perhentian setelah seluruh kanban terverifikasi. |

---

## 2. Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    actor Driver as Driver (Mobile)
    participant Client as Mobile App
    participant JobAPI as EDCL.Module.Job
    participant DB as SQL Server

    %% Fase 1
    rect rgb(240, 248, 255)
    Note over Driver,DB: 1. Membuka Daftar Manifest
    Driver->>Client: Buka Detail Pengiriman
    Client->>JobAPI: GET /stops/{stopId}/manifests
    JobAPI->>DB: Query [job].[pickup_order_manifests]
    DB-->>JobAPI: List Manifest + Current Scanned
    JobAPI-->>Client: 200 OK (Manifest List)
    end

    %% Fase 2
    rect rgb(255, 250, 240)
    Note over Driver,DB: 2. Pemindaian Kanban
    Driver->>Client: Scan Barcode ("KBN-101")
    Client->>JobAPI: POST /stops/{id}/manifests/{mId}/kanban
    JobAPI->>DB: Validasi & Insert [job].[pickup_order_kanbans]
    DB-->>JobAPI: Insert Sukses
    JobAPI-->>Client: 200 OK
    Client-->>Driver: Update UI Counter & Suara Beep
    end

    %% Fase 3
    rect rgb(255, 240, 245)
    Note over Driver,DB: 3. Selesai Pengambilan (Swipe)
    Driver->>Client: Geser "Selesai Pengambilan"
    Client->>JobAPI: POST /stops/{stopId}/complete (Lat, Long)
    JobAPI->>DB: Validasi Total Scanned == Required
    JobAPI->>DB: Update Stop Status -> "COMPLETED"
    JobAPI-->>Client: 200 OK
    Client-->>Driver: Kembali ke Info Pengiriman
    end
```

---

## 3. Best Practices
- **Camera Debouncing**: Jeda 1-2 detik setelah pemindaian barcode untuk mencegah duplicate scan.
- **Swipe-to-Complete**: Menggunakan gestur geser (*swipe*) untuk mencegah trigger selesai yang tidak disengaja.
- **Backend Strict Validation**: Menolak pemindaian kanban yang melebihi kuota manifes dengan `400 Bad Request`.
