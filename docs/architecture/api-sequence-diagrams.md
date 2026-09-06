# API Sequence Diagrams

Sequence diagram endpoint utama sistem **EDCL Mini** dikelompokkan berdasarkan modul.

---

## 1. Module Auth

### 1.1. Driver Login & PIN Authentication (Mobile Driver)
**Endpoints:** `POST /api/v1/mobile/auth/drivers/login` & `POST /api/v1/mobile/auth/drivers/change-pin`

```mermaid
sequenceDiagram
    autonumber
    actor Driver
    participant Gateway as API Gateway (YARP)
    participant Ctrl as DriversAuthController
    participant MediatR as MediatR Pipeline
    participant Hndl as CommandHandlers
    participant Repo as DriverRepository
    participant Jwt as JwtTokenService
    participant DB as SQL Server (Auth)

    rect rgb(240, 248, 255)
    Note over Driver, DB: Step 1: Login dengan No HP & PIN
    Driver->>Gateway: POST /api/v1/mobile/auth/drivers/login (PhoneNumber, PIN)
    Gateway->>Ctrl: Forward Request
    Ctrl->>MediatR: Send(LoginCommand)
    MediatR->>Hndl: Handle()
    Hndl->>Repo: FindActiveByPhoneAsync()
    Repo->>DB: Query [auth].[drivers]
    DB-->>Repo: Return Driver Entity
    Repo-->>Hndl: Return Driver Entity
    Hndl->>Hndl: Verify PIN Hash (BCrypt)

    alt PIN Invalid / Not Found
        Hndl-->>Ctrl: Error Result (Invalid Credentials)
        Ctrl-->>Gateway: HTTP 401 Unauthorized
        Gateway-->>Driver: HTTP 401 Unauthorized
    else PIN Valid tapi Masih PIN Default (MustChangePin = true)
        Hndl-->>Ctrl: Error Code "Auth.ForceChangePin"
        Ctrl-->>Gateway: HTTP 401 Unauthorized (Auth.ForceChangePin)
        Gateway-->>Driver: HTTP 401 (Arahkan ke Layar Wajib Ganti PIN)
    else PIN Valid & Aktif
        Hndl->>Jwt: GenerateAccessToken(Claims, Role: Driver)
        Jwt-->>Hndl: Access Token
        Hndl->>Jwt: GenerateRefreshToken()
        Jwt-->>Hndl: Refresh Token
        Hndl->>Repo: SaveRefreshToken(Hash, Expiry)
        Repo->>DB: Insert/Update [auth].[driver_refresh_tokens]
        Hndl-->>Ctrl: Success Result (Tokens & Profile)
        Ctrl-->>Gateway: HTTP 200 OK
        Gateway-->>Driver: HTTP 200 OK (Tokens)
    end
    end

    rect rgb(255, 250, 240)
    Note over Driver, DB: Step 2: Ganti PIN Wajib (First-Time Login)
    Driver->>Gateway: POST /api/v1/mobile/auth/drivers/change-pin (Phone, OldPin, NewPin)
    Gateway->>Ctrl: Forward Request
    Ctrl->>MediatR: Send(ChangeDriverPinCommand)
    MediatR->>Hndl: Handle()
    Hndl->>Repo: FindActiveByPhoneAsync()
    Hndl->>Hndl: Verify Old PIN Hash & Re-hash New PIN
    Hndl->>Repo: UpdatePinAndMustChangePinAsync(false)
    Repo->>DB: Update [auth].[drivers]
    Hndl->>Jwt: GenerateAccessToken & RefreshToken
    Hndl->>Repo: SaveRefreshToken(Hash, Expiry)
    Hndl-->>Ctrl: Success Result
    Ctrl-->>Gateway: HTTP 200 OK (Tokens)
    Gateway-->>Driver: HTTP 200 OK (Masuk ke Dashboard)
    end
```

### 1.2. AppUser Login (Web Admin)
**Endpoint:** `POST /api/v1/web/auth/users/login`

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

    AppUser->>Gateway: POST /api/v1/web/auth/users/login (Email, Password)
    Gateway->>Ctrl: Forward Request
    Ctrl->>MediatR: Send(LoginAppUserCommand)
    MediatR->>Hndl: Handle()
    Hndl->>Repo: GetByEmailAsync()
    Repo->>DB: Query [auth].[app_users]
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
        Repo->>DB: Insert/Update [auth].[app_user_refresh_tokens]
        Hndl-->>MediatR: AuthResponse
        MediatR-->>Ctrl: Success Result
        Ctrl-->>Gateway: HTTP 200 OK
        Gateway-->>AppUser: HTTP 200 OK (Tokens)
    end
