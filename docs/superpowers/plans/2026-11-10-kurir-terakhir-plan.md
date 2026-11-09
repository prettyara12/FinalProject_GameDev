# Implementasi Game "Kurir Terakhir" Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Membangun game 3D pengiriman paket perkotaan "Kurir Terakhir" lengkap dengan 5 misi pengiriman berantai, mekanik paket jatuh, rintangan mobil, timer 10 menit dengan penalti waktu, dialog interaktif berurutan, dan 3 variasi ending sesuai dokumen `History_Game_Kurir_Terakhir.docx`.

**Architecture:** Menggunakan arsitektur terisolasi dan modular: `DeliveryGameManager` sebagai sentral status misi, waktu, dan ending; `DeliveryRecipient` pada tiap NPC untuk validasi paket dan dialog; `DeliveryHUD` untuk kanvas antarmuka (timer, kartu misi, prompt [E], peta kota [M], dan layar ending); `CarHazardTrigger` untuk tabrakan mobil; dan pembaruan `NPCDialogueOverlay` untuk dialog berurutan multi-line yang membekukan kontrol pemain saat percakapan.

**Tech Stack:** Unity 6 C# (.NET Core/Standard), Unity Universal Render Pipeline (URP), Unity UI (Screen-Space Overlay dengan dynamicPixelsPerUnit 2.0x), Text/Image, Synty SimpleTown & SimplePeople.

**Spec:** `docs/superpowers/specs/2026-11-10-kurir-terakhir-design.md`

## Global Constraints
- Target Scene: `Assets/Module 2.unity` (dibersihkan dari elemen tugas modul lama).
- Batas Waktu: 10 Menit (600 detik).
- Penalti Salah Kirim: -20 detik per kesalahan.
- Penalti Tabrakan Mobil: -5 detik + dorongan mundur dan perlambatan sesaat (0.7 detik).
- Tombol Interaksi: [E] untuk bicara, serahkan paket, atau ambil paket jatuh.
- Tombol Peta: [M] untuk membuka/menutup peta denah kota sederhana.
- Tiga Ending: Ending 1 (Kurir Terpercaya), Ending 2 (Pengiriman Terlambat), Ending 3 (Gagal Bertugas).
- Standar Ponytail: Minimal diff, YAGNI, jangan membuat abstraksi yang tidak diminta.
- Standar Antislop: Bebas pola generik AI, kontras tinggi, tipografi tajam, tanpa em-dash di UI, tombol fungsional.

## Review Focus
1. **Pencegahan Spam Interaksi [E]:** Menekan [E] berulang kali saat dialog sedang aktif tidak boleh menggandakan penalti waktu atau memicu dialog ganda.
2. **Kondisi Waktu Habis Saat Dialog Berlangsung:** Jika timer mencapai 00:00 saat dialog atau cutscene sedang terbuka, game harus menutup dialog secara mulus dan langsung menampilkan Ending Screen yang tepat.
3. **Penyelamatan Paket Jatuh (Misi 4):** Objek paket fisik tidak boleh dapat diambil sebelum Misi 4 dimulai, dan harus langsung hilang dari tanah saat diambil dengan tombol [E].
4. **Collision Immunity Pasca Tabrakan Mobil:** Setelah terserempet mobil, pemain memiliki masa kebal tabrakan (*invulnerability cooldown*) selama 2 detik agar tidak terkena penalti waktu beruntun dalam hitungan milidetik.
5. **Freeze & Unfreeze Kontrol Pemain:** Kontrol WASD pemain harus terkunci saat berdialog dan langsung terbuka kembali 100% normal saat dialog selesai atau dibatalkan.

---

### Task 1: Peningkatan Sistem Dialog Berurutan (Sequential Dialogue in NPCDialogueOverlay)

**Files:**
- Modify: `Assets/Scripts/NPCDialogueOverlay.cs`
- Modify: `Assets/Scripts/PlayerController.cs`

**Interfaces:**
- Produces:
  - `PlayerController.SetMovementFrozen(bool frozen)`
  - `NPCDialogueOverlay.ShowSequentialDialogue(Transform speaker, string speakerName, string[] lines, System.Action onComplete = null)`
  - `NPCDialogueOverlay.IsDialogueActive { get; }`

