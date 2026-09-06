# UI to API Mapping (Mobile Driver App)

Pemetaan antarmuka aplikasi Mobile Driver ke endpoint backend **EDCL Mini**.

---

## 1. Driver Login (PIN Authentication)

<img src="../assets/images/login-driver-otp.png" width="300" alt="Driver Login UI" />

### A. Eksekusi Login
- **URL**: `POST /api/v1/mobile/auth/drivers/login`
- **Payload**:
  ```json
  {
    "phoneNumber": "08123456789",
    "pin": "123456"
  }
  ```
- **Response (200 OK)**:
  ```json
  {
    "success": true,
    "data": {
      "driverId": 1,
      "name": "LISTIONO",
      "nik": "DRV-001",
      "photoUrl": "https://...",
      "logisticPartnerName": "PT. BINTANG",
      "accessToken": "eyJhbGci...",
      "refreshToken": "d2FkYm...",
      "accessTokenExpiresAt": "2026-04-21T10:15:00Z",
      "refreshTokenExpiresAt": "2026-05-21T10:00:00Z"
    }
  }
  ```

### B. Force Change PIN (PIN Default)
Jika pengemudi masih menggunakan PIN default, API mengembalikan kode `Auth.ForceChangePin`:

<img src="../assets/images/simple_change_pin_ui.png" width="300" alt="Force Change PIN UI" />

- **URL**: `POST /api/v1/mobile/auth/drivers/change-pin`
- **Payload**:
  ```json
  {
    "phoneNumber": "08123456789",
    "oldPin": "123456",
    "newPin": "654321"
  }
  ```

---

## 2. Home / Dashboard

<img src="../assets/images/home-dashboard.png" width="300" alt="Home Dashboard UI" />

- **URL**: `GET /api/v1/mobile/jobs/dashboard`
- **Auth**: `Bearer JWT Token`
- **Response**:
  ```json
  {
    "success": true,
    "data": {
      "profile": {
        "name": "LISTIONO",
        "photoUrl": "https://...",
        "logisticPartnerName": "PT. BINTANG"
      },
      "currentJob": {
        "pickupOrderId": 1,
        "routeCode": "RD23",
        "cycle": "01",
        "deliveryNo": "R202402010081",
        "pickupDate": "25 Apr 2026",
        "time": "01:00",
        "truckPlate": "B 9607 PXT"
      },
      "nextJob": null
    }
  }
  ```

---

## 3. Notification Detail / Route Summary

<img src="../assets/images/notification-new-job.png" width="300" alt="Notification New Job UI" />

### A. Route Stops & Arrival Plan
- **URL**: `GET /api/v1/mobile/jobs/{id}/route-stops`
- **Response**:
  ```json
  {
    "success": true,
    "data": {
      "routeCode": "RD23",
      "cycle": "01",
      "deliveryNo": "DLV-001",
      "date": "21 Apr 2026",
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
        }
      ]
    }
  }
  ```

### B. Mulai Pekerjaan
- **URL**: `POST /api/v1/mobile/jobs/{id}/start`
- **Headers**: `X-Idempotency-Key: <UUID>`

---

## 4. Info Pengiriman (Active Route Execution)

<img src="../assets/images/info-pengiriman.png" width="300" alt="Info Pengiriman UI" />

- **Data Route**: `GET /api/v1/mobile/jobs/{id}/route-stops`
- **End Job**: `POST /api/v1/mobile/jobs/{id}/end`
  - **Headers**: `X-Idempotency-Key: <UUID>`
  - **Payload**: `{"latitude": -6.312151, "longitude": 107.135422}`

---

## 5. Detail Pengiriman (Manifest List per Stop)

<img src="../assets/images/detail-pengiriman.png" width="300" alt="Detail Pengiriman UI" />

- **Fetch Manifests**: `GET /api/v1/mobile/jobs/stops/{stopId}/manifests`
- **Scan Barcode**: `POST /api/v1/mobile/jobs/stops/{stopId}/manifests/{manifestId}/kanban`
- **Complete Stop**: `POST /api/v1/mobile/jobs/stops/{stopId}/complete`

---

## 6. Pindai Kanban (Camera Scanner)

<img src="../assets/images/scan-kanban.png" width="300" alt="Scan Kanban UI" />

- **URL**: `POST /api/v1/mobile/jobs/stops/{stopId}/manifests/{manifestId}/kanban`
- **Payload**:
  ```json
  {
    "kanbanCode": "KBN-ADV-001"
  }
  ```

---

## 7. Manifest Detail (Parts Breakdown)

<img src="../assets/images/manifest-detail.png" width="300" alt="Manifest Detail UI" />

- **URL**: `GET /api/v1/mobile/cargo/manifests/{manifestNo}/detail`
- **Pola**: *Client-Side Composition* (menggabungkan data master parts `Cargo` dengan rute `Job`).
