# Architecture: Event-Driven with Exponential Backoff & Dead Letter Exchange (DLX)

Pola mitigasi *out-of-order events* dan *transient errors* pada alur Change Data Capture (CDC) Debezium $\to$ RabbitMQ $\to$ SQL Server.

---

## 1. Topologi Antrean Retry & DLX

```mermaid
sequenceDiagram
    autonumber
    participant RMQ as RabbitMQ (edcl_ingestion)
    participant RetryQ as Retry Queue (TTL: 2s..32s)
    participant Worker as Ingestion Worker
    participant DB as SQL Server
    participant DLQ as DLX (edcl_ingestion_faults)

    RMQ->>Worker: 1. Terima Child Event (ManifestParts)
    Worker->>DB: 2. INSERT Child
    DB-->>Worker: 3. Error FK (Parent belum tiba)
    
    alt attempt < maxRetries (5x)
        Worker->>Worker: Hitung Delay: 2^attempt * 1000 ms
        Worker->>RetryQ: 4. Publish ke edcl_ingestion_retry_{delayMs}
        Note over RetryQ: Tunggu TTL habis (2s, 4s, 8s, 16s, 32s)
        RetryQ-->>RMQ: 5. DLX melempar kembali ke edcl_ingestion
        RMQ->>Worker: 6. Consume ulang setelah Parent masuk
        Worker->>DB: 7. INSERT Sukses
    else attempt >= maxRetries
        Worker->>DLQ: 8. Publish ke edcl_ingestion_faults (Poison Message)
        Worker->>DB: 9. Catat ke tabel [ingestion].[ingestion_errors]
    end
```

---

## 2. Struktur Antrean RabbitMQ

| Antrean | Tipe / Konfigurasi | Fungsi |
|---|---|---|
| `edcl_ingestion` | Main Consumer Queue | Antrean utama yang dikonsumsi oleh `EDCL.Worker.Ingestion`. |
| `edcl_ingestion_retry_2000` | TTL 2s, DLX `edcl_ingestion` | Ruang tunggu retry stage 1 (2 detik). |
| `edcl_ingestion_retry_4000` | TTL 4s, DLX `edcl_ingestion` | Ruang tunggu retry stage 2 (4 detik). |
| `edcl_ingestion_retry_8000` | TTL 8s, DLX `edcl_ingestion` | Ruang tunggu retry stage 3 (8 detik). |
| `edcl_ingestion_retry_16000` | TTL 16s, DLX `edcl_ingestion` | Ruang tunggu retry stage 4 (16 detik). |
| `edcl_ingestion_retry_32000` | TTL 32s, DLX `edcl_ingestion` | Ruang tunggu retry stage 5 (32 detik). |
| `edcl_ingestion_faults` | Dead Letter Queue (DLQ) | Penyimpanan pesan gagal permanen untuk inspeksi dan manual re-queue via Sync Command Center UI. |

---

## 3. Keunggulan Arsitektur
- **Zero Data Loss**: Pesan out-of-order tidak dibuang, melainkan ditunda secara non-blocking.
- **Head-of-Line Blocking Prevention**: Antrean utama tetap memproses pesan valid lainnya tanpa terhambat poison pill.
- **Automatic Recovery**: Hubungan relasional Foreign Key terpulihkan otomatis saat pesan induk selesai diproses.
