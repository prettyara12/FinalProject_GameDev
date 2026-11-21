using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DeliveryMissionState
{
    Prolog,                 // Menerima 5 paket & instruksi dari Pak Beni di Area Parkir
    Package1_CoffeeShop,    // Mengantar paket 1 ke Clara di Coffee Shop
    Package2_DrugStore,     // Mengantar paket 2 ke Pak Joko di Drug Store
    Package3_HardwareStore, // Mengantar paket 3 ke Dimas di Hardware Store
    Package4_DroppedPackage,// Mencari & memungut paket yang jatuh di jalan
    Package5_Apartment,     // Mengantar paket 5 ke Naya di Apartemen
    Final_ReturnToHub,      // Kembali ke Pak Beni di Area Parkir
    Completed,              // Selesai dengan Ending 1 (Kurir Terpercaya)
    Failed                  // Selesai dengan Ending 2 atau Ending 3
}

public enum GameEnding
{
    None,
    Ending1_KurirTerpercaya,    // 5/5 paket terkirim tepat waktu
    Ending2_PengirimanTerlambat,// Waktu habis, >= 3 paket terkirim
    Ending3_GagalBertugas       // Waktu habis, < 3 paket terkirim
}

/// <summary>
/// DeliveryGameManager: State machine inti dan manajer aturan game Kurir Terakhir.
/// Mengelola countdown timer 10 menit, penalti waktu, tahapan pengiriman, dan 3 variasi ending.
/// </summary>
public class DeliveryGameManager : MonoBehaviour
{
    private static DeliveryGameManager instance;
    public static DeliveryGameManager Instance => instance;

    [Header("=== Game Configuration ===")]
    [Tooltip("Total batas waktu permainan dalam detik (6 menit = 360 detik).")]
    public float totalTimeLimit = 360f;

    [Tooltip("Penalti waktu saat salah menyerahkan paket ke NPC (detik).")]
    public float wrongDeliveryPenalty = 20f;

    [Tooltip("Penalti waktu saat terserempet mobil (detik).")]
    public float carHitPenalty = 5f;

    [Header("=== Runtime Status ===")]
    [SerializeField] private float timeRemaining = 360f;
    [SerializeField] private int packagesDelivered = 0;
    [SerializeField] private int totalPackages = 5;
    [SerializeField] private int wrongDeliveryCount = 0;
    [SerializeField] private bool isGameStarted = false;
    [SerializeField] private bool isTimerRunning = false;
    [SerializeField] private bool isDroppedPackageRevealed = false;
    [SerializeField] private bool isDroppedPackagePickedUp = false;
    [SerializeField] private DeliveryMissionState currentState = DeliveryMissionState.Prolog;
    [SerializeField] private GameEnding finalEnding = GameEnding.None;

    private Vector3 initialScenePlayerPosition;
    private Quaternion initialScenePlayerRotation;
    private bool hasCapturedScenePlayerTransform = false;

    public float TimeRemaining => timeRemaining;
    public int PackagesDelivered => packagesDelivered;
    public int TotalPackages => totalPackages;
    public int WrongDeliveryCount => wrongDeliveryCount;
    public bool IsGameStarted => isGameStarted;
    public bool IsTimerRunning => isTimerRunning;
    public bool IsDroppedPackageRevealed => isDroppedPackageRevealed;
    public bool IsDroppedPackagePickedUp => isDroppedPackagePickedUp;
    public DeliveryMissionState CurrentState => currentState;
    public GameEnding FinalEnding => finalEnding;

    // Events untuk HUD & UI
    public event System.Action<float> OnTimerTick;
    public event System.Action<DeliveryMissionState> OnMissionChanged;
    public event System.Action<int, int> OnPackageProgressChanged;
    public event System.Action<GameEnding> OnGameEnded;
    public event System.Action<float, string> OnPenaltyApplied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        totalTimeLimit = 360f; // 6 menit di seluruh level
        timeRemaining = totalTimeLimit;
        isGameStarted = false;
        isTimerRunning = false;

