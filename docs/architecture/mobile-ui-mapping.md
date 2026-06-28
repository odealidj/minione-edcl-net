# UI to API Mapping (Functional Overview)

Dokumen ini memetakan tampilan antarmuka (UI) aplikasi klien (Mobile/Web) dengan Endpoint API yang sesuai di sisi Backend (EDCL Mini). Tujuannya adalah mempermudah tim Frontend untuk memahami endpoint mana yang harus dipanggil, parameter apa yang harus dikirim, dan struktur respons seperti apa yang akan diterima.

---

## 1. Driver Login (PIN Authentication)

Tampilan awal aplikasi untuk Driver masuk ke dalam sistem. Autentikasi dilakukan menggunakan Nomor Handphone dan **PIN statis 6-digit** milik Driver. Tidak ada fitur pengiriman OTP di setiap sesi masuk.

**Tampilan:** Layar Login Aplikasi Mobile Driver

**Deskripsi Alur:**
1. Driver memasukkan Nomor HP dan PIN mereka pada form aplikasi.
2. Aplikasi memanggil `POST /api/v1/auth/drivers/login` dengan kredensial tersebut.
3. Backend memverifikasi *hash* PIN. Jika cocok, Backend menerbitkan JWT Access Token & Refresh Token.

<img src="../assets/images/login-driver-otp.png" width="300" alt="Driver Login UI" />

### API Endpoints Terkait

#### A. Eksekusi Login
Endpoint ini akan memverifikasi kredensial driver dan mengembalikan Access Token serta Refresh Token.

- **URL:** `POST /api/v1/auth/drivers/login`
- **Method:** `POST`
- **Auth:** *(None / Public)*

**Request Body:**
```json
{
  "phoneNumber": "08123456789",
  "pin": "123456" 
}
```

**Contoh Request (cURL):**
```bash
curl -X POST http://localhost:5000/api/v1/auth/drivers/login \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber": "08123456789", "pin": "123456"}'
```

**Contoh Response (Success):**
```json
{
  "success": true,
  "traceId": "0HN...:00000003",
  "data": {
    "driverId": 1,
    "name": "LISTIONO",
    "nik": "DRV-001",
    "photoUrl": "https://...",
    "transporterName": "PT. BINTANG",
    "accessToken": "eyJhbGci...",
    "refreshToken": "d2FkYm...",
    "accessTokenExpiresAt": "2024-04-21T10:15:00Z",
    "refreshTokenExpiresAt": "2024-05-21T10:00:00Z"
  }
}
```
**Mapping ke UI:**
- Input `Nomor Handphone` ➡️ Dimasukkan ke *payload* `phoneNumber` pada request API.
- Tombol `MASUK` ➡️ Memicu request POST ke server.
- **Handling Token:** Parameter `accessToken` dan `refreshToken` dari respons wajib disimpan dengan aman di sisi klien (contoh: *Encrypted SharedPreferences* di Android atau *Keychain* di iOS) untuk dipanggil sebagai `Authorization: Bearer` pada layar berikutnya.

#### B. Wajib Ganti PIN (Force Change PIN)
Apabila kredensial *login* valid namun Driver menggunakan PIN bawaan sistem (contoh: `123456`), Endpoint A (Eksekusi Login) akan menolak memberikan token dan memunculkan respons spesifik ini:

**Contoh Response Login (Harus Ganti PIN):**
```json
{
  "success": false,
  "traceId": "0HN...:00000004",
  "status": "error",
  "code": 401,
  "message": "Harap ganti PIN bawaan Anda (6-digit) demi keamanan.",
  "errors": [
    {
      "field": "auth",
      "code": "Auth.ForceChangePin",
      "message": "Harap ganti PIN bawaan Anda (6-digit) demi keamanan."
    }
  ]
}
```

**Mapping ke UI:**
1. Aplikasi Mobile wajib membaca `errors[0].code == "Auth.ForceChangePin"`.
2. Jika terdeteksi, cegah transisi ke Dashboard dan **tampilkan layar Wajib Ganti PIN** (*Mandatory Change PIN Form*).

<img src="../assets/images/simple_change_pin_ui.png" width="300" alt="Force Change PIN UI" />

Pada layar ini, Driver harus membuat PIN baru, lalu dikirim via endpoint ini:

- **URL:** `POST /api/v1/auth/drivers/change-pin`
- **Method:** `POST`
- **Auth:** *(None / Public)*

