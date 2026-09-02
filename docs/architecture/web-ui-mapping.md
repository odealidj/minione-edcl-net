# Web UI to API Mapping (Functional Overview)

Dokumen ini memetakan seluruh tampilan antarmuka (UI) aplikasi Web Dashboard (Admin/Staff) dengan Endpoint API yang sesuai di sisi Backend (EDCL Mini).

---

## 1. 🔑 Autentikasi & Manajemen Pengguna Web

### A. Web Login (`/login`)
- **Endpoint**: `POST /api/v1/auth/users/login`
- **Request**: `{ "email": "admin@edcl.com", "password": "Password123!" }`
- **Response**: `{ "data": { "userId": "...", "roles": ["ADMIN"], "accessToken": "..." } }`

### B. Auto-Refresh Token
- **Endpoint**: `POST /api/v1/auth/users/refresh-token`
- **Deskripsi**: Di-trigger otomatis oleh HTTP Interceptor saat terjadi HTTP 401 Unauthorized tanpa memutus sesi user.

### C. Manajemen Pengguna (`/admin/users`)
- **List Users**: `GET /api/v1/auth/users`
- **Update Role User**: `PUT /api/v1/auth/users/{id}/role`

---

## 2. 📊 System Observability & Health (`/admin/system-observability`)

Tampilan pemantauan performa real-time, telemetri OpenTelemetry, dan panduan kapasitas infrastruktur.

- **Endpoint**: `GET /api/v1/web/admin/observability/metrics`
- **Auth**: `Bearer JWT Token` (Role: `ADMIN`)
- **Komponen UI**:
  - **4 Stat Cards**: CPU %, RAM Working Set (MB) & GC, Idempotency Shield count, CDC events throughput.
  - **2 Time-Series Charts (Chart.js)**: CPU & RAM Trend (Dual y-axis) dan Throughput (Requests/sec).
  - **Card Memory Allocation by Service**: Donut Chart & Tabel alokasi RAM 5 proses .NET (`EDCL.Api`, `EDCL.Worker.Ingestion`, `EDCL.Worker.GpsTracker`, `EDCL.Gateway`, `EDCL.Worker.Outbox` $\to$ Cluster Total: ~695 MB).
  - **Card Full-Stack Infrastructure Sizing**: Donut Chart & Tabel alokasi seluruh ekosistem (SQL Server ~1.45 GB, .NET App ~700 MB, Linux OS ~450 MB, Debezium ~180 MB, Jaeger/Prometheus ~120 MB, RabbitMQ ~115 MB, Redis ~28 MB) + 3 Rekomendasi Host (Min Dev 4 GB, Prod 8 GB, Multi-Server).
  - **Infrastructure Health Probes**: Live status probe untuk SQL Server, Redis, RabbitMQ.
  - **Launchpad**: Tautan langsung ke Jaeger (`:16686`), Prometheus (`:9090`), Hangfire (`:5140/hangfire`), RabbitMQ (`:15672`), dan `/metrics`.

---

## 3. 📋 Route Planning & Manifest Management (`/admin/route-planning`)

Tampilan perancangan rute pickup multi-stop harian untuk armada logistik.

### A. List Route Plans
- **Endpoint**: `GET /api/v1/web/admin/pickup-orders?page=1&pageSize=10`
- **Fitur**: Filter berdasarkan status (`PENDING`, `ON_PROGRESS`, `COMPLETED`), tanggal, dan rute.

### B. Form Create / Edit Route Plan (`/admin/route-planning/:id`)
- **Create**: `POST /api/v1/web/admin/pickup-orders`
- **Edit**: `PUT /api/v1/web/admin/pickup-orders/{id}`
- **Delete**: `DELETE /api/v1/web/admin/pickup-orders/{id}` (Hanya jika status `PENDING`)
- **Fitur Khusus**:
  - Drag-and-drop urutan stop menggunakan `@angular/cdk/drag-drop`.
  - Multi-manifest selection modal dengan filter supplier.
  - Smart cross-filter supir dan truk berdasarkan *Logistic Partner* rute.