- [ ] **Step 1: Tambahkan method `SetMovementFrozen` pada `PlayerController.cs`**
  - Buat boolean `isMovementFrozen` di `PlayerController.cs`.
  - Jika `isMovementFrozen == true`, set input pergerakan menjadi nol dan pertahankan posisi diam/idle.
  - Sediakan method publik:
    ```csharp
    public void SetMovementFrozen(bool frozen)
    {
        isMovementFrozen = frozen;
        if (frozen && rb != null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }
    ```

- [ ] **Step 2: Tambahkan logika percakapan berurutan (*Multi-line Sequential Dialogue*) pada `NPCDialogueOverlay.cs`**
  - Tambahkan array `currentLines`, integer `currentLineIndex`, dan callback `System.Action dialogueCompleteCallback`.
  - Tambahkan elemen UI navigasi di panel subtitle: teks kecil di pojok kanan bawah `"Tekan [E] atau [Spasi] Lanjut ▶"`.
  - Tambahkan method publik:
    ```csharp
    public static void ShowSequentialDialogue(Transform speaker, string speakerName, string[] lines, System.Action onComplete = null)
    ```
  - Di dalam `Update()`: Jika `isSequentialActive`, deteksi `Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)`. Jika ditekan, pindah ke kalimat berikutnya (`currentLineIndex++`). Jika sudah kalimat terakhir, tutup dialog, panggil `onComplete`, dan buka freeze player.

- [ ] **Step 3: Uji Coba Sintaks & Kompilasi C#**
  - Jalankan script kompilasi / verifikasi untuk memastikan tidak ada error pada `PlayerController.cs` dan `NPCDialogueOverlay.cs`.

- [ ] **Step 4: Commit Task 1**
  ```bash
  git add Assets/Scripts/PlayerController.cs Assets/Scripts/NPCDialogueOverlay.cs
  git commit -m "feat: add sequential dialogue system and player movement freeze"
  ```

---

### Task 2: Implementasi State Machine Inti Game (`DeliveryGameManager.cs`)

**Files:**
- Create: `Assets/Scripts/DeliveryGameManager.cs`

**Interfaces:**
- Consumes:
  - `NPCDialogueOverlay.ShowSequentialDialogue`
  - `PlayerController.SetMovementFrozen`
- Produces:
  - `enum DeliveryMissionState { Prolog, Package1_CoffeeShop, Package2_DrugStore, Package3_HardwareStore, Package4_DroppedPackage, Package5_Apartment, Final_ReturnToHub, Completed, Failed }`
  - `enum GameEnding { None, Ending1_KurirTerpercaya, Ending2_PengirimanTerlambat, Ending3_GagalBertugas }`
  - `DeliveryGameManager.Instance`
  - `DeliveryGameManager.CurrentState { get; }`
  - `DeliveryGameManager.TimeRemaining { get; }`
  - `DeliveryGameManager.PackagesDelivered { get; }`
  - `DeliveryGameManager.WrongDeliveryCount { get; }`
  - `DeliveryGameManager.TryDeliverPackage(DeliveryRecipient recipient)`
  - `DeliveryGameManager.PickUpDroppedPackage()`
  - `DeliveryGameManager.ApplyCarHitPenalty()`

- [ ] **Step 1: Buat kelas `DeliveryGameManager.cs`**
  - Implementasikan Singleton pattern yang aman untuk Unity Play Mode:
    ```csharp
    public class DeliveryGameManager : MonoBehaviour
    ```
  - Deklarasikan variabel state:
    - `float timeRemaining = 600f; // 10 menit`
    - `int packagesDelivered = 0;`
    - `int wrongDeliveryCount = 0;`
    - `bool isTimerRunning = false;`
    - `DeliveryMissionState currentState = DeliveryMissionState.Prolog;`
    - `GameEnding finalEnding = GameEnding.None;`

- [ ] **Step 2: Implementasikan Logika Timer & Countdown**
  - Di `Update()`, jika `isTimerRunning` dan bukan saat dialog terbuka, kurangi `timeRemaining -= Time.deltaTime`.
  - Jika `timeRemaining <= 0`, hentikan timer dan panggil `HandleTimeExpired()`.
  - `HandleTimeExpired()` mengevaluasi:
    - Jika `packagesDelivered >= 3` -> `TriggerEnding(GameEnding.Ending2_PengirimanTerlambat)`.
    - Jika `packagesDelivered < 3` -> `TriggerEnding(GameEnding.Ending3_GagalBertugas)`.

