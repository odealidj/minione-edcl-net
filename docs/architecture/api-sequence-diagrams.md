# API Sequence Diagrams

Dokumen ini memuat *Sequence Diagram* untuk seluruh endpoint yang ada di **EDCL Mini**. Diagram ini dirancang untuk memberikan pemahaman teknis mengenai alur request dari *Client* (Mobile/Web), melewati *API Gateway*, masuk ke *Modular Monolith (EDCL.Api)*, hingga dieksekusi oleh *Database* dan *Message Broker* (Outbox Pattern).

Agar mudah dipahami, dokumentasi ini dikelompokkan berdasarkan **Module**.

---

## Daftar Isi
1. [Module Auth (Authentication & Authorization)](#1-module-auth)
2. [Module Job (Core Operational)](#2-module-job)
3. [Module Cargo (Master Data Ingestion)](#3-module-cargo)
4. [Module Notification (Pusat Pemberitahuan)](#4-module-notification)

---

## 1. Module Auth
Module ini bertanggung jawab atas pembuatan token JWT, registrasi pengguna, dan manajemen sesi.

### 1.1. Driver Login
**Endpoint:** `POST /api/v1/auth/login`

```mermaid
sequenceDiagram
    autonumber
    actor Driver
    participant Gateway as API Gateway (YARP)
    participant Ctrl as AuthController
    participant MediatR as MediatR Pipeline
    participant Hndl as LoginCommandHandler
    participant Repo as DriverRepository
    participant Jwt as JwtTokenService
    participant DB as SQL Server (Auth)

    Driver->>Gateway: POST /login (Phone, Password)
    Gateway->>Ctrl: Forward Request
    Ctrl->>MediatR: Send(LoginCommand)
    MediatR->>Hndl: Handle()
    Hndl->>Repo: GetByPhone()
    Repo->>DB: Query Driver
    DB-->>Repo: Return Driver Entity
    Repo-->>Hndl: Return Driver Entity
    Hndl->>Hndl: Verify Password Hash (BCrypt)
    
    alt Password Invalid
        Hndl-->>MediatR: Error (Invalid Credentials)
        MediatR-->>Ctrl: Error Result
        Ctrl-->>Gateway: HTTP 401 Unauthorized
        Gateway-->>Driver: HTTP 401 Unauthorized
    else Password Valid
        Hndl->>Jwt: GenerateAccessToken(Claims, Role: Driver)
        Jwt-->>Hndl: Access Token
        Hndl->>Jwt: GenerateRefreshToken()
        Jwt-->>Hndl: Refresh Token
        Hndl->>Repo: SaveRefreshToken(Hash, Expiry)
        Repo->>DB: Insert/Update Token
        Hndl-->>MediatR: AuthResponse
        MediatR-->>Ctrl: Success Result
        Ctrl-->>Gateway: HTTP 200 OK
        Gateway-->>Driver: HTTP 200 OK (Tokens)
    end
```
**Penjelasan:**
- Driver login menggunakan Nomor HP dan Password. Sistem memverifikasi kredensial menggunakan BCrypt.
- Jika sukses, sistem meng-generate *Access Token* (umur pendek, misal 15 menit) dan *Refresh Token* (umur panjang, misal 30 hari).
- Refresh Token di-hash dan disimpan di database untuk mencegah pencurian token, sehingga bisa di-*revoke* (cabut akses) kapan saja.

### 1.2. AppUser Login (Web Admin)
**Endpoint:** `POST /api/v1/auth/users/login`

```mermaid
sequenceDiagram
    autonumber
    actor AppUser
    participant Gateway as API Gateway (YARP)
    participant Ctrl as AppUsersController
    participant MediatR as MediatR Pipeline
    participant Hndl as LoginAppUserCommandHandler
    participant Repo as AppUserRepository
    participant Jwt as JwtTokenService
    participant DB as SQL Server (Auth)

    AppUser->>Gateway: POST /users/login (Username, Password)
    Gateway->>Ctrl: Forward Request
    Ctrl->>MediatR: Send(LoginAppUserCommand)
    MediatR->>Hndl: Handle()
    Hndl->>Repo: GetByUsername()
    Repo->>DB: Query AppUser
    DB-->>Repo: Return AppUser Entity
    Repo-->>Hndl: Return AppUser Entity
    Hndl->>Hndl: Verify Password Hash (BCrypt)
    
    alt Password Invalid
        Hndl-->>MediatR: Error (Invalid Credentials)
        MediatR-->>Ctrl: Error Result
        Ctrl-->>Gateway: HTTP 401 Unauthorized
        Gateway-->>AppUser: HTTP 401 Unauthorized
    else Password Valid
        Hndl->>Jwt: GenerateAccessToken(Claims, AppUser.Role)
        Jwt-->>Hndl: Access Token
        Hndl->>Jwt: GenerateRefreshToken()
        Jwt-->>Hndl: Refresh Token
        Hndl->>Repo: SaveRefreshToken(Hash, Expiry)
        Repo->>DB: Insert/Update Token
        Hndl-->>MediatR: AuthResponse
        MediatR-->>Ctrl: Success Result
        Ctrl-->>Gateway: HTTP 200 OK
        Gateway-->>AppUser: HTTP 200 OK (Tokens)
    end
```
**Penjelasan:**
- Login untuk pengguna Web Admin (AppUser) menggunakan *Username* dan Password. 
- Alur intinya identik dengan login Driver, namun diarahkan ke `AppUsersController` dan tabel `app_users` untuk menjaga separasi entitas. Role code (misal: `ADMIN`) akan di-inject ke dalam JWT Claims.

### 1.3. Refresh Token
**Endpoint:** `POST /api/v1/auth/refresh`

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Ctrl as AuthController
    participant Hndl as RefreshTokenCommandHandler
    participant DB as SQL Server (Auth)
    participant Jwt as JwtTokenService

    Client->>Ctrl: POST /refresh (AccessToken, RefreshToken)
    Ctrl->>Hndl: Send(RefreshTokenCommand)
    Hndl->>Jwt: GetPrincipalFromExpiredToken(AccessToken)
    Hndl->>DB: GetStoredRefreshToken(UserId)
    
    alt Token Invalid / Expired
        Hndl-->>Ctrl: Error
        Ctrl-->>Client: HTTP 401
    else Token Valid
        Hndl->>Hndl: Verify RefreshToken Match
        Hndl->>Jwt: Generate New Access & Refresh Token
        Hndl->>DB: Update RefreshToken Hash
        Hndl-->>Ctrl: AuthResponse
        Ctrl-->>Client: HTTP 200 OK (New Tokens)
    end
```

---

## 2. Module Job
Module ini adalah pusat operasional di mana Driver menerima pekerjaan, mengeksekusi rute, men-scan barang, dan menyelesaikan tugas. Semua operasi tulis (Write) di modul ini menggunakan **Outbox Pattern**.

### 2.1. Start Job (Memulai Pekerjaan)
**Endpoint:** `POST /api/v1/jobs/{id}/start`

```mermaid
sequenceDiagram
    autonumber
    actor Driver
    participant Gateway as API Gateway
    participant Ctrl as JobController
    participant MediatR as MediatR
    participant Hndl as StartJobCommandHandler
    participant DB as SQL Server (Job)

    Driver->>Gateway: POST /jobs/{id}/start
    Gateway->>Gateway: Validate JWT & Rate Limit
    Gateway->>Ctrl: Forward
    Ctrl->>MediatR: Send(StartJobCommand)
    MediatR->>Hndl: Handle()
    Hndl->>DB: Get PickupOrder by Id
    DB-->>Hndl: Return Order
    
    Hndl->>Hndl: Validate Status == Pending
    Hndl->>Hndl: Order.Start() -> Change Status & Date
    
    Hndl->>Hndl: Add Domain Event (JobStartedEvent)
    Hndl->>DB: SaveChangesAsync()
    Note over DB: Transaksi ACID:<br/>1. Update Status<br/>2. Insert Outbox Event
    
    Hndl-->>MediatR: Success
    MediatR-->>Ctrl: Result
    Ctrl-->>Gateway: HTTP 200 OK
    Gateway-->>Driver: HTTP 200 OK
```

### 2.2. Scan Kanban
**Endpoint:** `POST /api/v1/jobs/stops/{stopId}/scan`

```mermaid
sequenceDiagram
    autonumber
    actor Driver
    participant Ctrl as JobController
    participant Hndl as ScanKanbanCommandHandler
    participant DB as SQL Server (Job)

    Driver->>Ctrl: POST /scan (StopId, KanbanCode)
    Ctrl->>Hndl: Send(ScanKanbanCommand)
    Hndl->>DB: Query PickupOrderDetail by StopId
    
    Hndl->>Hndl: Check if Kanban belongs to this Stop Manifests
    alt Invalid Kanban
        Hndl-->>Ctrl: NotFound / Error
        Ctrl-->>Driver: HTTP 400 Bad Request
    else Valid Kanban
        Hndl->>Hndl: Create PickupOrderKanban (Scan Record)
        Hndl->>Hndl: Manifest.IncrementScanned()
        Hndl->>DB: SaveChangesAsync()
        Hndl-->>Ctrl: Success
        Ctrl-->>Driver: HTTP 200 OK
    end
```
**Penjelasan:**
- Scan divalidasi apakah barcode Kanban tersebut terdaftar dalam kumpulan Manifest di Stop (Supplier) tersebut.
- Jika cocok, sistem mencatat waktu *scan* dan meng-update *counter* validasi Manifest.

### 2.3. Complete Stop (Selesai Pickup di Supplier)
**Endpoint:** `POST /api/v1/jobs/stops/{stopId}/complete`

```mermaid
sequenceDiagram
    autonumber
    actor Driver
    participant Ctrl as JobController
    participant Hndl as CompleteStopCommandHandler
    participant DB as SQL Server (Job)
    participant Worker as Outbox Worker (Background)
    participant Broker as RabbitMQ

    Driver->>Ctrl: POST /complete (Lat, Long)
    Ctrl->>Hndl: Send(CompleteStopCommand)
    
    Hndl->>Hndl: Validate Geofence (Distance < 500m)
    Hndl->>Hndl: Validate All Manifests Scanned
    Hndl->>Hndl: Update Status -> PickedUp
    
    Hndl->>Hndl: Add DomainEvent (StopCompletedEvent)
    Hndl->>DB: SaveChangesAsync()
    Note over DB: Simpan perubahan & <br/>Simpan ke Outbox Tabel
    
    Hndl-->>Ctrl: Success
    Ctrl-->>Driver: HTTP 200 OK
    
    %% Asynchronous Processing
    loop Every 5 Seconds
        Worker->>DB: Poll Outbox Table
        DB-->>Worker: Unprocessed Events
        Worker->>Broker: Publish (StopCompletedEvent)
        Worker->>DB: Mark Event as Processed
    end
```
**Penjelasan:**
- Endpoint ini menggabungkan **Validasi Bisnis (Geofence & Validasi Scan)** dengan arsitektur **Outbox Pattern**.
- Respons diberikan seketika ke Driver agar aplikasi cepat.
- Event `StopCompletedEvent` dilempar ke RabbitMQ di latar belakang (asinkron), yang nantinya bisa ditangkap oleh Modul Notifikasi atau Reporting.

---

## 3. Module Cargo
Module Cargo melayani pembacaan *Master Data* yang cepat (Query) untuk kebutuhan operasional. Write biasanya dilakukan via sinkronisasi *Legacy System* (Ingestion Worker).

### 3.1. Get Manifests by Stop ID / Parts / Kanbans
**Endpoint:** `GET /api/v1/manifests/...`

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Ctrl as CargoController
    participant Hndl as GetManifest/Parts/KanbanQueryHandler
    participant DB as SQL Server (Ingestion)

    Client->>Ctrl: GET /manifests/{id}/kanbans?page=1
    Ctrl->>Hndl: Send(GetManifestKanbanDetailsQuery)
    
    Note over Hndl,DB: Gunakan DAPPER (Bukan EF Core) untuk High Performance Read
    Hndl->>DB: Execute SQL SELECT LEFT JOIN dengan OFFSET-FETCH
    DB-->>Hndl: Data List & Total Count
    
    Hndl->>Hndl: Map to PaginatedResult Dto
    Hndl-->>Ctrl: Result
    Ctrl-->>Client: HTTP 200 OK (Paginated JSON)
```
**Penjelasan:**
- Operasi `GET` (Queries) di aplikasi ini menggunakan pustaka **Dapper** langsung memanggil syntax SQL murni.
- Ini menghilangkan *overhead* dari *Entity Framework Core Tracking*, memastikan operasi baca sangat cepat untuk mendukung pagination di Web Admin.

---

## 4. Module Notification
Modul ini bertugas menyajikan list notifikasi ke aplikasi klien (lonceng notifikasi).

### 4.1. Get Notifications & Mark as Read
**Endpoint:** `GET /api/v1/notifications` | `POST /api/v1/notifications/{id}/read`

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Ctrl as NotificationController
    participant Hndl as GetNotificationsQueryHandler
    participant DB as SQL Server (Notification)

    Client->>Ctrl: GET /notifications
    Ctrl->>Hndl: Send(GetNotificationsQuery)
    Hndl->>DB: Query Notifications by DriverId (Dapper/EF)
    DB-->>Hndl: Unread List
    Hndl-->>Ctrl: Result
    Ctrl-->>Client: HTTP 200 OK
```

### 4.2. Penerimaan Event Asinkron (Event-Driven)
*(Proses yang berjalan di latar belakang tanpa HTTP Request)*

```mermaid
sequenceDiagram
    autonumber
    participant Broker as RabbitMQ
    participant Consumer as NotificationConsumer (Background)
    participant DB as SQL Server (Notification)
    participant Push as FCM/OneSignal (External)

    Broker-->>Consumer: Receive Event (e.g., JobAssignedEvent)
    Consumer->>Consumer: Parse Message
    Consumer->>DB: Insert Notification Record (Unread)
    Consumer->>Push: Trigger Push Notification API (Optional)
    Push-->>Consumer: OK
    Consumer-->>Broker: Ack Message
```
**Penjelasan:**
- Modul Notifikasi adalah *Consumer* utama dari sistem event-driven. Saat ada pekerjaan baru di-assign (dari Job Module) dan dimasukkan ke RabbitMQ via Outbox, Worker Notifikasi akan menangkapnya, mencatat ke database, dan men-trigger *Push Notification* ke HP Driver.

---

## 💡 Best Practices yang Diterapkan:
1. **API Gateway First**: Semua request wajib melewati Gateway untuk validasi JWT terpusat dan *Rate Limiting* guna mencegah serangan DDoS.
2. **CQRS Strict Separation**: Command (Write) mengubah state via EF Core, sementara Query (Read) via Dapper.
3. **Outbox Pattern**: Transaksi *database* lokal (*SQL Commit*) dan publikasi *event* (*Message Broker Publish*) tidak akan pernah *out-of-sync*.
4. **Idempotency**: Request modifikasi (POST/PUT) wajib menyertakan kunci Idempotency untuk menghindari eksekusi ganda jika *Client* retry akibat *timeout* jaringan.