        CaptureScenePlayerTransform();
    }

    private void Start()
    {
        CaptureScenePlayerTransform();

        // Jangan jalankan timer otomatis sebelum pemain menekan 'Mulai Permainan' di Main Menu
        if (!isGameStarted)
        {
            isTimerRunning = false;
        }
    }

    public void CaptureScenePlayerTransform()
    {
        if (hasCapturedScenePlayerTransform) return;
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            initialScenePlayerPosition = player.transform.position;
            initialScenePlayerRotation = player.transform.rotation;
            hasCapturedScenePlayerTransform = true;
            Debug.Log($"[DeliveryGameManager] Posisi & rotasi Rian di Scene berhasil disimpan: {initialScenePlayerPosition}, rotasi: {initialScenePlayerRotation.eulerAngles}");
        }
    }

    public void StartGame()
    {
        totalTimeLimit = 360f; // 6 menit di seluruh level
        isGameStarted = true;
        timeRemaining = totalTimeLimit;
        packagesDelivered = 0;
        wrongDeliveryCount = 0;
        isDroppedPackageRevealed = false;
        currentState = DeliveryMissionState.Prolog;
        finalEnding = GameEnding.None;
        isTimerRunning = true;

        // Posisikan Rian (Player) sesuai posisi di Scene & aktifkan kontrol
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        if (player != null)
        {
            if (hasCapturedScenePlayerTransform)
            {
                player.transform.position = initialScenePlayerPosition;
                player.transform.rotation = initialScenePlayerRotation;
            }
            player.SetMovementFrozen(false);

            Camera cam = Camera.main;
            if (cam != null)
            {
                ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
                if (tpc != null)
                {
                    tpc.target = player.transform;
                    tpc.enabled = true;
                    tpc.SnapToTarget();
                }
            }
        }

        if (isAirportLevel2)
        {
            StartCoroutine(AutoTriggerRianAskPakBeni2Coroutine());
        }

        // Sembunyikan Main Menu jika terbuka
        if (DeliveryHUD.Instance != null)
        {
            DeliveryHUD.Instance.HideMainMenu();
        }

        OnTimerTick?.Invoke(timeRemaining);
        OnMissionChanged?.Invoke(currentState);
        OnPackageProgressChanged?.Invoke(packagesDelivered, totalPackages);
    }

    private System.Collections.IEnumerator AutoTriggerRianAskPakBeni2Coroutine()
    {
        yield return new WaitForSeconds(0.4f);
        if (currentState == DeliveryMissionState.Prolog)
        {
            DeliveryRecipient[] allRecipients = Object.FindObjectsByType<DeliveryRecipient>();
            DeliveryRecipient pakBeni = null;
            foreach (var r in allRecipients)
            {
                if (r != null && r.recipientType == DeliveryRecipient.RecipientType.PakBeni)
                {
                    pakBeni = r;
                    break;
                }
            }
            if (pakBeni != null)
            {
                InteractWithRecipientLevel2(pakBeni);
            }
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        // Debug Shortcut F9: Langsung selesaikan seluruh pengantaran & picu Ending 1 (Kurir Terpercaya)
        if (Input.GetKeyDown(KeyCode.F9) && finalEnding == GameEnding.None)
        {
            DebugTriggerInstantWin();
            return;
        }
#endif

        if (!isTimerRunning || finalEnding != GameEnding.None) return;

        // Timer terus berjalan kecuali saat permainan telah selesai
        timeRemaining -= Time.deltaTime;
        if (timeRemaining < 0f) timeRemaining = 0f;

        OnTimerTick?.Invoke(timeRemaining);

        if (timeRemaining <= 0f)
        {
            HandleTimeExpired();
        }
    }

    /// <summary>
    /// Shortcut pengujian / demonstrasi dosen: Memicu instant win Kurir Terpercaya.
    /// Memungkinkan penguji langsung melihat layar kemenangan dan menguji transisi ke Level 2 Bandara.
    /// </summary>
    public void DebugTriggerInstantWin()
    {
        packagesDelivered = totalPackages;
        OnPackageProgressChanged?.Invoke(packagesDelivered, totalPackages);
        TriggerEnding(GameEnding.Ending1_KurirTerpercaya);
        Debug.Log("[DEBUG] Instant win Kurir Terpercaya berhasil dipicu (Level 1 selesai)! Layar kemenangan aktif.");
    }

    public void ApplyWrongDeliveryPenalty()
    {
        if (finalEnding != GameEnding.None) return;

        timeRemaining = Mathf.Max(0f, timeRemaining - wrongDeliveryPenalty);
        wrongDeliveryCount++;

        GameAudioManager.Instance?.PlayWrongDelivery();

        OnPenaltyApplied?.Invoke(wrongDeliveryPenalty, "Salah Alamat! -20s");
        OnTimerTick?.Invoke(timeRemaining);

        if (timeRemaining <= 0f)
        {
            HandleTimeExpired();
        }
    }

    public void ApplyCarHitPenalty()
    {
        if (finalEnding != GameEnding.None) return;

        timeRemaining = Mathf.Max(0f, timeRemaining - carHitPenalty);

        GameAudioManager.Instance?.PlayTrafficPenalty();

        OnPenaltyApplied?.Invoke(carHitPenalty, "Tertabrak Kendaraan! -5s");
        OnTimerTick?.Invoke(timeRemaining);

        if (timeRemaining <= 0f)
        {
            HandleTimeExpired();
        }
    }

    /// <summary>
    /// Dipanggil saat Rian menekan tombol [E] pada NPC penerima.
    /// </summary>
    public void InteractWithRecipient(DeliveryRecipient recipient)
    {
        if (recipient == null || finalEnding != GameEnding.None) return;

        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        if (isAirportLevel2)
        {
            InteractWithRecipientLevel2(recipient);
            return;
        }

        // 1. Prolog: Bicara ke Pak Beni
        if (currentState == DeliveryMissionState.Prolog)
        {
            if (recipient.recipientType == DeliveryRecipient.RecipientType.PakBeni)
            {
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, "Pak Beni", recipient.introDialogueLines, () =>
                {
                    AdvanceMission(DeliveryMissionState.Package1_CoffeeShop);
                });
            }
            else
            {
                // Bicara ke orang lain sebelum ambil paket
                string[] lines = new string[] { "Temui Pak Beni di dekat mobil pengiriman area parkir untuk mengambil paket tugasmu!" };
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, lines);
            }
            return;
        }

        // 2. Petugas Parkir: Hanya aktif memberikan petunjuk saat Misi 4 (setelah Rian bicara dengan Dimas)
        if (recipient.recipientType == DeliveryRecipient.RecipientType.PetugasParkir)
        {
            if (currentState < DeliveryMissionState.Package4_DroppedPackage)
            {
                // Rian belum menyelesaikan pengantaran ke Dimas
                string[] advice = new string[]
                {
                    "Halo Rian! Selesaikan dulu tugas pengantaran paketmu ke pelanggan di kota sebelum bertanya hal lain.",
                    "Fokus antarkan paket ke tujuan yang sesuai dengan label alamat di HUD!"
                };
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, "Petugas Parkir", advice);
            }
            else if (currentState == DeliveryMissionState.Package4_DroppedPackage)
            {
                if (isDroppedPackagePickedUp)
                {
                    string[] alreadyPicked = new string[]
                    {
                        "Bagus Rian, kotak paket kardusmu sudah berhasil kamu ambil dari jalan aspal!",
                        "Sekarang lekas antarkan paket tersebut ke Naya di gedung apartemen."
                    };
                    NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, "Petugas Parkir", alreadyPicked);
                }
                else
                {
                    bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2";
                    string[] droppedClues = isDroppedPackageRevealed
                        ? (isLevel2
                            ? new string[]
                            {
                                "Kotak paket kardusmu ada di jalan aspal dekat trotoar toko donat sebelah utara.",
                                "Cepat lari ke sana dan tekan [E] untuk memungutnya!"
                            }
                            : new string[]
                            {
                                "Kotak paket kardusmu ada di jalan aspal dekat pembatas beton sebelah pos Fire Station.",
                                "Cepat kembali ke sana dan ambil dengan tombol [E] sebelum terlindas kendaraan!"
                            })
                        : (recipient.successDialogueLines != null && recipient.successDialogueLines.Length > 0
                            ? recipient.successDialogueLines
                            : recipient.introDialogueLines);

                    NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, "Petugas Parkir", droppedClues, () =>
                    {
                        RevealDroppedPackage();
                    });
                }
            }
            else
            {
                string[] doneAdvice = new string[]
                {
                    "Bagus Rian, paket yang terjatuh sudah berhasil kamu selamatkan!",
                    "Lanjutkan pengantaran paket terakhirmu ke Naya di apartemen sebelum waktu habis."
                };
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, "Petugas Parkir", doneAdvice);
            }
            return;
        }

        // 3. Misi 4: Interaksi khusus saat paket masih hilang di jalan
        if (currentState == DeliveryMissionState.Package4_DroppedPackage)
        {
            if (recipient.recipientType == DeliveryRecipient.RecipientType.PakBeni)
            {
                string[] lines = new string[] { "Rian, cepat ambil kotak paket kardus yang terjatuh di jalan aspal dekat pembatas beton sebelum terlindas!" };
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, "Pak Beni", lines);
            }
            else
            {
                string[] lines = recipient.wrongDeliveryLines != null && recipient.wrongDeliveryLines.Length > 0
                    ? recipient.wrongDeliveryLines
                    : new string[] { "Maaf mas, saya dengar ada kotak paket kardusmu yang terjatuh di jalan aspal dekat pembatas beton. Coba periksa di sana!" };
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, lines, () =>
                {
                    ApplyWrongDeliveryPenalty();
                });
            }
            return;
        }

        // 4. Final: Lapor kembali ke Pak Beni setelah Paket 5 selesai
        if (currentState == DeliveryMissionState.Final_ReturnToHub)
        {
            if (recipient.recipientType == DeliveryRecipient.RecipientType.PakBeni)
            {
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, "Pak Beni", recipient.successDialogueLines, () =>
                {
                    TriggerEnding(GameEnding.Ending1_KurirTerpercaya);
                });
            }
            else
            {
                string[] lines = new string[] { "Semua paket sudah terkirim! Segera kembali ke Pak Beni di Area Parkir untuk menyelesaikan tugas!" };
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, lines);
            }
            return;
        }

        // 4. Misi Pengiriman Standar (Misi 1, 2, 3, 5)
        bool isCorrectRecipient = (recipient.targetForMission == currentState);

        if (isCorrectRecipient)
        {
            // Penerima Benar!
            GameAudioManager.Instance?.PlayDeliverySuccess();
            NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, recipient.successDialogueLines, () =>
            {
                packagesDelivered++;
                OnPackageProgressChanged?.Invoke(packagesDelivered, totalPackages);

                // Tentukan langkah misi selanjutnya
                switch (currentState)
                {
                    case DeliveryMissionState.Package1_CoffeeShop:
                        AdvanceMission(DeliveryMissionState.Package2_DrugStore);
                        break;
                    case DeliveryMissionState.Package2_DrugStore:
                        AdvanceMission(DeliveryMissionState.Package3_HardwareStore);
                        break;
                    case DeliveryMissionState.Package3_HardwareStore:
                        AdvanceMission(DeliveryMissionState.Package4_DroppedPackage);
                        break;
                    case DeliveryMissionState.Package5_Apartment:
                        AdvanceMission(DeliveryMissionState.Final_ReturnToHub);
                        break;
                }
            });
        }
        else
        {
            // Penerima Salah! Terapkan Penalti
            string[] wrongLines = recipient.wrongDeliveryLines != null && recipient.wrongDeliveryLines.Length > 0
                ? recipient.wrongDeliveryLines
                : new string[] { "Maaf mas, saya bukan penerima paket ini. Coba periksa alamat dan papan nama toko!" };

            NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, wrongLines, () =>
            {
                ApplyWrongDeliveryPenalty();
            });
        }
    }

    /// <summary>
    /// Alur Pengantaran Paket Level 2 Bandara:
    /// Pak Beni 2 -> Naya 2 -> Dimas 2 -> Tukang Parkir 2 -> Pak Joko 2 -> Clara 2 -> Pak Beni 2 -> Game Berakhir (Tamat)
    /// </summary>
    private void InteractWithRecipientLevel2(DeliveryRecipient recipient)
    {
        // 1. Prolog: Bicara ke Pak Beni 2
        if (currentState == DeliveryMissionState.Prolog)
        {
            if (recipient.recipientType == DeliveryRecipient.RecipientType.PakBeni)
            {
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, recipient.introDialogueLines, () =>
                {
                    AdvanceMission(DeliveryMissionState.Package1_CoffeeShop);
                });
            }
            else
            {
                string[] lines = new string[]
                {
                    "Rian: Permisi, apa Anda tahu tugas paket saya di bandara ini?",
                    recipient.characterName + ": Temui Pak Beni 2 terlebih dahulu di dekat mobil pengiriman untuk menerima instruksi paket!"
                };
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, lines);
            }
            return;
        }

        // 2. Final: Lapor kembali ke Pak Beni 2 setelah Paket 5 selesai
        if (currentState == DeliveryMissionState.Final_ReturnToHub)
        {
            if (recipient.recipientType == DeliveryRecipient.RecipientType.PakBeni)
            {
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, recipient.successDialogueLines, () =>
                {
                    TriggerEnding(GameEnding.Ending1_KurirTerpercaya);
                });
            }
            else
            {
                string[] lines = new string[]
                {
                    "Rian: Saya sudah mengantarkan seluruh 5 paket bandara.",
                    recipient.characterName + ": Hebat sekali Mas Rian! Segera kembali ke Pak Beni 2 untuk menyelesaikan tugasmu!"
                };
                NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, lines);
            }
            return;
        }

        // 3. Pengantaran 5 Paket Langsung ke Penerima (Misi 1 s/d 5)
        bool isCorrectRecipient = (recipient.targetForMission == currentState);

        if (isCorrectRecipient)
        {
            GameAudioManager.Instance?.PlayDeliverySuccess();
            NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, recipient.successDialogueLines, () =>
            {
                packagesDelivered++;
                OnPackageProgressChanged?.Invoke(packagesDelivered, totalPackages);

                switch (currentState)
                {
                    case DeliveryMissionState.Package1_CoffeeShop:
                        AdvanceMission(DeliveryMissionState.Package2_DrugStore);
                        break;
                    case DeliveryMissionState.Package2_DrugStore:
                        AdvanceMission(DeliveryMissionState.Package3_HardwareStore);
                        break;
                    case DeliveryMissionState.Package3_HardwareStore:
                        AdvanceMission(DeliveryMissionState.Package4_DroppedPackage);
                        break;
                    case DeliveryMissionState.Package4_DroppedPackage:
                        AdvanceMission(DeliveryMissionState.Package5_Apartment);
                        break;
                    case DeliveryMissionState.Package5_Apartment:
                        AdvanceMission(DeliveryMissionState.Final_ReturnToHub);
                        break;
                }
            });
        }
        else
        {
            string[] wrongLines = recipient.wrongDeliveryLines != null && recipient.wrongDeliveryLines.Length > 0
                ? recipient.wrongDeliveryLines
                : new string[] { "Maaf mas, ini bukan paket pesanan saya. Coba periksa petunjuk alamat di layarmu!" };

            NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, recipient.characterName, wrongLines, () =>
            {
                ApplyWrongDeliveryPenalty();
            });
        }
    }

    /// <summary>
    /// Mengambil paket yang terjatuh (Misi 4).
    /// </summary>
    public void PickUpDroppedPackage()
    {
        if (finalEnding != GameEnding.None) return;

        isDroppedPackagePickedUp = true;
        GameAudioManager.Instance?.PlayPackagePickup();

        // Segera hilangkan dan sembunyikan semua objek paket di scene
        DroppedPackageItem[] pkgs = Object.FindObjectsByType<DroppedPackageItem>(FindObjectsInactive.Include);
        foreach (var p in pkgs)
        {
            if (p != null)
            {
                p.SetPackageVisible(false);
            }
        }
        GameObject[] allBoxes = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var b in allBoxes)
        {
            if (b != null && b.name.Contains("CardboardBox_02"))
            {
                b.SetActive(false);
                Renderer[] rList = b.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rList) r.enabled = false;
                Collider[] cList = b.GetComponentsInChildren<Collider>(true);
                foreach (var c in cList) c.enabled = false;
            }
        }

        // Perbarui progress paket dan lanjutkan misi ke Naya seketika saat paket diambil!
        packagesDelivered = Mathf.Max(packagesDelivered, 4);
        OnPackageProgressChanged?.Invoke(packagesDelivered, totalPackages);
        AdvanceMission(DeliveryMissionState.Package5_Apartment);

        bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2";
        string[] pickupMsg = isLevel2
            ? new string[]
            {
                "Kotak paket kardus berhasil diambil dari jalan dekat toko donat!",
                "Paket sekarang sudah berada bersamamu. Alamat tujuan: Naya di Hunian Deretan Timur.",
                "Cepat antarkan ke Naya sebelum batas waktu 6 menit habis!"
            }
            : new string[]
            {
                "Kotak paket kardus berhasil diambil dari jalan aspal!",
                "Paket sekarang sudah berada bersamamu. Alamat tujuan: Naya di Gedung Apartemen.",
                "Cepat antarkan ke Naya sebelum batas waktu 6 menit habis!"
            };

        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        Transform targetTr = player != null ? player.transform : transform;

        NPCDialogueOverlay.ShowSequentialDialogue(targetTr, "Rian (Kurir)", pickupMsg);
    }

    private void AdvanceMission(DeliveryMissionState nextState)
    {
        currentState = nextState;
        OnMissionChanged?.Invoke(currentState);
    }

    /// <summary>
    /// Mengaktifkan paket kardus di jalan setelah Rian mendapatkan info lokasi dari Petugas Parkir.
    /// </summary>
    public void RevealDroppedPackage()
    {
        if (isDroppedPackagePickedUp) return;

        isDroppedPackageRevealed = true;

        DroppedPackageItem[] pkgs = Object.FindObjectsByType<DroppedPackageItem>(FindObjectsInactive.Include);
        foreach (var pkg in pkgs)
        {
            if (pkg != null)
            {
                pkg.SetPackageVisible(true);
            }
        }

        GameObject[] allBoxes = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var b in allBoxes)
        {
            if (b != null && b.name.Contains("CardboardBox_02"))
            {
                b.SetActive(true);
                foreach (var r in b.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
                foreach (var c in b.GetComponentsInChildren<Collider>(true)) c.enabled = true;
            }
        }

        OnMissionChanged?.Invoke(currentState);
    }

    /// <summary>
    /// Mengembalikan pemain ke Main Menu dan mereset status permainan.
    /// </summary>
    public void GoToMainMenu()
    {
        bool isAirport = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2" || isAirport)
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Module 2");
            return;
        }

        Time.timeScale = 1f;
        isGameStarted = false;
        isTimerRunning = false;
        finalEnding = GameEnding.None;
        packagesDelivered = 0;
        wrongDeliveryCount = 0;
        timeRemaining = totalTimeLimit;
        isDroppedPackageRevealed = false;
        isDroppedPackagePickedUp = false;
        currentState = DeliveryMissionState.Prolog;

        // Tutup dialog aktif & sembunyikan modal ending
        NPCDialogueOverlay.EndSequentialDialogue();

        // Tampilkan Main Menu di HUD
        if (DeliveryHUD.Instance != null)
        {
            DeliveryHUD.Instance.HideEndingModal();
            DeliveryHUD.Instance.ShowMainMenu();
        }

        // Posisikan kembali Rian (Player) ke posisi awal scene & bekukan kontrol
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            if (hasCapturedScenePlayerTransform)
            {
                player.transform.position = initialScenePlayerPosition;
                player.transform.rotation = initialScenePlayerRotation;
            }
            player.SetMovementFrozen(true);
        }

        // Sembunyikan paket kardus yang terjatuh
        DroppedPackageItem[] allDropItems = Object.FindObjectsByType<DroppedPackageItem>(FindObjectsInactive.Include);
        foreach (var dropItem in allDropItems)
        {
            if (dropItem != null) dropItem.SetPackageVisible(false);
        }

        Debug.Log("[DeliveryGameManager] Kembali ke Main Menu berhasil!");
    }

    /// <summary>
    /// Merestart game secara in-place di scene yang sama tanpa berpindah scene,
    /// sehingga permainan terulang terus menerus secara instan dan mulus.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;

        totalTimeLimit = 360f; // 6 menit (Scene 1 & Scene 2)

        // 1. Reset status misi, paket, penalti, dan timer
        finalEnding = GameEnding.None;
        packagesDelivered = 0;
        wrongDeliveryCount = 0;
        timeRemaining = totalTimeLimit;
        isDroppedPackageRevealed = false;
        isDroppedPackagePickedUp = false;
        currentState = DeliveryMissionState.Prolog;
        isTimerRunning = true;
        isGameStarted = true;

        // 2. Tutup dialog aktif & sembunyikan modal ending
        NPCDialogueOverlay.EndSequentialDialogue();
        if (DeliveryHUD.Instance != null)
        {
            DeliveryHUD.Instance.HideEndingModal();
            DeliveryHUD.Instance.HideMainMenu();
        }

        // 3. Posisikan kembali Rian (Player) ke posisi awal scene & unfreeze kontrol
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            if (hasCapturedScenePlayerTransform)
            {
                player.transform.position = initialScenePlayerPosition;
                player.transform.rotation = initialScenePlayerRotation;
            }
            player.SetMovementFrozen(false);
        }

        // 4. Sembunyikan kembali paket kardus yang terjatuh (baru aktif saat Misi 4 Petugas Parkir)
        DroppedPackageItem[] resetDropItems = Object.FindObjectsByType<DroppedPackageItem>(FindObjectsInactive.Include);
        foreach (var dropItem in resetDropItems)
        {
            if (dropItem != null) dropItem.SetPackageVisible(false);
        }

        // 5. Perbarui HUD ke kondisi awal pengiriman
        OnTimerTick?.Invoke(timeRemaining);
        OnMissionChanged?.Invoke(currentState);
        OnPackageProgressChanged?.Invoke(packagesDelivered, totalPackages);

        Debug.Log("[DeliveryGameManager] Game berhasil di-restart in-place di scene saat ini!");
    }

    private void HandleTimeExpired()
    {
        if (finalEnding != GameEnding.None) return;

        isTimerRunning = false;

        // Tutup dialog aktif jika ada
        NPCDialogueOverlay.EndSequentialDialogue();

        // Evaluasi Ending
        if (packagesDelivered >= 3)
        {
            TriggerEnding(GameEnding.Ending2_PengirimanTerlambat);
        }
        else
        {
            TriggerEnding(GameEnding.Ending3_GagalBertugas);
        }
    }

    public void TriggerEnding(GameEnding ending)
    {
        isTimerRunning = false;
        finalEnding = ending;
        currentState = (ending == GameEnding.Ending1_KurirTerpercaya)
            ? DeliveryMissionState.Completed
            : DeliveryMissionState.Failed;

        // Freeze kontrol pergerakan pemain
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) player.SetMovementFrozen(true);

        OnGameEnded?.Invoke(ending);
    }
}

