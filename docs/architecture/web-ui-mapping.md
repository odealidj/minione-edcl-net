# Web UI to API Mapping (Functional Overview)

Dokumen ini memetakan tampilan antarmuka (UI) aplikasi Web Dashboard (Admin/Staff) dengan Endpoint API yang sesuai di sisi Backend (EDCL Mini).

---

## 1. Web Login (Admin & Staff)

Tampilan awal aplikasi Web untuk Admin Transporter atau Staff Pabrik masuk ke dalam sistem.

**Tampilan:** Halaman Login Web (`/login`)

**Deskripsi Alur:**
1. Pengguna memasukkan Email dan Password.
2. Web memanggil `POST /api/v1/auth/users/login` dengan kredensial tersebut.
3. Backend memverifikasi *hash* Password. Jika cocok, Backend menerbitkan JWT Access Token.
4. Token disimpan (misalnya di `localStorage` atau `HttpOnly Cookies`) dan pengguna diarahkan ke Dashboard.

### API Endpoints Terkait

#### A. Eksekusi Login
- **URL:** `POST /api/v1/auth/users/login`
- **Method:** `POST`
- **Auth:** *(None / Public)*

**Request Body:**
```json
{
  "email": "admin@edcl.com",
  "password": "Password123!" 
}
```

**Contoh Request (cURL):**
```bash
curl -X POST http://localhost:5000/api/v1/auth/users/login \
  -H "Content-Type: application/json" \
  -d '{"email": "admin@edcl.com", "password": "Password123!"}'
```

**Contoh Response (Success):**
```json
{
  "success": true,
  "traceId": "0HN...:00000003",
  "data": {
    "userId": "guid-uuid-string",
    "name": "Super Admin",
    "email": "admin@edcl.com",
    "roles": ["ADMIN"],
    "accessToken": "eyJhbGci...",
    "accessTokenExpiresAt": "2024-04-21T10:15:00Z"
  }
}
```

---

---

## 2. Manajemen Pengguna Web (Registrasi Web)

Tampilan untuk mendaftarkan akun baru bagi Admin Transporter atau Staff Pabrik. Saat ini bisa diakses *Public* untuk keperluan *setup* awal, namun ke depan akan diproteksi khusus Admin.

**Tampilan:** Halaman Tambah Pengguna Web

### API Endpoints Terkait

#### A. Registrasi Pengguna Web Baru
- **URL:** `POST /api/v1/auth/users/register`
- **Method:** `POST`
- **Auth:** *(None / Public sementara)*

**Request Body:**
```json
{
  "name": "Admin Bintang Logistik",
  "email": "admin.bintang@edcl.com",
  "password": "Password123!",
  "roleCode": "ADMIN"
}
```

**Contoh Request (cURL):**
```bash
curl -X POST http://localhost:5000/api/v1/auth/users/register \
  -H "Content-Type: application/json" \
  -d '{"name": "Admin Bintang Logistik", "email": "admin.bintang@edcl.com", "password": "Password123!", "roleCode": "ADMIN"}'
```

**Contoh Response (Success - 201 Created):**
```json
{
  "success": true,
  "traceId": "0HN...:00000005",
  "data": {
    "id": 2,
    "name": "Admin Bintang Logistik",
    "email": "admin.bintang@edcl.com",
    "roleCode": "ADMIN"
  }
}
```

---

## 3. Manajemen Driver (Registrasi Driver)

Tampilan untuk mendaftarkan Driver baru oleh Admin. Driver yang didaftarkan di sini secara otomatis akan mendapatkan PIN bawaan (misal: 123456) dan akan diwajibkan mengganti PIN saat masuk pertama kali di Aplikasi Mobile.

**Tampilan:** Halaman Tambah Driver

### API Endpoints Terkait

#### A. Registrasi Driver Baru
- **URL:** `POST /api/v1/auth/drivers`
- **Method:** `POST`
- **Auth:** Bearer Token (Diwajibkan)

**Request Body:**
```json
{
  "name": "Budi Santoso",
  "nik": "DRV-1029",
  "phoneNumber": "081999888777",
  "transporterId": 1
}
```

**Contoh Request (cURL):**
```bash
curl -X POST http://localhost:5000/api/v1/auth/drivers \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5c..." \
  -H "Content-Type: application/json" \
  -d '{"name": "Budi Santoso", "nik": "DRV-1029", "phoneNumber": "081999888777", "transporterId": 1}'
```

**Contoh Response (Success - 201 Created):**
```json
{
  "success": true,
  "traceId": "0HN...:00000006",
  "data": {
    "id": 5,
    "name": "Budi Santoso",
    "nik": "DRV-1029",
    "phoneNumber": "081999888777",
    "transporterId": 1
  }
}
```

---

*(Dokumen ini akan terus diperbarui secara bertahap seiring bertambahnya fitur Web).*
