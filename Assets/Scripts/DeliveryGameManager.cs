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
    [Tooltip("Total batas waktu permainan dalam detik (10 menit = 600 detik).")]
    public float totalTimeLimit = 600f;

    [Tooltip("Penalti waktu saat salah menyerahkan paket ke NPC (detik).")]
    public float wrongDeliveryPenalty = 20f;

    [Tooltip("Penalti waktu saat terserempet mobil (detik).")]
    public float carHitPenalty = 5f;

    [Header("=== Runtime Status ===")]
    [SerializeField] private float timeRemaining = 600f;
    [SerializeField] private int packagesDelivered = 0;
    [SerializeField] private int totalPackages = 5;
    [SerializeField] private int wrongDeliveryCount = 0;
    [SerializeField] private bool isGameStarted = false;
    [SerializeField] private bool isTimerRunning = false;
    [SerializeField] private bool isDroppedPackageRevealed = false;
    [SerializeField] private bool isDroppedPackagePickedUp = false;
    [SerializeField] private DeliveryMissionState currentState = DeliveryMissionState.Prolog;
    [SerializeField] private GameEnding finalEnding = GameEnding.None;

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

        timeRemaining = totalTimeLimit;
        isGameStarted = false;
        isTimerRunning = false;
    }

    private void Start()
    {
        // Jangan jalankan timer otomatis sebelum pemain menekan 'Mulai Permainan' di Main Menu
        if (!isGameStarted)
        {
            isTimerRunning = false;
        }
    }

    public void StartGame()
    {
        isGameStarted = true;
        timeRemaining = totalTimeLimit;
        packagesDelivered = 0;
        wrongDeliveryCount = 0;
        isDroppedPackageRevealed = false;
        currentState = DeliveryMissionState.Prolog;
        finalEnding = GameEnding.None;
        isTimerRunning = true;

        // Posisikan kembali Rian (Player) ke depan Mall & aktifkan kontrol
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = new Vector3(310.18f, 0.02f, 4.34f);
            player.transform.rotation = Quaternion.identity;
            player.SetMovementFrozen(false);
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

    private void Update()
    {
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
                    string[] droppedClues = isDroppedPackageRevealed
                        ? new string[]
                        {
                            "Kotak paket kardusmu ada di jalan aspal dekat pembatas beton sebelah pos Fire Station.",
                            "Cepat kembali ke sana dan ambil dengan tombol [E] sebelum terlindas kendaraan!"
                        }
                        : (recipient.successDialogueLines != null && recipient.successDialogueLines.Length > 0
                            ? recipient.successDialogueLines
                            : recipient.introDialogueLines);

                    NPCDialogueOverlay.ShowSequentialDialogue(recipient.transform, "Petugas Parkir", droppedClues, () =>
                    {
                        if (!isDroppedPackageRevealed)
                        {
                            RevealDroppedPackage();
                        }
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
                p.gameObject.SetActive(false);
                Renderer[] rList = p.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rList) r.enabled = false;
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
            }
        }

        string[] pickupMsg = new string[]
        {
            "Kotak paket kardus berhasil dipungut dari jalan!",
            "Label alamat: Naya - Penghuni Apartemen bertingkat tinggi di ujung jalan.",
            "Cepat antarkan ke Naya sebelum toko-toko menutup pintunya!"
        };

        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        Transform targetTr = player != null ? player.transform : transform;

        NPCDialogueOverlay.ShowSequentialDialogue(targetTr, "Rian (Kurir)", pickupMsg, () =>
        {
            packagesDelivered = Mathf.Max(packagesDelivered, 4);
            OnPackageProgressChanged?.Invoke(packagesDelivered, totalPackages);
            AdvanceMission(DeliveryMissionState.Package5_Apartment);
        });
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

        DroppedPackageItem pkg = Object.FindAnyObjectByType<DroppedPackageItem>(FindObjectsInactive.Include);
        if (pkg != null)
        {
            pkg.gameObject.SetActive(true);
        }
        else
        {
            GameObject go = GameObject.Find("CardboardBox_02");
            if (go == null) go = GameObject.Find("DroppedPackage_Mission4");
            if (go != null) go.SetActive(true);
        }

        OnMissionChanged?.Invoke(currentState);
    }

    /// <summary>
    /// Mengembalikan pemain ke Main Menu dan mereset status permainan.
    /// </summary>
    public void GoToMainMenu()
    {
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

        // Posisikan kembali Rian (Player) ke depan Mall & bekukan kontrol
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = new Vector3(310.18f, 0.02f, 4.34f);
            player.transform.rotation = Quaternion.identity;
            player.SetMovementFrozen(true);
        }

        // Sembunyikan paket kardus yang terjatuh
        DroppedPackageItem dropItem = Object.FindAnyObjectByType<DroppedPackageItem>(FindObjectsInactive.Include);
        if (dropItem != null)
        {
            dropItem.gameObject.SetActive(false);
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

        // 3. Posisikan kembali Rian (Player) ke titik awal di depan Mall & unfreeze kontrol
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = new Vector3(310.18f, 0.02f, 4.34f);
            player.transform.rotation = Quaternion.identity;
            player.SetMovementFrozen(false);
        }

        // 4. Sembunyikan kembali paket kardus yang terjatuh (baru aktif saat Misi 4 Petugas Parkir)
        DroppedPackageItem dropItem = Object.FindAnyObjectByType<DroppedPackageItem>(FindObjectsInactive.Include);
        if (dropItem != null)
        {
            dropItem.gameObject.SetActive(false);
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