- [ ] **Step 3: Implementasikan Logika Pengiriman & Penalti**
  - `TryDeliverPackage(DeliveryRecipient recipient)`:
    - Memeriksa apakah `recipient.targetForMission == currentState`.
    - Jika **Benar**:
      - Mainkan dialog sukses dari recipient.
      - Tambah `packagesDelivered++`.
      - Perbarui `currentState` ke misi berikutnya.
      - Jika selesai Misi 3, aktifkan objek paket jatuh di area parkir.
      - Jika selesai Misi 5, set state ke `Final_ReturnToHub`.
      - Jika di state `Final_ReturnToHub` dan bicara ke Pak Beni, trigger `GameEnding.Ending1_KurirTerpercaya`.
    - Jika **Salah**:
      - Mainkan dialog penolakan dari recipient.
      - Kurangi waktu: `ApplyWrongDeliveryPenalty() -> timeRemaining -= 20f`.
      - Tambah `wrongDeliveryCount++`.
  - `ApplyCarHitPenalty()`:
    - Kurangi waktu: `timeRemaining -= 5f`.
    - Memicu event flash penalti ke HUD.

- [ ] **Step 4: Uji Coba Kompilasi `DeliveryGameManager.cs`**
  - Pastikan script terkompilasi bersih tanpa peringatan atau error.

- [ ] **Step 5: Commit Task 2**
  ```bash
  git add Assets/Scripts/DeliveryGameManager.cs
  git commit -m "feat: implement DeliveryGameManager with timer, mission state machine, and endings"
  ```

---

### Task 3: Komponen Penerima Paket & Paket Fisik Terjatuh (`DeliveryRecipient.cs` & `DroppedPackageItem.cs`)

**Files:**
- Create: `Assets/Scripts/DeliveryRecipient.cs`
- Create: `Assets/Scripts/DroppedPackageItem.cs`

**Interfaces:**
- Consumes:
  - `DeliveryGameManager.Instance.TryDeliverPackage`
  - `DeliveryGameManager.Instance.PickUpDroppedPackage`
- Produces:
  - `DeliveryRecipient`: Komponen untuk NPC target dan NPC ambient.
  - `DroppedPackageItem`: Komponen paket 3D fisik yang dapat dipungut.

- [ ] **Step 1: Buat kelas `DeliveryRecipient.cs`**
  - Enum tipe karakter: `PakBeni, Clara, PakJoko, Dimas, Naya, PetugasParkir, AmbientCitizen`.
  - Bidang konfigurasi:
    - `public RecipientType recipientType;`
    - `public DeliveryMissionState targetForMission;`
    - `[TextArea(2, 4)] public string[] introDialogueLines;`
    - `[TextArea(2, 4)] public string[] successDialogueLines;`
    - `[TextArea(2, 4)] public string[] wrongDeliveryLines;`
    - `[TextArea(2, 4)] public string[] repeatHintLines;`
  - Method `public void InteractWithRian()`:
    - Memanggil `DeliveryGameManager.Instance.InteractWithRecipient(this)`.

- [ ] **Step 2: Buat kelas `DroppedPackageItem.cs`**
  - Objek kardus 3D dengan trigger collider.
  - Menampilkan partikel/highlight lembut saat aktif di Misi 4.
  - Method `public void PickUp()`:
    - Memanggil `DeliveryGameManager.Instance.PickUpDroppedPackage()`.
    - Memainkan efek suara / partikel singkat lalu menonaktifkan GameObject kardus.

- [ ] **Step 3: Uji Coba Kompilasi**
  - Verifikasi integritas script dan commit.

- [ ] **Step 4: Commit Task 3**
  ```bash
  git add Assets/Scripts/DeliveryRecipient.cs Assets/Scripts/DroppedPackageItem.cs
  git commit -m "feat: add DeliveryRecipient and DroppedPackageItem components"
  ```

---

### Task 4: Deteksi Hambatan Kendaraan (`CarHazardTrigger.cs`)