**Request Body:**
```json
{
  "phoneNumber": "08123456789",
  "oldPin": "123456",
  "newPin": "654321" 
}
```

**Contoh Request (cURL):**
```bash
curl -X POST http://localhost:5000/api/v1/auth/drivers/change-pin \
  -H "Content-Type: application/json" \
  -d '{"phoneNumber": "08123456789", "oldPin": "123456", "newPin": "654321"}'
```

Jika sukses, Backend akan **otomatis me-*login*-kan** dan mengembalikan objek token yang persis sama seperti *Step 2 (Login)*. Aplikasi dapat segera menyimpannya dan memindahkan pengguna ke layar **Dashboard**.

---

## 2. Home / Dashboard (Single Source of Truth)

Tampilan ini adalah halaman utama aplikasi (Home Screen) setelah Driver berhasil login. Layar ini bertanggung jawab untuk selalu menarik data pekerjaan terkini, sehingga menjadi perlindungan utama jika Push Notification terlewat/hilang.

<img src="../assets/images/home-dashboard.png" width="300" alt="Home Dashboard UI" />

### API Endpoints Terkait

#### A. Fetching Dashboard Data (Read)
Endpoint ini digunakan untuk memuat data profil driver, pekerjaan saat ini (Current Job), dan pekerjaan selanjutnya (Next Job). Aplikasi **wajib** memanggil endpoint ini setiap kali halaman utama dibuka atau di-refresh.

- **URL:** `GET /api/v1/jobs/dashboard`
- **Method:** `GET`
- **Auth:** Bearer Token (Driver)

**Contoh Request (cURL):**
```bash
curl -X GET http://localhost:5000/api/v1/jobs/dashboard \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5c..."
```

**Contoh Response:**
```json
{
  "success": true,
  "traceId": "0HN...:00000002",
  "data": {
    "profile": {
      "name": "LISTIONO",
      "photoUrl": "https://...",
      "transporterName": "PT. BINTANG"
    },
    "currentJob": {
      "pickupOrderId": 1,
      "routeCode": "RD23",
      "cycle": "01",
      "deliveryNo": "R202402010081",
      "pickupDate": "25 Apr 2024",
      "time": "01 Feb 2024, 01:00",
      "truckPlate": "B 9607 PXT"
    },
    "nextJob": null
  }
}
```

**Mapping ke UI:**
- Saat aplikasi dibuka, panggil endpoint ini.
- Tampilkan `currentJob` di kotak "Tugas Saat Ini". Jika driver mengetuk (tap) kotak tersebut, arahkan driver ke **Halaman Detail Rute/Notifikasi** (Lanjut ke Poin 3).
- Jika ada notifikasi masuk, setelah diklik, arahkan juga ke Poin 3.

---

## 3. Notification Detail / Route Summary

Tampilan ini muncul ketika *Driver* mengetuk notifikasi "Kerjaan Baru", atau ketika mereka membuka detail rute dari dashboard. Layar ini menampilkan daftar *Supplier* beserta estimasi kedatangan (Arrival Plan).

<img src="../assets/images/notification-new-job.png" width="300" alt="Notification New Job UI" />

### API Endpoints Terkait

#### A. Fetching Data Rute & Supplier (Read)
Endpoint ini digunakan untuk memuat keseluruhan teks pada card (Pickup Date, Route, Cycle, dan List Supplier).

- **URL:** `GET /api/v1/jobs/{id}/route-stops`
- **Method:** `GET`
- **Auth:** Bearer Token (Driver)

**Contoh Request (cURL):**
```bash
curl -X GET http://localhost:5000/api/v1/jobs/1/route-stops \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5c..."
```

