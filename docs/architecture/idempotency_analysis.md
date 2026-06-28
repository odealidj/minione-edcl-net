# Analisis Mendalam: Kebutuhan Idempotency-Key pada Endpoint Mobile API

Dalam pengembangan aplikasi dengan mobilitas tinggi (*on-the-go*) seperti aplikasi logistik untuk *Driver*, isu ketidakstabilan jaringan seluler adalah tantangan terbesar. Sering kali aplikasi harus melakukan *retry* (coba ulang) atau Driver tidak sabar sehingga menekan tombol berkali-kali (*double-tap/fat-finger*).

Untuk mencegah eksekusi ganda di *Backend* (misalnya: status berubah dua kali, atau entitas ter-insert ganda), kita memerlukan mekanisme **Idempotency-Key** pada *request* bertipe `POST`.

Berikut adalah analisis mendalam (*Endpoint per Endpoint*) terhadap seluruh API yang digunakan oleh aplikasi Mobile:

## 1. Grup Autentikasi (`Auth Controller`)
*Endpoint: `POST /api/v1/auth/drivers/login` dan `POST /api/v1/auth/drivers/change-pin`*

- **Sifat:** Pada dasarnya, memanggil endpoint ini berkali-kali tidak akan merusak integritas *database*. Login akan selalu me-return Token baru. Ganti PIN adalah operasi pergantian data absolut (seperti `PUT`).
- **Analisis:** Jika ada *retry* jaringan, sistem hanya memvalidasi ulang dan memberikan respons baru.
- **Rekomendasi:** **TIDAK PERLU** menggunakan `Idempotency-Key`. Cukup tangani *double-tap* dengan *UI blocking* (men-disable tombol *loading* di layar Mobile).

---

## 2. Pemicu State Pekerjaan (*Job Transitions*)
*Endpoint: `POST /api/v1/jobs/{id}/start` dan `POST /api/v1/jobs/{id}/end`*

- **Sifat:** Endpoint ini merubah *Domain State* (dari `PENDING` -> `ON_PROGRESS` -> `COMPLETED`). Metode pada *Domain Entity* (`job.Start()` dan `job.Complete()`) dirancang sangat ketat (*Rich Domain Model*); jika status tidak sesuai, ia akan melempar *Exception* / Error 400 Bad Request.
- **Analisis (Masalah):** Bayangkan Driver di area TMMIN menekan "Mulai Rute". Sinyal putus. *Backend* menerima dan sukses memproses (status jadi `ON_PROGRESS`). Namun *Response HTTP* gagal sampai ke Mobile. Mobile kemudian melakukan *Auto-Retry*. Pada percobaan kedua, *Backend* menolak dan melempar *Error* "Job is already ON_PROGRESS". Akibatnya, UI Mobile menampilkan Error merah muda kepada Driver, padahal secara aktual sistem sukses di percobaan pertama.
- **Rekomendasi:** **SANGAT WAJIB** menggunakan `Idempotency-Key`.
  - Jika klien mengirimkan UUID `X-Idempotency-Key` yang sama pada percobaan kedua, *Backend* harus secara langsung mengembalikan respons sukses 200 OK yang tersimpan (*cached*), tanpa mengeksekusi logika atau memunculkan error *Domain*.

---

## 3. Pemicu Penyelesaian Titik (*Stop Complete*)
*Endpoint: `POST /api/v1/jobs/stops/{stopId}/complete`*

- **Sifat:** Fungsi `MarkPickedUp()` di Backend hanya menimpa status dari `PENDING` ke `PICKED_UP`. Jika dipanggil dua kali, ia tidak akan melempar *Exception* (karena tidak ada validasi tolakan).
- **Analisis:** Secara kode, ia sudah bersifat *idempotent* secara kebetulan (menimpa status yang sama).
- **Rekomendasi:** **DISARANKAN**, demi konsistensi arsitektur. Meski kode Backend tidak akan rusak, mengaktifkan `Idempotency-Key` di sini akan memotong akses ke *Database* (langsung mengembalikan memori *cache* respons pertama), sehingga beban server berkurang secara signifikan saat terjadi lonjakan *retry*.

---

## 4. Perekaman Scan Kanban (*Kanban Scanning*)
*Endpoint: `POST /api/v1/jobs/stops/{stopId}/manifests/{manifestId}/kanban`*

- **Sifat:** Menambah data ke tabel `pickup_order_kanbans`.
- **Analisis (Masalah):** Jika Driver menembakkan *barcode scanner* dua kali cepat secara berturut-turut pada 1 kartu Kanban yang sama, Backend akan memprosesnya. Pada kode kita saat ini, ada baris:
  `if (manifest.Kanbans.Any(x => x.KanbanCode == request.KanbanCode)) return Error("AlreadyScanned");`
  Jika terjadi *Network Retry* karena sinyal pabrik jelek, percobaan kedua akan dibalas dengan **Error (Already Scanned)**. Ini bisa membingungkan Driver ("Loh kok error, apakah tidak terekam?").
- **Rekomendasi:** **OPSIONAL BERSIFAT KHUSUS**.
  Pada skenario ini, nilai `KanbanCode` dari *body request* itu sendiri sejatinya bisa difungsikan sebagai *Idempotency-Key* logis. 
  Pilihannya:
  1. Frontend memaksa mengirimkan `X-Idempotency-Key` berupa UUID.
  2. Atau, Backend merubah logika kode agar ketika terjadi Error "Already Scanned", Backend tidak membalas `400 Bad Request`, melainkan membalas `200 OK` (seolah-olah sukses). **Ini pendekatan yang lebih disukai untuk scanner berkecepatan tinggi**.

---

## Kesimpulan Eksekusi untuk Backend (Action Plan)

Berdasarkan analisis di atas, jika Anda ingin menerapkan *best practice* level Enterprise, kita harus mengimplementasikan **Idempotency Middleware** atau *Action Filter* di ASP.NET Core kita.

1. **Membuat Idempotency Table/Cache:** Menggunakan `IDistributedCache` (Redis/Memory) untuk menyimpan hasil respons (berdasarkan `X-Idempotency-Key` + ID Klien) selama 24 jam.
2. **Membuat Idempotency Attribute:** Contoh `[RequireIdempotency]` yang ditempelkan khusus di atas `[HttpPost("{id}/start")]`, `[HttpPost("{id}/end")]`, dan `[HttpPost("stops/{stopId}/complete")]`.
3. **Penyesuaian Respons Scan:** Mengubah `ScanKanbanCommandHandler` agar mereturn kesuksesan semu jika Kanban sudah pernah di-scan, bukan melempar Error teknis yang ditangkap UI.