**Files:**
- Create: `Assets/Scripts/CarHazardTrigger.cs`
- Modify: `Assets/Scripts/CityCarMover.cs` (atau pasang langsung pada prefab mobil)

**Interfaces:**
- Consumes:
  - `DeliveryGameManager.Instance.ApplyCarHitPenalty`
  - `PlayerController`
- Produces:
  - Komponen pendeteksi tabrakan fisik mobil terhadap Rian.

- [ ] **Step 1: Buat kelas `CarHazardTrigger.cs`**
  - Memiliki `float knockbackForce = 4.0f;` dan `float hitCooldown = 2.0f;`.
  - Di `OnTriggerEnter(Collider other)` atau `OnCollisionEnter(Collision collision)`:
    - Deteksi apakah objek adalah Player (`PlayerController`).
    - Cek timer cooldown tabrakan terakhir (`Time.time - lastHitTime > hitCooldown`).
    - Jika valid:
      - Beri impuls dorongan mundur ke `Rigidbody` pemain menjauh dari mobil.
      - Panggil perlambatan gerak sesaat pada `PlayerController`.
      - Panggil `DeliveryGameManager.Instance.ApplyCarHitPenalty()`.
      - Perbarui `lastHitTime = Time.time`.

- [ ] **Step 2: Uji Coba Kompilasi**
  - Verifikasi script dan commit.

- [ ] **Step 3: Commit Task 4**
  ```bash
  git add Assets/Scripts/CarHazardTrigger.cs
  git commit -m "feat: add CarHazardTrigger with knockback and time penalty"
  ```

---

### Task 5: Antarmuka Pengguna & HUD Lengkap (`DeliveryHUD.cs`)

**Files:**
- Create: `Assets/Scripts/DeliveryHUD.cs`

**Interfaces:**
- Consumes:
  - `DeliveryGameManager.Instance`
  - `PlayerController`
- Produces:
  - Dashboard Catatan Pengiriman (Timer, Target, Petunjuk).
  - Indikator Tombol Interaksi Dinamis `[E]`.
  - Overlay Peta Denah Kota `[M]`.
  - Layar Modal Ending (Ending 1 / 2 / 3, Epilog Narasi, Statistik, Tombol Restart).

- [ ] **Step 1: Buat kelas `DeliveryHUD.cs` berbasis programatis (Code-Driven UI)**
  - Mengikuti pola `NPCDialogueOverlay.cs`: Canvas ScreenSpaceOverlay, Font Roboto-Bold, dynamicPixelsPerUnit 2.0f.
  - Membangun UI secara otomatis jika belum ada di scene.
  - Komponen Dashboard Kiri Atas:
    - Kotak latar panel gelap elegan bergaris tepi kontras.
    - Text Timer: `⏱ 10:00`.
    - Text Paket: `📦 Paket: 0 / 5`.
    - Text Misi & Catatan: Nama target dan petunjuk alamat.
  - Komponen Prompt Interaksi Tengah Bawah:
    - Box banner dinamis: `[E] Bicara dengan Pak Beni`, aktif hanya saat dalam jangkauan interaksi.

- [ ] **Step 2: Implementasikan Panel Peta Kota ([M])**
  - Panel peta denah visual yang menampilkan tata letak kota (Area Parkir, Pertokoan, Coffee Shop, Drug Store, Hardware Store, Apartemen).
  - Dapat dibuka/tutup dengan tombol **[M]** atau tombol **[Escape]**.

- [ ] **Step 3: Implementasikan Layar Modal Ending (Ending 1 / 2 / 3)**
  - Teks Judul Besar (Ending 1: Kurir Terpercaya / Ending 2: Pengiriman Terlambat / Ending 3: Gagal Bertugas).
  - Teks Narasi Epilog mendalam sesuai dokumen cerita.
  - Statistik Misi: Waktu Tersisa, Jumlah Paket Terkirim (X/5), Jumlah Kesalahan Alamat.
  - Tombol interaktif `[Main Lagi]` yang memuat ulang scene saat diklik.

- [ ] **Step 4: Uji Coba Kompilasi**
  - Verifikasi integritas script dan commit.

- [ ] **Step 5: Commit Task 5**
  ```bash
  git add Assets/Scripts/DeliveryHUD.cs
  git commit -m "feat: add DeliveryHUD with timer, objective card, map overlay, and ending modal"
  ```