**Contoh Response:**
```json
{
  "success": true,
  "traceId": "0HN...:00000001",
  "data": {
    "routeCode": "RD23",
    "cycle": "01",
    "deliveryNo": "DLV-001",
    "date": "21 Apr 2024",
    "stops": [
      {
        "stopId": 1,
        "sequence": 1,
        "label": "Supplier",
        "status": "PENDING",
        "supplierName": "ADVICS INDONESIA",
        "supplierCode": "ADV",
        "eta": "03:16",
        "etd": "03:45",
        "original": { "scanned": 0, "total": 5 },
        "others": { "scanned": 0, "total": 0 },
        "eo": { "scanned": 0, "total": 0 }
      },
      {
        "stopId": 2,
        "sequence": 2,
        "label": "Supplier",
        "status": "PENDING",
        "supplierName": "PT. INDONESIA THAI SUMMIT PLAS",
        "supplierCode": "TSP",
        "eta": "04:05",
        "etd": "04:30",
        "original": { "scanned": 0, "total": 3 },
        "others": { "scanned": 0, "total": 0 },
        "eo": { "scanned": 0, "total": 0 }
      }
    ]
  }
}
```
**Mapping ke UI:**
- Teks `Pickup Date`: Diambil dari `data.date`.
- Teks `Route & Cycle`: Diambil dari penggabungan `data.routeCode` + " - " + `data.cycle`.
- List `Supplier` & `Arrival Plan`: Melakukan *looping* pada array `data.stops`, lalu menampilkan `supplierName` dan `eta`.

#### B. Tombol "Mulai Pekerjaan" (Write)
Endpoint ini dieksekusi ketika pengguna menekan tombol biru **Mulai Pekerjaan**.

- **URL:** `POST /api/v1/jobs/{PickupOrderId}/start`
- **Method:** `POST`
- **Auth:** Bearer Token (Driver)
- **Headers:** 
  - `X-Idempotency-Key`: UUID unik per *tap* tombol (mencegah *double klik*).

**Request Body:** *(Kosong)*
```json
{}
```

**Contoh Request (cURL):**
```bash
curl -X POST http://localhost:5000/api/v1/jobs/1/start \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5c..." \
  -H "X-Idempotency-Key: a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6" \
  -H "Content-Type: application/json" \
  -d '{}'
```

**Contoh Response (Success):**
```json
{
  "success": true,
  "traceId": "0HN...:00000002",
  "data": null
}
```
*(Catatan: Endpoint ini digunakan pada layar sebelumnya untuk mengubah status keseluruhan *Order* menjadi aktif/dimulai).*

---

## 4. Info Pengiriman (Active Route Execution)

Tampilan ini adalah pusat komando bagi Driver saat sedang dalam perjalanan (sedang menjalankan Job). Layar ini menampilkan status tiap-tiap titik penjemputan (*Route Stops*), jumlah barang/Kanban yang harus diambil, serta aksi untuk menyelesaikan rute secara keseluruhan.

<img src="../assets/images/info-pengiriman.png" width="300" alt="Info Pengiriman UI" />

### Analisis UI Terhadap Arsitektur API

Berdasarkan *screenshot* yang Anda unggah, berikut adalah pemetaan teknis yang relevan dengan `JobController`:

#### A. Data Header & Timeline (Read)
Seluruh informasi di layar ini (mulai dari *Route & Cycle*, *Delivery No.*, hingga daftar *1st Pickup*, *2nd Pickup*) menggunakan respons dari endpoint `Route Stops` yang sama seperti di Bagian 3, namun kali ini mencerminkan progres nyata.

- **URL:** `GET /api/v1/jobs/{id}/route-stops`
- **Mapping ke UI:**
  - `status: "COMPLETED"` (seperti pada ADVICS INDONESIA) -> Menampilkan *badge* hijau **✓ Picked Up** dan mengubah tombol aksi menjadi tombol Edit (warna jingga).
  - `status: "PENDING"` (seperti pada PT. INDONESIA THAI SUMMIT PLAS) -> Menampilkan tombol arah panah biru (➡) untuk memulai proses pengambilan barang di titik tersebut.
  - Jumlah Kanban (Original, Others, EO) -> Di-render dari objek `original`, `others`, dan `eo` (menampilkan format `scanned / total`).
  - *Color coding*: Warna teks hijau (misal `8/8`) jika `scanned == total`, dan merah muda (misal `0/3`) jika `scanned < total`.

#### B. Tombol Panah Biru & Tombol Edit (Aksi Navigasi)
Tombol panah biru pada pemberhentian yang masih *Pending*, maupun tombol *Edit* jingga pada pemberhentian yang sudah *Picked Up*, **tidak memanggil API secara langsung**. 
Kedua tombol tersebut berfungsi murni secara UI (*Client-side routing*) untuk mengarahkan pengguna ke layar berikutnya: **Layar Pemindaian Kanban (Scanner)**. 
- Di layar *Scanner* kelak, aplikasi akan menggunakan endpoint `POST /stops/{stopId}/manifests/{manifestId}/kanban`.
- Setelah pemindaian selesai, akan ada aksi untuk memanggil endpoint `POST /stops/{stopId}/complete` guna merubah status *Supplier* menjadi `Picked Up`.

