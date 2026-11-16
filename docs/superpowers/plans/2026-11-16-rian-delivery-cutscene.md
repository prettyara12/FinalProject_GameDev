# Rian Delivery Cutscene (Cinemachine & Timeline) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Membangun cutscene sinematik berdurasi ~10 detik menggunakan Unity Cinemachine dan Timeline di scene `Module 2`, yang menceritakan Rian kurir mengantar paket kepada para NPC (Pak Beni -> Clara di Coffee Shop -> Kota luas), lengkap dengan teks narasi/dialog di layar, audio jingle pembuka, dan opsi Skip ([Spasi]/[Esc]/UI).

**Architecture:** 
- Membuat Timeline Asset `RianDeliveryCutsceneTimeline.playable` (durasi 10 detik) dengan Cinemachine Track (3 shot ber-blend halus) dan Audio Track (`CutsceneJingle.wav`).
- Menyusun 3 Cinemachine Virtual Camera di `Module 2` (`CM_Delivery_Shot1_PakBeni`, `CM_Delivery_Shot2_ClaraCoffee`, `CM_Delivery_Shot3_CityStreet`).
- Menulis skrip runtime `RianDeliveryCutscene.cs` untuk mengelola pemutaran otomatis saat Play mode, pergantian teks dialog subtitle sesuai shot, dan fungsi Skip ([Spasi] / [Esc] / tombol UI) serta pemulihan kontrol player.
- Menyediakan Editor Tool `RianDeliveryCutsceneSetup.cs` untuk mengonfigurasi dan memvalidasi seluruh hierarki scene secara otomatis dan sekali klik.

**Tech Stack:** Unity 2022.3+, Cinemachine (com.unity.cinemachine 2.10.3), Unity Timeline (com.unity.timeline 6.6.0), Universal Render Pipeline (URP), C# scripting.

---

## Global Constraints

- Target Scene: `Assets/Module 2.unity`.
- Durasi Cutscene: Kurang lebih 10 detik (tepat 10.0 detik di Timeline).
- Auto-play: Dimulai secara otomatis begitu scene `Module 2` dimainkan (Play mode).
- Kontrol Gameplay: Input pemain (WASD, Space, Mouse Look) dinonaktifkan selama cutscene dan dipulihkan kembali ke `ThirdPersonCamera` dan `PlayerController` saat selesai.
- Opsi Skip: Pemain dapat menekan [Spasi] atau [Esc] atau klik tombol "Skip" untuk langsung melompat ke gameplay tanpa error.
- Non-Destructive: Tidak menghapus atau merusak setup kota, mobil, atau misi pengiriman 5 paket kurir yang sudah ada.

---

## Review Focus

1. **Transisi Kamera**: Cinemachine Virtual Camera harus ber-blend mulus antara Shot 1, Shot 2, dan Shot 3 tanpa glitch / patah.
2. **Pemulihan Kontrol**: Saat cutscene selesai di detik ke-10 (atau saat di-skip), kamera utama harus kembali mengendalikan `ThirdPersonCamera` dan karakter Rian bisa langsung digerakkan (WASD).
3. **Fungsi Skip**: Menekan tombol [Spasi] atau [Esc] di sembarang detik (misal detik ke-2 atau ke-8) harus menghentikan timeline seketika dan mengaktifkan gameplay tanpa error null reference.
4. **Sinkronisasi Subtitle**: Teks narasi di layar harus berubah tepat waktu sesuai pergantian shot karakter (Pak Beni -> Clara -> Rian).
5. **Kompilasi & Build Clean**: Tidak ada error CS di Unity `Editor.log`.

---

## Task Decomposition

### Task 1: Membuat Timeline Asset 10 Detik (Cinemachine Track & Audio Track)

**Files:**
- Create: `Assets/Timelines/RianDeliveryCutsceneTimeline.playable`
- Create: `Assets/Timelines/RianDeliveryCutsceneTimeline.playable.meta`

- [x] **Step 1.1**: Buat Timeline Asset `RianDeliveryCutsceneTimeline.playable` dengan durasi tepat 10 detik (600 frame pada 60 fps).
- [x] **Step 1.2**: Tambahkan `CinemachineTrack` berisi 3 `CinemachineShot`:
  - Shot 1 (Pak Beni): Start 0.0s, Durasi 3.6s, BlendOut 0.6s.
  - Shot 2 (Clara Coffee Shop): Start 3.0s, Durasi 4.0s, BlendIn 0.6s, BlendOut 0.6s.
  - Shot 3 (City Street Overview): Start 6.4s, Durasi 3.6s, BlendIn 0.6s, BlendOut 0.0s.
- [x] **Step 1.3**: Tambahkan `AudioTrack` yang memutar `CutsceneJingle.wav` pada detik 0.0s hingga 3.5s.
- [x] **Step 1.4**: Buat file meta yang sesuai dan verifikasi integritas file YAML playable asset.

---

### Task 2: Membuat Komponen Runtime `RianDeliveryCutscene.cs` (Subtitle UI, Skip, & Control Handlers)

**Files:**
- Create: `Assets/Scripts/RianDeliveryCutscene.cs`
- Create: `Assets/Scripts/RianDeliveryCutscene.cs.meta`

