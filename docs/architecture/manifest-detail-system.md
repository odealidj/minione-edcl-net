# Arsitektur Sistem: Manifest Detail

Spesifikasi teknis query detail part manifes dari modul `Cargo` menggunakan pola **Client-Side Composition**.

---

## 1. Spesifikasi Endpoint & DTO Contract

**Endpoint**: `GET /api/v1/mobile/cargo/manifests/{manifestNo}/detail`

```csharp
public sealed record ManifestDetailDto(
    string RouteCycle,
    string DeliveryNo,
    string ManifestNo,
    int TotalKanban,
    string OrderNo,
    string DockCode,
    string PLaneNo,
    List<ManifestPartDto> PartList
);

public sealed record ManifestPartDto(
    int No,
    string PartNo,
    string UniqNo,
    int PcsKbn,
    string BoxType,
    string NoOfKbn // Contoh: "2/2", "5/5"
);
```

---

## 2. Sequence Diagram (Client-Side Composition)

```mermaid
sequenceDiagram
    participant UI as Mobile App (UI)
    participant State as Local State (Memory)
    participant Cargo as Cargo API (Backend)
    participant DB as Cargo DB (SQL)

    UI->>State: Ambil DeliveryNo & RouteCycle
    State-->>UI: { DeliveryNo, RouteCycle }
    
    UI->>Cargo: GET /api/v1/mobile/cargo/manifests/{manifestNo}/detail
    activate Cargo
    Cargo->>DB: Kueri EF Core .Include(Parts).Include(Kanbans)
    DB-->>Cargo: Data Entitas Manifest Lengkap
    
    Cargo->>Cargo: Hitung Aggregasi Kbn per Part
    Cargo-->>UI: ApiResponse<ManifestDetailDto>
    deactivate Cargo
    
    UI->>UI: Merge State Data dengan DTO
    UI->>UI: Render Header & Part List Table
```

---

## 3. Keunggulan Arsitektur
- **Zero Cross-Module SQL JOIN**: Modul `Cargo` dan `Job` terisolasi secara mandiri.
- **Client-Side Composition**: Frontend mobile menggabungkan data status operasional rute (`Job`) dengan rincian master part (`Cargo`).
