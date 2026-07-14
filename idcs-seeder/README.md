# EDCL IDCS Seeder

IDCS Seeder adalah proyek simulasi berupa .NET Console Application yang dirancang untuk meniru perilaku sistem IDCS (*legacy*) dengan melakukan Manipulasi Data (Insert/Update) secara langsung ke dalam tabel-tabel *database* IDCS.

Proyek ini menjadi tulang punggung (*backbone*) untuk **Pengujian End-to-End (E2E)** dari mekanisme **Change Data Capture (CDC)** menggunakan **Debezium**, yang terhubung ke **RabbitMQ** (melalui MassTransit) dan kemudian datanya dikonsumsi oleh layanan backend `EDCL.Worker.Ingestion`.

## 🎯 Kapabilitas Utama

- **Inisialisasi Database IDCS:** Membuat *database*, tabel-tabel, dan mengaktifkan SQL Server Change Data Capture (CDC) secara otomatis baik di tingkat *database* maupun tabel.
- **Simulasi Insert Real-time:** Memasukkan data acak yang realistis (Manifest, Part, Kanban, Skid) yang mencerminkan struktur data IDCS sesungguhnya.
- **Bulk Seeding (Data Massal):** Menghasilkan data dalam volume besar (misalnya 200 Manifest sekaligus) untuk pengujian beban dan evaluasi performa CDC.
- **Pengujian Gatekeeper (CDC Anomalies):** Menghasilkan status transaksional tertentu di backend EDCL secara spesifik untuk menguji aturan penolakan data (misalnya: menolak pembaruan CDC ketika status Manifest sudah `InTransit` atau `Delivered`).
- **Simulasi Kasus Ekstrem (Edge-Case):**
  - **Out of Order (Tidak Berurutan):** Mensimulasikan masuknya data anak (Part/Kanban) sebelum data induknya (Manifest) ada, memicu mekanisme *retry* EDCL dan memberikan peringatan via Server-Sent Events (SSE).
  - **Race Conditions (Kondisi Balapan):** Mensimulasikan penundaan proses simpan data Manifest sehingga berbarengan dengan proses data lainnya di situasi konkurensi yang tinggi.
- **Master Data Seeding:** Secara otomatis menyuntikkan data master pendukung langsung ke dalam *database* EDCL (seperti Ekspedisi, Rute, Truk, Sopir, Pemasok).

## 🛠 Prasyarat

Proyek ini menyertakan *container* SQL Server tersendiri yang terisolasi untuk keperluan simulasi IDCS (`docker-compose-idcs.yml`).
1. Pastikan Anda menjalankan `make idcs-up` di direktori ini untuk menyalakan SQL Server IDCS di *port* `1466`.
2. Infrastruktur utama EDCL (RabbitMQ, SQL Server EDCL di *port* `1444`, Debezium) juga harus dalam keadaan berjalan.

## 🚀 Panduan Penggunaan (Local Makefile)

Direktori `idcs-seeder` menyediakan file `Makefile` khusus agar Anda dapat menjalankan perintah dengan cepat. Jalankan perintah di bawah ini dari dalam direktori `idcs-seeder`:

### Perintah Infrastruktur
| Perintah | Deskripsi |
|---|---|
| `make idcs-up` | Menyalakan *container* SQL Server IDCS menggunakan `docker-compose-idcs.yml`. |
| `make idcs-down` | Menghentikan dan menghapus *container* SQL Server IDCS. |

### Inisialisasi & Pembersihan (Cleanup)
| Perintah | Deskripsi |
|---|---|
| `make seed-init` | Menginisialisasi *database* IDCS, membuat seluruh tabel yang diperlukan, dan mengaktifkan CDC via `sys.sp_cdc_enable_db`. **Perintah ini harus dijalankan paling pertama.** |
| `make reset-transaction` | Menghapus (truncate) dan me-reset seluruh data transaksional Manifest di **kedua** *database* (IDCS maupun EDCL). |
| `make reset-pickup` | Secara aman membersihkan tabel `job.pickup_orders` (dan anakannya) di EDCL yang dihasilkan saat proses *testing*. |
| `make reset-master` | Mereset seluruh Data Master dari *database* EDCL. |

### Master Data Seeding (Pembuatan Data Master)
| Perintah | Deskripsi |
|---|---|
| `make seed-master` | Membuat data master inti EDCL (Ekspedisi/Logistic Partners, Pemasok, Rute, Sopir, dan Truk). |
| `make seed-one-master` | Membuat satu spesifik data master (secara interaktif). |

### Transactional Data Seeding (Pembuatan Data Transaksi)
| Perintah | Deskripsi |
|---|---|
| `make seed-transaction` | Menyuntikkan satu alur transaksi lengkap (1 Manifest, 1 Skid, 1 Part, 1 Kanban). |
| `make seed-bulk` | Menjalankan *insert* besar-besaran (200 Manifest beserta Part, Kanban, dan Skid yang realistis) untuk menguji kestabilan antrean CDC dan beban dari *Worker Ingestion*. |

### Simulasi Kasus Ekstrem & Gatekeeper
| Perintah | Deskripsi |
|---|---|
| `make trigger-reject` | **Pengujian Gatekeeper:** Membuat data *Pickup Order* fiktif dengan status `ON_PROGRESS` dan `COMPLETED` langsung di EDCL, kemudian memicu *update* CDC dari IDCS untuk menguji sistem menolak *update* tersebut dan merekamnya di tabel `manifest_problems`. |
| `make seed-out-of-order` | Menyuntikkan data *Part* dengan ID *Manifest* yang tidak pernah ada. Digunakan untuk menguji *Dead Letter Queues* (DLQ) dan mekanisme *Retry* pada *Worker Ingestion*. |
| `make seed-race-condition`| Secara sengaja menunda proses pembuatan *Manifest* *setelah* data *Part*-nya selesai dibuat guna melihat bagaimana *Worker* CDC menanggulangi *Race Conditions*. |
| `make seed-k6-clean` | Membersihkan sistem khusus untuk persiapan tes performa K6. |

## 🏗 Cara Kerja Sistem (Under the Hood)

1. *Seeder* ini terhubung langsung ke SQL Server menggunakan `Dapper` (`1466` untuk IDCS, `1444` untuk EDCL).
2. Ketika melakukan *insert* ke IDCS (contoh: `make seed-transaction`), *container* Debezium Server memantau log transaksi (*transaction log*) di dalam *database* `IDCS`.
3. Debezium mendeteksi baris-baris baru dan langsung mengirimkan pesan (JSON *payloads*) ke RabbitMQ.
4. Layanan `EDCL.Worker.Ingestion` (Worker) akan mengonsumsi pesan-pesan dari RabbitMQ ini.
5. **Validasi Gatekeeper:** Worker memvalidasi data terhadap status `job.pickup_order_manifests` saat ini. Jika status valid, data akan dilanjutkan dan dirapihkan masuk ke dalam tabel `ingestion.manifests` EDCL. Namun jika ditolak (misalnya barang sudah *picked up*), *Worker* tersebut akan mencatat masalahnya (log) ke tabel `ingestion.manifest_problems`.
