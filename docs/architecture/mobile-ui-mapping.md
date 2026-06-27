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
*Catatan:* Setelah menerima `success: true`, Frontend harus me-redirect driver ke layar navigasi (maps) menuju pemberhentian pertama.

---

*(Dokumen ini akan terus diperbarui secara bertahap setiap kali Anda mengunggah tangkapan layar UI berikutnya).*
