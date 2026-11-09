# 📦 KURIR TERAKHIR (A City Delivery Adventure Game)
> **Final Project — Mata Kuliah: Introduction to Game Development**  
> Game petualangan 3D eksplorasi perkotaan dan manajemen waktu (*Third-Person Delivery Adventure*) yang dikembangkan persis mengikuti dokumen narasi dan rancangan `History_Game_Kurir_Terakhir.docx`.

---

## 📌 Daftar Isi
1. [Kisah & Latar Belakang (Story)](#-kisah--latar-belakang-story)
2. [Spesifikasi Teknis](#-spesifikasi-teknis)
3. [Fitur & Mekanisme Gameplay](#-fitur--mekanisme-gameplay)
4. [Panduan Kontrol Pemain](#-panduan-kontrol-pemain)
5. [Daftar Karakter & Alur Misi 1 s/d 5](#-daftar-karakter--alur-misi-1-sd-5)
6. [Sistem 3 Variasi Ending](#-sistem-3-variasi-ending)
7. [Arsitektur Kode & Struktur Skrip](#-arsitektur-kode--struktur-skrip)
8. [Panduan Menjalankan Game (Quick Start)](#-panduan-menjalankan-game-quick-start)

---

## 📖 Kisah & Latar Belakang (Story)

Di sebuah kawasan perkotaan yang ramai dengan apartemen bertingkat, deretan pertokoan, *coffee shop*, *drug store*, *fire station*, dan area parkir, seorang kurir muda bernama **Rian** (*The Determined Rookie*) menerima tugas penting: mengantarkan lima paket terakhir sebelum toko-toko menutup pintunya. Jika semua paket berhasil sampai kepada penerima yang tepat sebelum batas waktu berakhir, Rian akan resmi diangkat menjadi kurir tetap.

Namun, label alamat pada paket terkena air hujan sehingga nama penerima dan nomor tujuan tidak terbaca jelas. Berbekal peta denah kota sederhana dan informasi singkat, Rian harus aktif mengamati lingkungan, membaca papan nama bangunan, dan berbicara dengan warga untuk mengurai petunjuk. Di tengah perjalanan, Rian juga harus menghindari lalu lalang kendaraan serta menyelamatkan paket yang sempat terjatuh di area parkir!

---

## ⚙️ Spesifikasi Teknis

| Parameter | Spesifikasi |
|---|---|
| **Game Engine** | Unity 6 (`6000.6.2f1`) |
| **Bahasa Pemrograman** | C# (.NET Standard / Unity Core) |
| **Render Pipeline** | Universal Render Pipeline (URP) |
| **Genre** | 3D Adventure, Exploration, Time Management |
| **Sudut Pandang** | Third-Person Follow Camera |
| **Batas Waktu** | 10 Menit (600 Detik) Countdown Timer |
| **Penalti Salah Kirim** | -20 detik per kesalahan pengiriman ke NPC |
| **Penalti Tabrakan Mobil** | -5 detik + Efek dorongan mundur (*knockback*) dan tertahan sesaat |
| **Target Scene** | `Assets/Module 2.unity` |
| **Font Utama** | `Roboto-Bold.ttf` (dengan *supersampling* 2.0x tajam bebas buram) |

---

## ✨ Fitur & Mekanisme Gameplay

### 1. ⏱️ Manajemen Waktu & Penalti Realistis
- **Countdown Timer 10:00:** Menghitung mundur secara *real-time*. Warna indikator berubah menjadi kuning saat waktu tersisa < 3 menit, dan merah berkedip saat < 1 menit.
- **Konsekuensi Kelalaian:**
  - Salah menyerahkan paket ke warga atau toko yang salah mengurangi waktu sebesar **-20 detik**.
  - Terserempet kendaraan yang melintas di jalan raya memicu efek terdorong mundur (*knockback*) dan penalti waktu **-5 detik** (dilengkapi *cooldown* 2 detik).

### 2. 💬 Dialog Interaktif Berurutan (Multi-Line Dialogue)
- Menampilkan balon percakapan di atas kepala karakter (*3D-to-2D projection*) dan panel subtitle sinematik di bagian bawah layar.
- Percakapan disampaikan berurutan (1–3 kalimat) dengan tombol navigasi `[E]` atau `[Spasi]` untuk lanjut membaca dengan nyaman.
- Kontrol karakter pemain dibekukan (*frozen*) otomatis selama berdialog agar fokus menyerap petunjuk.

### 3. 🗺️ Denah Kawasan Kota ([M])
- Pemain dapat menekan tombol **[M]** kapan saja untuk membuka peta denah sederhana yang menandai lokasi Area Parkir, Coffee Shop, Drug Store, Fire Station (Pos Pemadam), dan Gedung Apartemen.
- Tidak menggunakan panah GPS mengambang di atas target agar pemain aktif membaca papan nama toko sesuai esensi dokumen cerita.

### 4. 📦 Penyelamatan Paket Jatuh (Misi 4)
- Setelah menyelesaikan Misi 3, satu paket terjatuh di sela deretan kendaraan area parkir.
- Rian dapat meminta bantuan Petugas Parkir untuk memperoleh petunjuk lokasi jatuhnya paket, lalu memungutnya dengan tombol **[E]**.

---

## ⌨️ Panduan Kontrol Pemain

| Tombol / Input | Fungsi |
|---|---|
| **W, A, S, D** / **Panah** | Bergerak (Maju, Kiri, Mundur, Kanan) |
| **Shift Kiri** (Tahan) | Berlari Cepat (*Sprint*) |
| **Spasi** | Melompat (*Jump*) / Lanjut Dialog |
| **E** | **Interaksi Utama:** Bicara, Serahkan Paket, Ambil Paket Jatuh, Lanjut Dialog |
| **M** | Membuka / Menutup Peta Denah Kawasan Kota |
| **Geser Mouse** | Memutar Sudut Pandang Kamera (*Orbit 360°*) |
| **Scroll Wheel Mouse** | Memperbesar / Memperkecil Jarak Kamera (*Zoom In/Out*) |
| **C** atau **X** | Duduk Santai di Bangku Kota / Berdiri Kembali |

---

## 👥 Daftar Karakter & Alur Misi 1 s/d 5

```text
Area Parkir (Pak Beni) ──▶ Coffee Shop (Clara) ──▶ Drug Store (Pak Joko) ──▶ Fire Station (Dimas)
         ▲                                                                            │
         │                                                                            ▼
  Final Selesai ◀── Gedung Apartemen (Naya) ◀── Cari Paket Jatuh (Petugas Parkir) ◀───┘
```

1. **Pak Beni (`SimplePeople_Sheriff_Black`) — Petugas Pengiriman (Area Parkir):**
   - Memberikan 5 paket kiriman dan pengarahan batas waktu 10 menit. Menjadi titik lapor akhir pengiriman.
2. **Clara (`SimplePeople_Waitress_White`) — Pegawai Coffee Shop (Paket 1):**
   - Menunggu kiriman biji kopi di kedai kopi trotoar timur sebelum toko tutup. Memberi petunjuk lokasi Drug Store.
3. **Pak Joko (`SimplePeople_Prostitute_Brown`) — Pemilik Drug Store (Paket 2):**
   - Menunggu pasokan obat di sudut pertokoan barat. Memberi petunjuk lokasi Fire Station.
4. **Dimas (`SimplePeople_StreetMan_Black`) — Pegawai Fire Station (Paket 3):**
   - Menunggu kiriman perlengkapan darurat di pos Fire Station sebelah barat. Mengabarkan adanya paket kurir yang terjatuh di area parkir.
5. **Petugas Parkir (`SimplePeople_RoadWorker_White`) — Pengatur Area Parkir & Jalur Alternatif (Misi 4):**
   - Mengatur arus kendaraan di area parkir. Memberi informasi mengenai ruas jalan yang padat/terhalang dan memandu Rian memilih rute alternatif lewat trotoar dan lorong gedung, serta memberi petunjuk lokasi paket yang terlempar di sela mobil van parkir.
6. **Naya (`SimplePeople_BeachBabe_Black`) — Penghuni Apartemen (Paket 5):**
   - Menunggu kiriman dokumen keluarga penting di lobi pintu masuk apartemen bertingkat tinggi ujung kawasan.

---

## 🏆 Sistem 3 Variasi Ending

1. **Ending 1 — Kurir Terpercaya (Menang Sempurna):**
   - Seluruh 5 paket berhasil diantarkan ke penerima yang benar dan Rian kembali lapor ke Pak Beni sebelum batas waktu 10 menit habis.
   - *Epilog:* Rian membuktikan dedikasi serta kecepatannya dan resmi diangkat menjadi kurir tetap.
2. **Ending 2 — Pengiriman Terlambat:**
   - Batas waktu habis, namun sebagian besar paket (minimal 3 paket) telah berhasil diantarkan.
   - *Epilog:* Sebagian paket sampai tepat waktu, namun keterlambatan menjadi bahan evaluasi manajemen rute.
3. **Ending 3 — Gagal Bertugas:**
   - Batas waktu habis ketika kurang dari 3 paket yang berhasil diantarkan.
   - *Epilog:* Paket-paket harus dikembalikan ke pusat pengiriman dan tugas malam ini dinyatakan gagal.

---

## 📂 Arsitektur Kode & Struktur Skrip

Semua skrip permainan terletak di direktori [`Assets/Scripts/`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/FinalProject/Assets/Scripts):

- **[`DeliveryGameManager.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/FinalProject/Assets/Scripts/DeliveryGameManager.cs):**
  Pusat pengelola *state machine* misi, *countdown timer*, kalkulasi penalti waktu, validasi pengantaran, dan penentuan 3 cabang ending.
- **[`DeliveryHUD.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/FinalProject/Assets/Scripts/DeliveryHUD.cs):**
  Pembangun antarmuka programatis kanvas *Screen-Space Overlay* (Dashboard Timer, Catatan Misi, Prompt [E], Modal Peta [M], dan Layar Hasil Akhir).
- **[`DeliveryRecipient.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/FinalProject/Assets/Scripts/DeliveryRecipient.cs):**
  Komponen pada setiap NPC kota untuk menangani interaksi tombol [E], dialog sukses, dan respon penolakan salah alamat.
- **[`DroppedPackageItem.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/FinalProject/Assets/Scripts/DroppedPackageItem.cs):**
  Objek fisik kardus 3D di area parkir yang dapat dipungut pada Misi 4.
- **[`CarHazardTrigger.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/FinalProject/Assets/Scripts/CarHazardTrigger.cs):**
  Pendeteksi senggolan kendaraan yang memberi dorongan fisik (*knockback*) dan penalti waktu -5 detik.
- **[`PlayerController.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/FinalProject/Assets/Scripts/PlayerController.cs):**
  Pengendali fisik karakter Rian (WASD, sprint, jump, sit, freeze saat dialog, dan perlambatan pasca tabrakan).
- **[`NPCDialogueOverlay.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/FinalProject/Assets/Scripts/NPCDialogueOverlay.cs):**
  Sistem balon percakapan di atas kepala dan panel subtitle sinematik berurutan (*sequential dialogue*).
- **[`PlayerBootstrap.cs`](file:///d:/Mata%20Kuliah/Introduction%20to%20Game%20Development/FinalProject/Assets/Scripts/PlayerBootstrap.cs):**
  Titik masuk otomatis saat Play (▶) yang membersihkan objek modul lama dan mengonfigurasi kota menjadi game Kurir Terakhir.

---

## 🚀 Panduan Menjalankan Game (Quick Start)

1. Buka project di **Unity Editor**.
2. Buka scene utama: `Assets/Module 2.unity`.
3. *(Opsional)* Klik menu Unity: **Tools ➔ ⭐ Setup Game Kurir Terakhir (Final Project)** untuk menyelaraskan seluruh objek di Editor.
4. Klik tombol **Play (▶)** di bagian atas Editor.
5. Permainan langsung dimulai di **Area Parkir** di samping Pak Beni. Tekan tombol **[E]** untuk memulai tugas!