#### C. Tombol Merah "END JOB" (Write)
Tombol berbentuk lingkaran merah di sudut kanan atas berfungsi untuk mengakhiri keseluruhan pekerjaan secara paksa atau normal jika semua titik sudah disinggahi.

- **URL:** `POST /api/v1/jobs/{id}/end`
- **Method:** `POST`
- **Auth:** Bearer Token (Driver)

**Request Body:**
```json
{
  "latitude": -6.312151,
  "longitude": 107.135422
}
```
*Catatan:* Sesuai *best practice*, aplikasi *Mobile* sebaiknya memunculkan dialog konfirmasi ("Apakah Anda yakin ingin mengakhiri rute ini?") sebelum memanggil endpoint ini, karena *End Job* bersifat final. Titik koordinat GPS (`latitude` & `longitude`) ditangkap secara *real-time* dari perangkat untuk validasi *geofencing*.

---

## 5. Detail Pengiriman (Kanban Scanner / Manifest List)

Tampilan ini muncul setelah Driver mengetuk ikon panah biru (➡) pada salah satu *Supplier* yang berstatus *Pending* di layar **Info Pengiriman**. Layar ini berfokus pada daftar *Manifest* yang harus dipenuhi oleh Driver di lokasi tersebut.

<img src="../assets/images/detail-pengiriman.png" width="300" alt="Detail Pengiriman UI" />

### Analisis UI Terhadap Arsitektur API

Berdasarkan analisis visual, **kita telah memiliki endpoint khusus** untuk menampilkan daftar Manifest per titik (*Stop*). Endpoint ini menyuplai data operasional (termasuk *Order Type*, *Total Skid*, dan *Dock Code*) dengan cepat berkat duplikasi arsitektur *microservices* di tabel `PickupOrderManifest`.

#### A. Fetching Daftar Manifest (Telah Diimplementasikan)
Layar ini menampilkan tabel dengan kolom `Manifest No`, `Type`, `Total SKID`, `Dock Code`, `No of Kanban`, dan `Status` (Checkbox/Silang). 

- **Draft URL:** `GET /api/v1/jobs/stops/{stopId}/manifests`
- **Mapping ke UI:**
  - `Type`: Berasal dari jenis tipe order (Original, Others, EO). Di contoh ini (ORG).
  - `No of Kanban`: Nilai target Kanban yang harus discan di manifest tersebut.
  - `Status`: Diwakili oleh lencana Checkbox Hijau (jika `scannedKanban == totalKanban`) atau Silang Merah (jika belum tercapai).

#### B. Tombol "Mulai Scan" (Navigasi Kamera)
Tombol biru ini murni merupakan pemicu *Hardware Camera* atau *Bluetooth Scanner* dari sisi klien (Mobile). Tidak ada pemanggilan API ke *Backend* saat tombol ini diklik, sampai sebuah *barcode* berhasil terbaca.

- Ketika barcode (contoh: `KBN-001`) berhasil terbaca oleh kamera:
  - **URL Terkait:** `POST /api/v1/jobs/stops/{stopId}/manifests/{manifestId}/kanban`
  - *Payload*: `{"kanbanCode": "KBN-001"}`
  - Jika berhasil, UI akan memperbarui *counter* di kotak atas (`Original 7/8` menjadi `8/8`).

#### C. Slider "Geser Selesai Pengambilan" (Complete Stop)
Ini adalah mekanisme pengunci untuk menandakan bahwa Driver telah selesai memuat semua barang di *Supplier* tersebut dan siap beranjak. Penggunaan komponen *Slider/Swipe* adalah **Best Practice** UI/UX untuk mencegah ketidaksengajaan klik (fat-finger errors) pada aksi krusial.

- **URL:** `POST /api/v1/jobs/stops/{stopId}/complete`
- **Method:** `POST`
- **Payload:**
```json
{
  "latitude": -6.312151,
  "longitude": 107.135422
}
```
*Catatan:* Backend akan memvalidasi apakah semua Manifest (atau Kanban wajib) sudah dipenuhi. Jika sukses, layar akan tertutup dan mengembalikan Driver ke layar **Info Pengiriman** dengan status *Supplier* berubah menjadi **✓ Picked Up**.

---

## 6. Pindai Kanban (Camera Scanner)

