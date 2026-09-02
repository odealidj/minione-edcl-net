# Analisis Kebutuhan Idempotency-Key pada Mobile API

Analisis kebutuhan header `X-Idempotency-Key` (UUIDv4) pada endpoint mobile untuk mencegah *double-execution* akibat retries jaringan seluler dan *double-tap*.

---

## 1. Matriks Kebutuhan Endpoint

| Kategori | Endpoint | HTTP Method | Wajib Idempotency? | Rationale |
|---|---|---|---|---|
| **Auth** | `/api/v1/mobile/driver/auth/login` | `POST` | ❌ Tidak | Safe retry; hanya me-return token JWT baru. |
| **Auth** | `/api/v1/mobile/driver/auth/change-pin` | `POST` | ❌ Tidak | Operasi mutasi absolut (*overwrite* PIN). |
| **Job Transition** | `/api/v1/mobile/driver/jobs/{id}/start` | `POST` | ✅ **Wajib** | Mencegah error *state transition* (400 Bad Request) saat auto-retry. |
| **Job Transition** | `/api/v1/mobile/driver/jobs/{id}/end` | `POST` | ✅ **Wajib** | Mencegah double complete dan duplikasi settlement event. |
| **Stop Status** | `/api/v1/mobile/driver/jobs/stops/{stopId}/arrive` | `POST` | ✅ **Wajib** | Mencegah race condition pencatatan waktu kedatangan di geofence. |
| **Stop Status** | `/api/v1/mobile/driver/jobs/stops/{stopId}/complete` | `POST` | ✅ **Wajib** | Memotong akses database; mengembalikan respons cache saat retry. |
| **Kanban Scanning** | `/api/v1/mobile/driver/jobs/stops/{stopId}/manifests/{manifestId}/kanban` | `POST` | ⚠️ Opsional / Logis | `KanbanCode` berlaku sebagai unique key logis. Return 200 jika sudah ter-scan. |

---

## 2. Strategi Implementasi

1. **Redis Cache-Aside**:
   - Simpan `IdempotencyKey` + User ID di Redis (TTL 24 jam) bersama hash payload respons.
2. **MediatR Pipeline Behavior (`IdempotencyBehavior`)**:
   - Jika key ditemukan, langsung kembalikan *cached response* tanpa mengeksekusi *command handler* dan transaksi database.
3. **Frontend UI Handling**:
   - Disable tombol aksi (*loading state*) saat proses transmisi berlangsung.
