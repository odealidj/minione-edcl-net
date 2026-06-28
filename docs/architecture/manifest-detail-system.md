# Arsitektur Sistem: Manifest Detail

Dokumen ini memuat detail implementasi teknis untuk layar Manifest Detail yang mengambil rincian komponen suku cadang (*Parts*) langsung dari **Cargo Module (Ingestion)** menggunakan pola *Client-Side Composition*.

## 1. Domain Entities & Database Mapping

Modul Cargo menyimpan struktur *master data* dari IDCS (sistem Ingestion Eksternal). Untuk mengakomodasi tampilan Mobile, entitas telah diperluas.

### a. `Manifest` Entity (`ingestion.manifests`)
Menyimpan keseluruhan dokumen *header* operasional:
- `manifest_no` (string) - PK/Identitas unik.
- `order_no` (string) - Nomor pesanan dari pabrik.
- `dock_cd` (string) - Kode *Dock* penjemputan barang.
- `p_lane_no` (string) - Kode Lajur (*Lane*).

### b. `ManifestPart` Entity (`ingestion.manifest_parts`)
Menyimpan komponen suku cadang untuk satu manifest:
- `part_no` (string) - Kode spesifik suku cadang.
- `uniq_no` (string) - Kode unik dari supplier untuk identifikasi pembeda *part*.
- `box_type` (string) - Tipe boks (misal: TP364, BK40).
- `qty` (int) - Menandakan *Pcs/Kbn* (Kuantitas per Kanban).

### c. `ManifestKanban` Entity (`ingestion.manifest_kanbans`)
Menyimpan kode bar/kanban spesifik per komponen.

## 2. API Contract

### Endpoint
`GET /api/v1/cargo/manifests/{manifestNo}/detail`

### DTO (Data Transfer Object)
Responsibilities:
Kueri `GetManifestDetailQueryHandler` merelasikan (melakukan *Include*) properti `Parts` dan `Kanbans`, lalu menghitung kemunculan *Kanban* berdasarkan *PartNo* untuk mencetak string rasio "Total Kanban" pada kolom `NoOfKbn`.

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

## 3. Client-Side Composition

Sesuai pola *Microservices*, API ini tidak mengembalikan informasi `DeliveryNo` maupun `RouteCycle` secara *native*, melainkan menggantinya dengan nilai bawaan `-` atau menyisakan ruang tersebut. 
Alasannya: Properti `DeliveryNo` dan `RouteCycle` sepenuhnya dimiliki oleh **Job Module**. 

Oleh karena itu, pada sisi *Mobile Frontend*:
1. Mengambil State `DeliveryNo` dan `RouteCode` dari layar *Info Pengiriman* sebelumnya.
2. Memanggil API Master Data ini.
3. Menggabungkan hasilnya ke dalam komponen *Header* di UI.

## 4. Keuntungan Skalabilitas
- **No Cross-Database JOINs**: Kita terhindar dari pembuatan kueri JOIN berat lintas modul (*Job* dan *Cargo*) yang dapat berisiko menyebabkan *Database Deadlock* atau penurunan performa secara drastis saat trafik penjemputan tinggi.
- **Isolasi Beban Kerja (Fault Tolerance)**: Jika *Master Data* di Ingestion sedang dalam perbaikan (menurun), Driver masih tetap dapat melakukan proses Pemindaian (*Scan*) Kanban karena modul `Job` mandiri dari modul ini.