- [x] **Step 2.1**: Tulis skrip `RianDeliveryCutscene.cs` yang memiliki:
  - Referensi ke `PlayableDirector`, `CinemachineBrain`, `ThirdPersonCamera`, dan `PlayerController`.
  - Event listener `director.stopped` untuk mendeteksi penyelesaian cutscene.
  - Update loop untuk mendeteksi tombol Skip (`KeyCode.Space` dan `KeyCode.Escape`).
  - Method `SkipCutscene()` yang menghentikan timeline, mematikan UI subtitle, dan memulihkan kontrol pemain secara instan.
  - Sistem Subtitle UI yang menampilkan:
    - 0.0s - 3.5s: *"Pak Beni: 'Rian! Ini paket-paket terakhir malam ini. Antarkan segera sebelum toko dan apartemen tutup!' "*
    - 3.5s - 7.0s: *"Clara (Coffee Shop): 'Terima kasih banyak, Mas Rian! Paket biji kopinya sampai tepat waktu sebelum kedai tutup.' "*
    - 7.0s - 10.0s: *"Rian (Kurir): 'Siap, Pak! Seluruh paket akan saya antarkan tepat waktu malam ini!' "*
  - Menampilkan hint tombol Skip di pojok kanan atas: `[Spasi] Lewati Sinematik`.
- [x] **Step 2.2**: Pastikan saat cutscene dimulai, `ThirdPersonCamera` dan `PlayerController` dinonaktifkan, dan `CinemachineBrain` aktif. Saat selesai / di-skip, `CinemachineBrain` dinonaktifkan, UI subtitle ditutup, dan `ThirdPersonCamera` + `PlayerController` dipulihkan 100%.

---

### Task 3: Membuat Editor Setup Tool `RianDeliveryCutsceneSetup.cs`

**Files:**
- Create: `Assets/Editor/RianDeliveryCutsceneSetup.cs`
- Create: `Assets/Editor/RianDeliveryCutsceneSetup.cs.meta`

- [x] **Step 3.1**: Buat editor script dengan menu item `Tools/🎬 Setup Rian Delivery Cutscene (10 Detik)` yang secara otomatis:
  1. Membuka scene `Assets/Module 2.unity`.
  2. Memastikan `Main Camera` memiliki komponen `CinemachineBrain` yang terhubung.
  3. Membuat objek root `RianDeliveryCutscene_System`.
  4. Menyiapkan 3 Virtual Cameras:
     - `CM_Delivery_Shot1_PakBeni` pada posisi sekitar `(311.6, 0.45, 5.0)` menatap ke arah Pak Beni & Rian (`(310.05, 0.02, 6.06)`).
     - `CM_Delivery_Shot2_ClaraCoffee` pada posisi sekitar `(314.2, 0.45, 10.2)` menatap ke arah Clara & Rian di Coffee Shop (`(315.35, 0.02, 9.01)`).
     - `CM_Delivery_Shot3_CityStreet` pada posisi sekitar `(321.0, 1.8, -2.0)` menatap pemandangan koridor jalan kota luas (`(310.0, 0.5, 15.0)`).
  5. Menghubungkan Timeline Asset `RianDeliveryCutsceneTimeline.playable` ke `PlayableDirector` pada `RianDeliveryCutscene_System`.
  6. Mengikat (bind) trek Cinemachine Track ke 3 Virtual Camera tersebut.
  7. Menyiapkan Canvas UI Subtitle sinematik & tombol Skip.
  8. Memasang komponen `RianDeliveryCutscene` dengan konfigurasi `playOnStart = true`.
  9. Menyimpan scene `Module 2.unity`.
- [x] **Step 3.2**: Buat method batch execution agar setup dapat dijalankan langsung via CLI / script.

---

### Task 4: Eksekusi Setup Scene `Module 2.unity` & Validasi Otomatis

**Files:**
- Modify: `Assets/Module 2.unity`
- Modify: `Assets/Scripts/PlayerBootstrap.cs`

- [x] **Step 4.1**: Jalankan skrip setup untuk menyusun dan menyimpan cutscene ke dalam `Assets/Module 2.unity` serta integrasikan ke `PlayerBootstrap.cs`.
- [x] **Step 4.2**: Jalankan skrip validasi python untuk memverifikasi keberadaan:
  - `PlayableDirector` dengan durasi 10 detik.
  - 3 Virtual Cameras terpasang di posisi koordinat yang tepat.
  - `CinemachineBrain` pada Main Camera.
  - Komponen `RianDeliveryCutscene` terpasang dan `playOnStart = true`.
- [x] **Step 4.3**: Periksa `Editor.log` untuk memastikan tidak ada error kompilasi C# (0 compile errors).

---

### Task 5: Dokumentasi & Laporan Penyelesaian

- [x] **Step 5.1**: Buat panduan kontrol dan pengujian untuk pengguna (cara menguji Play mode, melihat cutscene 10 detik, dan mencoba tombol Skip).
- [x] **Step 5.2**: Berikan respon ringkas dengan tautan berkas `file:///` ke komponen yang telah dibuat.