---

### Task 6: Integrasi Scene `Module 2.unity` & Bootstrap Otomatis (`PlayerBootstrap.cs`)

**Files:**
- Modify: `Assets/Scripts/PlayerBootstrap.cs`
- Modify: `Assets/Module 2.unity`

**Interfaces:**
- Consumes:
  - Seluruh modul Tasks 1-5.
- Produces:
  - Scene `Module 2.unity` yang bersih dari tugas lama dan langsung menjalankan game "Kurir Terakhir" begitu tombol Play (▶) ditekan.

- [ ] **Step 1: Perbarui `PlayerBootstrap.cs` untuk Pembersihan & Setup Kurir Terakhir**
  - Bersihkan / nonaktifkan objek tugas lama:
    - `Quizmaster_Steve` dan `ExitPortal` (Modul 7).
    - `CutsceneTriggerZone_DonutShop`, `DollyTrack_DonutShop`, `CM_vcam_Dolly`, dll (Modul 8).
    - SubScene platform lama (Modul 6).
  - Tempatkan dan konfigurasikan 6 Karakter Utama dengan `DeliveryRecipient`:
    1. **Pak Beni:** Area Parkir (304.0, 0.02, -12.0).
    2. **Clara:** Depan Coffee Shop (316.0, 0.02, 14.0).
    3. **Pak Joko:** Depan Drug Store (320.0, 0.02, 5.0).
    4. **Dimas:** Depan Hardware Store (308.0, 0.02, 7.0).
    5. **Petugas Parkir:** Trotoar Area Parkir (298.0, 0.02, -5.0).
    6. **Naya:** Pintu masuk Gedung Apartemen (288.0, 0.02, 18.0).
  - Siapkan kardus paket fisik `DroppedPackageItem` di koordinat sela mobil parkir (294.5, 0.05, -8.0).
  - Posisikan Rian (Player) di Area Parkir menghadap ke Pak Beni (302.0, 0.02, -12.0).
  - Pasang `CarHazardTrigger` pada mobil bergerak di kota.
  - Inisialisasi `DeliveryGameManager` dan `DeliveryHUD`.

- [ ] **Step 2: Buat MenuItem di Unity Editor**
  - Tambahkan menu: `[MenuItem("Tools/⭐ Setup Game Kurir Terakhir (Final Project)", false, 1)]` agar setup dapat dijalankan langsung dengan satu klik kapan saja di Unity Editor.

- [ ] **Step 3: Uji Coba Kompilasi C# & Batch Setup**
  - Jalankan script verifikasi kompilasi untuk memastikan seluruh skrip di project berhasil build tanpa error.

- [ ] **Step 4: Commit Task 6**
  ```bash
  git add Assets/Scripts/PlayerBootstrap.cs Assets/Module 2.unity
  git commit -m "feat: configure Kurir Terakhir complete setup in PlayerBootstrap and clean legacy objects"
  ```

---

### Task 7: Verifikasi End-to-End & Pengujian Skenario Game

**Files:**
- Test verification & scene execution

- [ ] **Step 1: Verifikasi Alur Pengiriman Penuh (Misi 1 s/d Final)**
  - Start -> Bicara ke Pak Beni -> Clara -> Pak Joko -> Dimas -> Ambil Paket di Parkiran -> Naya -> Lapor ke Pak Beni -> Ending 1 muncul dengan benar.

- [ ] **Step 2: Verifikasi Penalti Waktu & Tabrakan Mobil**
  - Uji serahkan paket ke NPC yang salah: dialog penolakan muncul, timer berkurang 20 detik.
  - Sengaja terserempet mobil: knockback bekerja, timer berkurang 5 detik.

- [ ] **Step 3: Verifikasi Ending 2 & Ending 3**
  - Simulasikan waktu habis saat 3 paket terkirim: Ending 2 muncul.
  - Simulasikan waktu habis saat < 3 paket terkirim: Ending 3 muncul.

- [ ] **Step 4: Final Documentation & Commit**
  - Perbarui `README.md` dengan panduan kontrol dan alur game Kurir Terakhir.
  ```bash
  git add README.md
  git commit -m "docs: update README with Kurir Terakhir game guide and features"
  ```