### C. Assign & Force Complete
- **Assign Driver & Truck**: `POST /api/v1/web/admin/pickup-orders/{id}/assign`
- **Force Complete (Admin Action)**: `POST /api/v1/web/admin/pickup-orders/{id}/complete`

---

## 4. 🗺️ Live Fleet Tracking & Operational Dashboard (`/dashboard`)

Peta operasional pemantauan pergerakan armada secara real-time.

- **KPI Summary**: `GET /api/v1/web/admin/dashboard/summary` (Total order, pending, on progress, completed, kanban count, Hangfire jobs).
- **Live Fleets Coordinates**: `GET /api/v1/web/admin/dashboard/live-fleets`
- **SignalR Real-Time Stream**: `ws://localhost:5140/hubs/tracking` (Menerima broadcast pembaruan posisi truk aktif secara instan).
- **Fitur Peta Leaflet**: Marker truk bergerak adaptif, info popup detail PO & sopir, status geofencing kedatangan di supplier.

---

## 5. ⚡ Sync Command Center & CDC Monitoring (`/admin/sync-monitoring`)

Pusat kendali sinkronisasi Change Data Capture (CDC) dari sistem IDCS.

- **Aliran Real-Time**: Server-Sent Events (SSE) `GET /api/v1/web/admin/sync-monitoring/live-stream`
- **Histori Sesi**: `GET /api/v1/web/admin/sync-monitoring/sessions`
- **Dead Letter Queue (DLQ) Resolution**:
  - Tinjau pesan error di `edcl_ingestion_faults`.
  - Tombol **Re-Queue All** / **Resolve Fault** langsung dari UI.

---

## 6. 🚚 Delivery Monitoring (`/operations/monitoring`)

Monitoring status pengiriman perhentian sopir (*Supplier Stops*).

- **Endpoint**: `GET /api/v1/web/admin/delivery-monitoring`
- **Fitur**: Tracking status perhentian (*Arrived, Picked Up, Verified*) dan alert keterlambatan keberangkatan.

---

## 7. 🔔 Notification Logs & Alerts (`/admin/notification-logs`)

Audit trail pengiriman notifikasi FCM ke aplikasi mobile supir.

- **List Logs**: `GET /api/v1/web/admin/notifications/logs?page=1&pageSize=10`
- **Resend Notification**: `POST /api/v1/web/admin/notifications/{id}/resend`
- **Unread Alerts**: `GET /api/v1/web/admin/notifications/alerts`
- **Acknowledge Alert**: `PUT /api/v1/web/admin/notifications/alerts/{id}/acknowledge`

---

## 8. 🗄️ Master Data Management

| Menu | Path UI | Endpoint API | Deskripsi |
|---|---|---|---|
| **Driver** | `/master/driver` | `GET/POST/PUT/DELETE /api/v1/auth/drivers` | Manajemen supir, NIK, No HP, & Toggle Status |
| **Supplier** | `/master/supplier` | `GET/POST/PUT/DELETE /api/v1/web/master/suppliers` | Pabrik supplier, koordinat lat/lng, radius geofence |
| **Truck** | `/master/truck` | `GET/POST/PUT/DELETE /api/v1/web/master/trucks` | Plat nomor, tipe kendaraan, kapasitas |
| **Truck Assignment** | `/master/truck-assignments` | `POST /api/v1/web/master/trucks/{id}/assign` | Pasangkan supir dengan kendaraan |
| **Logistic Partner** | `/master/logisticPartner` | `GET/POST/PUT/DELETE /api/v1/web/master/logistic-partners` | Vendor logistik & Assign GPS Vendor modal |
| **GPS Vendor** | `/master/gps-vendor` | `GET/POST/PUT/DELETE /api/v1/web/master/gps-vendors` | API Key vendor GPS & Tombol Test Connection |
| **Route & Cycle** | `/master/route` | `GET/POST/PUT/DELETE /api/v1/web/master/routes` | Master kode rute, siklus, & Logistic Partner filter |
