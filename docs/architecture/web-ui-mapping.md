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

*(Dokumen ini akan terus diperbarui secara bertahap seiring bertambahnya fitur Web, seperti Manajemen Driver, Manajemen Rute, dll).*
