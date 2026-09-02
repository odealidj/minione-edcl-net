# API Sequence Diagrams

Sequence diagram endpoint utama sistem **EDCL Mini** dikelompokkan berdasarkan modul.

---

## 1. Module Auth

### 1.1. Driver Login (2-Step OTP Verification)
**Endpoints:** `POST /api/v1/auth/request-otp` & `POST /api/v1/auth/login`

```mermaid
sequenceDiagram
    autonumber
    actor Driver
    participant Gateway as API Gateway (YARP)
    participant Ctrl as AuthController
    participant MediatR as MediatR Pipeline
    participant Hndl as CommandHandlers
    participant Repo as DriverRepository
    participant Cache as Redis (ICachePort)
    participant Jwt as JwtTokenService
    participant DB as SQL Server (Auth)

    rect rgb(240, 248, 255)
    Note over Driver, DB: Step 1: Request OTP
    Driver->>Gateway: POST /request-otp (PhoneNumber)
    Gateway->>Ctrl: Forward Request
    Ctrl->>MediatR: Send(RequestOtpCommand)
    MediatR->>Hndl: Handle()
    Hndl->>Repo: FindActiveByPhoneAsync()
    Repo-->>Hndl: Return Driver Entity
    Hndl->>Hndl: Generate 4-digit PIN
    Hndl->>Cache: SetAsync(OtpKey, PIN, TTL: 3 mins)
    Cache-->>Hndl: OK
    Hndl-->>Ctrl: Success Result
    Ctrl-->>Gateway: HTTP 200 OK
    Gateway-->>Driver: HTTP 200 OK
    end

    rect rgb(255, 250, 240)
    Note over Driver, DB: Step 2: Verify PIN & Login
    Driver->>Gateway: POST /login (PhoneNumber, PIN)
    Gateway->>Ctrl: Forward Request
    Ctrl->>MediatR: Send(LoginCommand)
    MediatR->>Hndl: Handle()
    Hndl->>Repo: FindActiveByPhoneAsync()
    Repo-->>Hndl: Return Driver Entity
    Hndl->>Cache: GetAsync(OtpKey)
    Cache-->>Hndl: Return stored PIN
    
    alt PIN Invalid / Expired
        Hndl-->>Ctrl: Error Result (Invalid Credentials)
        Ctrl-->>Gateway: HTTP 401 Unauthorized
        Gateway-->>Driver: HTTP 401 Unauthorized
    else PIN Valid
        Hndl->>Cache: RemoveAsync(OtpKey)
        Hndl->>Jwt: GenerateAccessToken(Claims, Role: Driver)
        Jwt-->>Hndl: Access Token
        Hndl->>Jwt: GenerateRefreshToken()
        Jwt-->>Hndl: Refresh Token
        Hndl->>Repo: SaveRefreshToken(Hash, Expiry)
        Repo->>DB: Insert/Update Token
        Hndl-->>Ctrl: Success Result
        Ctrl-->>Gateway: HTTP 200 OK
        Gateway-->>Driver: HTTP 200 OK (Tokens)
    end
    end
```

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
        Ctrl-->>Client: HTTP 401 Unauthorized
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

### 2.1. Start Job
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
    Hndl->>DB: SaveChangesAsync() (Transactional Outbox)
    
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

### 2.3. Complete Stop
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
    
    Hndl->>Hndl: Validate Geofence (< 500m) & Scan Completeness
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

### 3.1. Query Manifests / Parts / Kanbans
**Endpoint:** `GET /api/v1/manifests/...`

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant Ctrl as CargoController
    participant Hndl as QueryHandler
    participant DB as SQL Server (Ingestion)

    Client->>Ctrl: GET /manifests/{id}/kanbans?page=1
    Ctrl->>Hndl: Send(GetManifestKanbanDetailsQuery)
    Hndl->>DB: Execute Dapper Raw SQL with OFFSET-FETCH
    DB-->>Hndl: Data List & Total Count
    Hndl->>Hndl: Map to PaginatedResult
    Hndl-->>Ctrl: Result
    Ctrl-->>Client: HTTP 200 OK (Paginated JSON)
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
