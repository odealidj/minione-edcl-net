# Web UI to API Mapping (Functional Overview)

Pemetaan antarmuka Web Admin ke endpoint backend **EDCL Mini**.

---

## 1. 🔑 Autentikasi & Manajemen Pengguna Web

| Modul UI | Endpoint API | Method | Deskripsi |
|---|---|---|---|
| **Web Login** (`/login`) | `/api/v1/auth/users/login` | `POST` | Login email + password, mengembalikan JWT & Role. |
| **Auto-Refresh** | `/api/v1/auth/users/refresh-token` | `POST` | Rotasi token via HTTP Interceptor saat 401. |
| **User List** (`/admin/users`) | `/api/v1/auth/users` | `GET` | Daftar pengguna dan role backoffice. |
| **User Role** | `/api/v1/auth/users/{id}/role` | `PUT` | Perubahan hak akses pengguna (`ADMIN` / `USER`). |

---

## 2. 📊 System Observability & Health (`/admin/system-observability`)

- **Telemetry Endpoint**: `GET /api/v1/web/admin/observability/metrics`
- **Metrik & Visualisasi**:
  - **Live Stat Cards**: CPU %, Memory Working Set (MB), GC Collections, Idempotency Shield, Throughput CDC.
  - **Charts (Chart.js)**: Time-series CPU/RAM Dual Y-Axis, Throughput req/sec.
  - **Cluster Memory Breakdown**: Donut chart alokasi RAM proses .NET (`Api`, `Worker.Ingestion`, `Worker.GpsTracker`, `Gateway`, `Worker.Outbox`).
  - **Full-Stack Infrastructure Sizing**: Estimasi kapasitas SQL Server, RabbitMQ, Redis, Jaeger/Prometheus, Linux OS.
  - **Launchpad**: Jaeger (`:16686`), Prometheus (`:9090`), Hangfire Dashboard (`:5140/hangfire`), RabbitMQ (`:15672`).

---

## 3. 📋 Route Planning & Operations (`/admin/route-planning`)

| Fitur | Endpoint API | Method | Deskripsi |
|---|---|---|---|
| **List Plans** | `/api/v1/web/admin/pickup-orders` | `GET` | Filter status (`PENDING`, `ON_PROGRESS`, `COMPLETED`), tanggal, & rute. |
| **Create Plan** | `/api/v1/web/admin/pickup-orders` | `POST` | Buat rencana penjemputan multi-stop & multi-manifest. |
| **Update Plan** | `/api/v1/web/admin/pickup-orders/{id}` | `PUT` | Edit urutan stop (drag-drop) dan daftar manifes. |
| **Delete Plan** | `/api/v1/web/admin/pickup-orders/{id}` | `DELETE` | Hapus rencana penjemputan (hanya status `PENDING`). |
| **Assign Driver** | `/api/v1/web/admin/pickup-orders/{id}/assign` | `POST` | Pasangkan driver & truk ke rencana kerja. |
| **Force Complete** | `/api/v1/web/admin/pickup-orders/{id}/complete` | `POST` | Bypass penyelesaian rute dari admin. |

---

## 4. 🗺️ Live Fleet Tracking (`/dashboard`)

- **KPI Summary**: `GET /api/v1/web/admin/dashboard/summary`
- **Live Fleet Coordinates**: `GET /api/v1/web/admin/dashboard/live-fleets`
- **Real-Time SignalR Stream**: `ws://localhost:5140/hubs/tracking`
- **Peta Leaflet**: Marker armada bergerak adaptif, popup detail tugas, status geofencing perhentian.

---

## 5. ⚡ Sync Command Center & CDC (`/admin/sync-monitoring`)

- **SSE Live Stream**: `GET /api/v1/web/admin/sync-monitoring/live-stream`
- **Histori Sesi**: `GET /api/v1/web/admin/sync-monitoring/sessions`
- **DLQ Resolution**: Inspeksi pesan gagal `edcl_ingestion_faults`, tombol Re-Queue All / Resolve Fault.

---

## 6. 🔔 Notification Logs (`/admin/notification-logs`)

- **List Logs**: `GET /api/v1/web/admin/notifications/logs?page=1&pageSize=10`
- **Resend FCM**: `POST /api/v1/web/admin/notifications/{id}/resend`
- **Unread Alerts**: `GET /api/v1/web/admin/notifications/alerts`
- **Acknowledge**: `PUT /api/v1/web/admin/notifications/alerts/{id}/acknowledge`

---

## 7. 🗄️ Master Data Management

| Modul Master | Path Web UI | Endpoint API | Deskripsi |
|---|---|---|---|
| **Driver** | `/master/driver` | `GET/POST/PUT/DELETE /api/v1/auth/drivers` | Kelola akun pengemudi, NIK, No HP, & PIN. |
| **Supplier** | `/master/supplier` | `GET/POST/PUT/DELETE /api/v1/web/master/suppliers` | Titik pabrik supplier, lat/long, radius geofence. |
| **Truck** | `/master/truck` | `GET/POST/PUT/DELETE /api/v1/web/master/trucks` | Data armada fisik, plat nomor, tipe kendaraan. |
| **Truck Assignment** | `/master/truck-assignments` | `POST /api/v1/web/master/trucks/{id}/assign` | Pasangkan supir aktif ke truk armada. |
| **Logistic Partner** | `/master/logisticPartner` | `GET/POST/PUT/DELETE /api/v1/web/master/logistic-partners` | Vendor logistik rekanan & integrasi GPS vendor. |
| **GPS Vendor** | `/master/gps-vendor` | `GET/POST/PUT/DELETE /api/v1/web/master/gps-vendors` | API Key vendor pelacak GPS & tes koneksi. |
| **Route & Cycle** | `/master/route` | `GET/POST/PUT/DELETE /api/v1/web/master/routes` | Kode rute pengiriman dan siklus harian. |