Layar ini muncul saat Driver menekan tombol **"Mulai Scan"** dari layar Detail Pengiriman. Kamera akan diaktifkan untuk membaca *barcode* atau *QR code* pada fisik kartu Kanban.

<img src="../assets/images/scan-kanban.png" width="300" alt="Scan Kanban UI" />

### Analisis UI Terhadap Arsitektur API

Berdasarkan penelusuran pada *source code* (khususnya `JobController.cs`), **kita telah memiliki endpoint khusus** untuk menangani pindaian (*scan*) ini.

- **URL API:** `POST /api/v1/jobs/stops/{stopId}/manifests/{manifestId}/kanban`
- **Method:** `POST`
- **Request Payload:**
  ```json
  {
    "kanbanCode": "KODE-BARCODE-KANBAN-YANG-TERBACA"
  }
  ```

#### Mekanisme Pemrosesan Backend (Sudah Tersedia)
Ketika Mobile UI mengirimkan `kanbanCode` ke *endpoint* di atas, sistem kita (*CQRS Command `ScanKanbanCommand`*) akan melakukan validasi secara seketika:
1. Memastikan kode Kanban tersebut benar-benar **terdaftar** untuk dijemput pada *Manifest* ini (mencegah salah angkut barang pabrik lain).
2. Memastikan Kanban tersebut **belum pernah di-scan** sebelumnya (mencegah *double scan*).
3. Memberi stempel waktu (*timestamp*) pada kolom `ScannedAt` di tabel `pickup_order_kanbans`.
4. Menambahkan konter `ScannedKanban` (+1) pada tabel `pickup_order_manifests`.
5. Mengembalikan *Response* `200 OK`, agar UI dapat memicu bunyi *"Beep"* hijau dan menambahkan angka indikator di layar (contoh: *Original* bertambah menjadi `8/8`).

---

## 7. Manifest Detail

Layar ini muncul apabila Driver mengetuk salah satu baris (*row*) *Manifest* pada layar **Detail Pengiriman**. Tujuannya adalah untuk menampilkan rincian barang (suku cadang/*part*) yang terkandung di dalam *manifest* tersebut, beserta jumlah boks/kanban.

<img src="../assets/images/manifest-detail.png" width="300" alt="Manifest Detail UI" />

### Analisis UI Terhadap Arsitektur API

Berdasarkan penelusuran kode, saat ini **kita BELUM memiliki endpoint khusus** untuk menampilkan rincian *Part List* ini. Endpoint operasional yang baru saja kita buat (`GetManifests`) hanya menampilkan daftar *header* Manifest-nya saja.

- **Kebutuhan Draft URL Baru:** `GET /api/v1/manifests/{manifestNo}/detail` (Sebaiknya berada di luar modul operasional `Job`).
- **Mapping Data yang Diperlukan:**
  - **Header**: `Route & Cycle`, `Delivery No.`, `Manifest No`, `Total Kanban`, `Order No`, `Dock Code`, `P-Lane No`.
  - **Tabel Part List**: Terdiri dari rincian agregasi `Part No.`, `Uniq No`, `Pcs/Kbn` (Kuantitas per Kanban), `Box Type`, dan `No. Of Kbn` (misalnya 2/2, yang berarti tipe part ini membutuhkan 2 kotak kanban).

#### Tantangan Arsitektur (Microservices Trade-off)
Tabel operasional kita saat ini (`PickupOrderManifest` dan `PickupOrderKanban` di dalam modul `Job`) **sengaja tidak menyimpan** rincian detail *parts* (`Part No`, `Uniq No`, `Box Type`, dll.). Hal tersebut murni merupakan data rekam jejak statis dari IDCS.

Sesuai **Best Practice Microservices**:
*Endpoint* rincian daftar *parts* ini sebaiknya **tidak dibangun** di dalam `JobController`, melainkan disediakan oleh modul pengumpul data (misalnya **Ingestion Module** atau **Master Module**) yang menguasai tabel `MANIFEST`, `MANIFEST_PART`, dan `MANIFEST_KANBAN`. 

UI Mobile cukup mengambil `Manifest No` (dari layar sebelumnya), lalu memanggil API *Read-Only* ke modul tersebut secara langsung tanpa membebani modul `Job` yang sedang sibuk memproses *Scanning*.

---

*(Dokumen ini akan terus diperbarui secara bertahap setiap kali Anda mengunggah tangkapan layar UI berikutnya).*
