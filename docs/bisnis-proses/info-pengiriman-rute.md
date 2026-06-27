# Proses Bisnis: Eksekusi Rute (Info Pengiriman)

Dokumen ini memaparkan alur operasional di lapangan ketika Driver sedang menjalankan tugas penjemputan barang menggunakan fitur **Info Pengiriman** pada aplikasi EDCL Mini.

## 1. Tujuan Bisnis
1. **Navigasi Pekerjaan**: Memberikan panduan urutan kunjungan *Supplier* (pemasok) yang harus disinggahi oleh Driver dalam satu siklus (Route & Cycle).
2. **Visibilitas Progres**: Menampilkan status *real-time* mengenai titik mana saja yang sudah selesai dikerjakan (Picked Up) dan barang apa yang wajib diangkut.
3. **Pencatatan Waktu & Lokasi**: Memastikan bahwa pekerjaan diselesaikan sesuai dengan estimasi waktu (ETA/ETD) dan dihentikan di lokasi yang valid.

## 2. Aktor yang Terlibat
- **Driver (Pengemudi)**: Pengguna utama yang mengeksekusi instruksi di lapangan.
- **Sistem Pusat (Backend)**: Pengawas dan penyedia data rute terkini.

## 3. Skenario Operasional (Info Pengiriman)

### Skenario A: Membuka Daftar Pekerjaan Aktif
Skenario ini terjadi saat Driver baru saja mengeklik "Start Job" atau saat mereka kembali ke layar Info Pengiriman di tengah perjalanan.

1. Driver melihat detail di layar:
   - **Nomor Delivery & Rute**: Sebagai identitas tugas.
   - **Waktu Mulai**: Waktu order tersebut dijadwalkan.
2. Di bawah header, terdapat daftar pemberhentian (*Stops*):
   - **1st Pickup, 2nd Pickup, dst.**
   - Nama perusahaan *Supplier* dan kode *Supplier*.
   - Target waktu kedatangan (ETA) dan waktu keberangkatan (ETD).
   - Jumlah Kanban yang dijadwalkan vs aktual.

### Skenario B: Melakukan Pengambilan Barang (Pickup)
1. Driver melihat pemberhentian yang masih berstatus *Pending* (ditandai dengan panah navigasi biru).
2. Setibanya di lokasi pabrik/supplier, Driver mengetuk tombol panah tersebut untuk masuk ke Mode Pemindaian (*Scanner*).
3. Driver memindai semua Kanban fisik menggunakan kamera HP (detail *scanning* dijelaskan pada dokumen terpisah).
4. Setelah seluruh barang dipindai dan dimasukkan ke truk, Driver menekan tombol "Selesai" di layar *Scanner*.
5. Sistem mengunci data untuk supplier tersebut.
6. Pada layar Info Pengiriman, status *Supplier* akan berubah secara visual menjadi lencana hijau bertuliskan **"Picked Up"**.
7. Tombol panah biru berganti menjadi tombol Edit (Jingga) jika sewaktu-waktu ada koreksi barang yang tertinggal saat di lokasi.

### Skenario C: Menyelesaikan Seluruh Rute (End Job)
1. Setelah Driver mendatangi seluruh *Supplier* di daftar, mereka akan meluncur menuju destinasi akhir (misalnya gudang/pabrik pusat).
2. Setibanya di sana, Driver mengeklik tombol merah melingkar bertuliskan **"END JOB"**.
3. Aplikasi akan meminta konfirmasi.
4. Aplikasi menangkap koordinat GPS Driver untuk memastikan bahwa tugas diakhiri di zona yang valid (*Geofencing* opsional).
5. Pekerjaan tertutup, status keseluruhan Order menjadi selesai (COMPLETED).
6. Layar kembali ke Beranda (Dashboard) dan Driver siap menerima rute selanjutnya.