```

### 1.3. Refresh Token
**Endpoints:** `POST /api/v1/web/auth/users/refresh-token` & `POST /api/v1/mobile/auth/drivers/refresh-token`

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Ctrl as AuthController
    participant Hndl as RefreshTokenCommandHandler
    participant DB as SQL Server (Auth)
    participant Jwt as JwtTokenService

    Client->>Ctrl: POST /refresh-token (RefreshToken)
    Ctrl->>Hndl: Send(RefreshTokenCommand)
    Hndl->>DB: GetStoredRefreshToken(UserId)
    
    alt Token Invalid / Expired / Revoked
        Hndl-->>Ctrl: Error (Unauthorized)
        Ctrl-->>Client: HTTP 401 Unauthorized
    else Token Valid
        Hndl->>Hndl: Verify RefreshToken Match
        Hndl->>Jwt: Generate New Access & Refresh Token
        Hndl->>DB: Update RefreshToken Hash & Expiry
        Hndl-->>Ctrl: AuthResponse
        Ctrl-->>Client: HTTP 200 OK (New Tokens)
    end
```

---

## 2. Module Job

### 2.1. Start Job
**Endpoint:** `POST /api/v1/mobile/jobs/{id}/start`

```mermaid
sequenceDiagram
    autonumber
    actor Driver
    participant Gateway as API Gateway (YARP)
    participant Ctrl as MobileJobController
    participant MediatR as MediatR
    participant Hndl as StartJobCommandHandler
    participant DB as SQL Server (Job)

    Driver->>Gateway: POST /api/v1/mobile/jobs/{id}/start
    Gateway->>Gateway: Validate JWT & Rate Limit
    Gateway->>Ctrl: Forward
    Ctrl->>MediatR: Send(StartJobCommand)
    MediatR->>Hndl: Handle()
    Hndl->>DB: Get PickupOrder by Id
    DB-->>Hndl: Return Order
    
    Hndl->Hndl: Validate Status == Pending
    Hndl->>Hndl: Order.Start() -> Change Status & Date
    Hndl->>Hndl: Add Domain Event (JobStartedEvent)
    Hndl->>DB: SaveChangesAsync() (Transactional Outbox)
    
    Hndl-->>MediatR: Success
    MediatR-->>Ctrl: Result
    Ctrl-->>Gateway: HTTP 200 OK
    Gateway-->>Driver: HTTP 200 OK
```

### 2.2. Scan Kanban
**Endpoint:** `POST /api/v1/mobile/jobs/stops/{stopId}/manifests/{manifestId}/kanban`

```mermaid
sequenceDiagram
    autonumber
    actor Driver
    participant Ctrl as MobileJobController
    participant Hndl as ScanKanbanCommandHandler
    participant DB as SQL Server (Job)

    Driver->>Ctrl: POST /stops/{stopId}/manifests/{manifestId}/kanban (KanbanCode)
    Ctrl->>Hndl: Send(ScanKanbanCommand)
    Hndl->>DB: Query PickupOrderManifest & Kanbans
    
    alt Invalid Kanban / Double Scan
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

### 2.3. Complete Stop
**Endpoint:** `POST /api/v1/mobile/jobs/stops/{stopId}/complete`

```mermaid
sequenceDiagram
    autonumber
    actor Driver
    participant Ctrl as MobileJobController
    participant Hndl as CompleteStopCommandHandler
    participant DB as SQL Server (Job)
    participant Worker as Outbox Worker (Background)
    participant Broker as RabbitMQ

    Driver->>Ctrl: POST /stops/{stopId}/complete (Lat, Long)
    Ctrl->>Hndl: Send(CompleteStopCommand)
    
    Hndl->>Hndl: Validate Geofence & Scan Completeness
    Hndl->>Hndl: Update Status -> PickedUp
    Hndl->>Hndl: Add DomainEvent (StopCompletedEvent)
    Hndl->>DB: SaveChangesAsync() (Write status & outbox)
    
    Hndl-->>Ctrl: Success
    Ctrl-->>Driver: HTTP 200 OK
    
    loop Polling Outbox
        Worker->>DB: Poll Unprocessed Events
        Worker->>Broker: Publish (StopCompletedEvent)
        Worker->>DB: Mark Event as Processed
    end
```

---

## 3. Module Cargo

### 3.1. Query Manifest Detail (Parts Breakdown)
**Endpoint:** `GET /api/v1/mobile/cargo/manifests/{manifestNo}/detail`

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Ctrl as MobileCargoController
    participant Hndl as GetManifestDetailQueryHandler
    participant DB as SQL Server (Ingestion)

    Client->>Ctrl: GET /manifests/{manifestNo}/detail
    Ctrl->>Hndl: Send(GetManifestDetailQuery)
    Hndl->>DB: Query Manifest with Parts & Kanbans
    DB-->>Hndl: Data List & Parts
    Hndl->>Hndl: Map to ManifestDetailDto
    Hndl-->>Ctrl: Result
    Ctrl-->>Client: HTTP 200 OK (ManifestDetailDto JSON)
```

---

## 4. Module Notification

### 4.1. Event-Driven Notification Push
```mermaid
sequenceDiagram
    autonumber
    participant Broker as RabbitMQ
    participant Consumer as NotificationConsumer
    participant DB as SQL Server (Notification)
    participant Push as Firebase Cloud Messaging (FCM)

    Broker-->>Consumer: Receive Event (e.g. JobAssignedEvent)
    Consumer->>Consumer: Parse Message
    Consumer->>DB: Insert Notification Record (Unread)
    Consumer->>Push: Send FCM Push Notification
    Push-->>Consumer: OK
    Consumer-->>Broker: Ack Message
```
