using System;
using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;

/// <summary>
/// Mengelola pemutaran Cutscene Sinematik Pengantaran Paket Rian (Module 2).
/// - Durasi: ~10 detik (3 Shot: Pak Beni -> Clara Coffee Shop -> Kota Luas).
/// - Otomatis berjalan saat awal Play mode (Intro Cutscene).
/// - Menampilkan subtitle/narasi dialog sinematik di layar.
/// - Fitur Skip instan dengan tombol [Spasi] atau [Esc].
/// - Mengamankan kendali pemain & ThirdPersonCamera sebelum dan sesudah cutscene.
/// </summary>
public class RianDeliveryCutscene : MonoBehaviour
{
    private static RianDeliveryCutscene _instance;
    public static RianDeliveryCutscene Instance => _instance;

    [Header("=== Konfigurasi Timeline ===")]
    [Tooltip("PlayableDirector yang memutar Timeline Cutscene.")]
    public PlayableDirector cutsceneDirector;

    [Tooltip("Apakah cutscene otomatis berjalan saat game dimulai?")]
    public bool playOnStart = false;

    [Tooltip("Hanya putar satu kali per sesi.")]
    public bool playOnce = true;

    public bool HasPlayed => hasPlayed;
    public bool IsPlaying => isPlaying;

    [Header("=== Opsi UI & Skip ===")]
    [Tooltip("Aktifkan tombol Skip di layar / keyboard.")]
    public bool allowSkip = true;

    [Tooltip("Tampilkan subtitle narasi dialog di bagian bawah layar.")]
    public bool showSubtitles = true;

    [Header("=== Referensi Komponen (Auto-Find jika kosong) ===")]
    public CinemachineBrain cinemachineBrain;
    public ThirdPersonCamera thirdPersonCamera;
    public PlayerController playerController;

    private bool isPlaying = false;
    private bool hasPlayed = false;
    private Coroutine cutsceneRoutine;
    private int startFrame = -1;

    private string currentSpeaker = "";
    private string currentDialogue = "";

    // Narasi garis besar cerita "Kurir Terakhir" (sinkron dengan 3 shot Timeline ±30 detik):
    // Shot 1 (0–11s) Pos pengiriman Pak Beni, Shot 2 (11–20.4s) kawasan pertokoan, Shot 3 (20.4–30s) jalan kota.
    private struct SubtitleBeat
    {
        public double start;
        public double end;
        public string speaker;
        public string line;
        public SubtitleBeat(double start, double end, string speaker, string line)
        {
            this.start = start; this.end = end; this.speaker = speaker; this.line = line;
        }
    }

    private static readonly SubtitleBeat[] storyBeats =
    {
        // Shot 1: Pos pengiriman — Pak Beni menyerahkan lima paket terakhir
        new SubtitleBeat(0.0, 3.6, "Narator",
            "Sore itu, toko-toko di kawasan kota mulai bersiap tutup. Rian, kurir muda, baru tiba di pos pengiriman."),
        new SubtitleBeat(3.6, 7.4, "Pak Beni (Bagian Pengiriman)",
            "\"Rian, ini lima paket terakhir hari ini. Semuanya harus sampai sebelum jam operasional berakhir!\""),
        new SubtitleBeat(7.4, 11.0, "Pak Beni (Bagian Pengiriman)",
            "\"Hati-hati, labelnya terkena air hujan. Beberapa nama dan alamat penerima tidak terbaca jelas.\""),

        // Shot 2: Kawasan pertokoan — mencari penerima yang tepat
        new SubtitleBeat(11.0, 14.2, "Rian (Kurir)",
            "\"Kalau begitu, saya akan bertanya pada pegawai toko, petugas parkir, dan warga sekitar.\""),
        new SubtitleBeat(14.2, 17.4, "Narator",
            "Tujuan Rian tersebar di seluruh kota: coffee shop, drug store, fire station, hingga gedung apartemen."),
        new SubtitleBeat(17.4, 20.4, "Narator",
            "Setiap paket harus dicocokkan dengan nama penerima yang benar. Salah kirim hanya akan membuang waktu."),

        // Shot 3: Jalan kota — rintangan & tujuan akhir
        new SubtitleBeat(20.4, 23.6, "Narator",
            "Jalanan kota ramai kendaraan, dan satu paket bisa saja terjatuh di area parkir."),
        new SubtitleBeat(23.6, 26.8, "Pak Beni (Bagian Pengiriman)",
            "\"Antarkan kelima paket tepat waktu, dan kau resmi menjadi kurir tetap kami.\""),
        new SubtitleBeat(26.8, 30.5, "Rian (Kurir)",
            "\"Siap, Pak! Semua paket akan sampai ke orang yang tepat sebelum waktu habis!\""),
    };

    private CinemachineVirtualCameraBase[] cutsceneCameras;

    private void Awake()
    {
        _instance = this;
        playOnStart = false;
        isPlaying = false;
        hasPlayed = true;

        if (cutsceneDirector == null)
        {
            cutsceneDirector = GetComponent<PlayableDirector>();
        }
        if (cutsceneDirector != null)
        {
            cutsceneDirector.Stop();
        }

        cutsceneCameras = GetComponentsInChildren<CinemachineVirtualCameraBase>(true);
        SetCutsceneCamerasActive(false);

        enabled = false;
    }

    private void SetCutsceneCamerasActive(bool active)
    {
        if (cutsceneCameras == null) return;
        foreach (var vcam in cutsceneCameras)
        {
            if (vcam != null) vcam.gameObject.SetActive(active);
        }
    }

    private void Start()
    {
        // Cutscene telah dihapus dari game: langsung nonaktifkan
        enabled = false;
    }

    private void FindRequiredReferences()
    {
        // 1. Cari Camera dan CinemachineBrain
        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = UnityEngine.Object.FindAnyObjectByType<Camera>();

        if (mainCam != null)
        {
            if (cinemachineBrain == null)
            {
                cinemachineBrain = mainCam.GetComponent<CinemachineBrain>();
                if (cinemachineBrain == null)
                {
                    cinemachineBrain = mainCam.gameObject.AddComponent<CinemachineBrain>();
                }
            }

            if (thirdPersonCamera == null)
            {
                thirdPersonCamera = mainCam.GetComponent<ThirdPersonCamera>();
            }
        }

        // 2. Cari PlayerController
        if (playerController == null)
        {
            GameObject playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                playerController = playerGo.GetComponent<PlayerController>();
            }
        }
    }

    /// <summary>
    /// Memulai pemutaran Cutscene Sinematik (Dinonaktifkan: langsung masuk gameplay).
    /// </summary>
    public void StartCutscene()
    {
        EndCutscene();
    }

    private System.Collections.IEnumerator CutsceneWatchdog(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (isPlaying)
        {
            Debug.Log("[RianDeliveryCutscene] ⏱️ Watchdog cutscene selesai (durasi tercapai), otomatis masuk gameplay.");
            EndCutscene();
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        // Abaikan input & deteksi akhir pada frame yang sama dengan awal cutscene
        // (mis. tombol Spasi yang dipakai menekan "Mulai Permainan" tidak boleh langsung men-skip).
        if (Time.frameCount <= startFrame) return;

        // 1. Deteksi input Skip ([Spasi] atau [Esc])
        if (allowSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
        {
            SkipCutscene();
            return;
        }

        // 2. Deteksi otomatis jika timeline selesai atau berhenti
        //    (durasi harus valid; durasi 0 membuat cutscene langsung dianggap selesai)
        if (cutsceneDirector != null && cutsceneDirector.duration > 0.1)
        {
            if (cutsceneDirector.time >= cutsceneDirector.duration - 0.05 ||
                (cutsceneDirector.time > 0.5 && cutsceneDirector.state != PlayState.Playing))
            {
                EndCutscene();
                return;
            }
        }

        // 3. Sinkronisasi Teks Subtitle Dialog berdasarkan waktu pemutaran (Timeline Time)
        if (cutsceneDirector != null && showSubtitles)
        {
            double t = cutsceneDirector.time;

            currentSpeaker = "";
            currentDialogue = "";
            foreach (var beat in storyBeats)
            {
                if (t >= beat.start && t < beat.end)
                {
                    currentSpeaker = beat.speaker;
                    currentDialogue = beat.line;
                    break;
                }
            }
        }
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        EndCutscene();
    }

    /// <summary>
    /// Melewati cutscene secara instan dan langsung masuk ke gameplay.
    /// </summary>
    public void SkipCutscene()
    {
        if (!isPlaying) return;

        Debug.Log("[RianDeliveryCutscene] ⏩ Cutscene di-skip oleh pemain.");

        if (cutsceneDirector != null)
        {
            cutsceneDirector.time = cutsceneDirector.duration;
            cutsceneDirector.Stop();
        }

        EndCutscene();
    }

    /// <summary>
    /// Mengakhiri cutscene dan memulihkan kontrol pemain serta kamera normal.
    /// </summary>
    public void EndCutscene()
    {
        if (!isPlaying && hasPlayed) return;

        isPlaying = false;
        hasPlayed = true;
        currentSpeaker = "";
        currentDialogue = "";

        if (cutsceneRoutine != null)
        {
            StopCoroutine(cutsceneRoutine);
            cutsceneRoutine = null;
        }

        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= OnDirectorStopped;
            if (cutsceneDirector.state == PlayState.Playing)
            {
                cutsceneDirector.Stop();
            }
        }

        // Nonaktifkan CinemachineBrain & kamera virtual cutscene
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = false;
        }
        SetCutsceneCamerasActive(false);

        // Pulihkan kendali Player
        if (playerController != null)
        {
            playerController.SetMovementFrozen(false);
        }

        // Mulai gameplay & aktifkan timer DeliveryGameManager
        // (StartGame memindahkan Rian ke posisi awal, jadi kamera dipulihkan SETELAH ini)
        if (DeliveryGameManager.Instance != null && !DeliveryGameManager.Instance.IsGameStarted)
        {
            DeliveryGameManager.Instance.StartGame();
        }

        // Pulihkan ThirdPersonCamera agar langsung fokus ke Rian
        RestoreGameplayCamera();

        // Tampilkan dashboard dan radar HUD
        if (DeliveryHUD.Instance != null)
        {
            DeliveryHUD.Instance.OnCutsceneFinished();
        }

        // Ulangi penguncian kamera di akhir frame berikutnya, setelah Timeline/Cinemachine
        // selesai mengevaluasi frame terakhirnya, supaya kamera tidak tertinggal di shot cutscene.
        if (isActiveAndEnabled)
        {
            StartCoroutine(RestoreCameraAfterFrames());
        }

        Debug.Log("[RianDeliveryCutscene] ✅ Cutscene Selesai! Kamera dan kendali pemain telah dipulihkan.");
    }

    private void RestoreGameplayCamera()
    {
        if (cinemachineBrain != null && cinemachineBrain.enabled)
        {
            cinemachineBrain.enabled = false;
        }

        if (thirdPersonCamera == null)
        {
            FindRequiredReferences();
        }

        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.viewMode = ThirdPersonCamera.CameraViewMode.ThirdPersonFollow;
            if (thirdPersonCamera.target == null)
            {
                thirdPersonCamera.FindPlayerTarget();
            }
            if (thirdPersonCamera.target == null)
            {
                // Fallback: gunakan Rian (PlayerController) walau tag/namanya berubah atau sedang nonaktif
                PlayerController rian = playerController;
                if (rian == null)
                {
                    var all = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
                    if (all.Length > 0) rian = all[0];
                }
                if (rian != null)
                {
                    Debug.LogWarning($"[RianDeliveryCutscene] 🔍 Fallback target ke '{rian.name}': activeSelf={rian.gameObject.activeSelf}, " +
                                     $"activeInHierarchy={rian.gameObject.activeInHierarchy}, tag={rian.tag}, pos={rian.transform.position}");
                    if (!rian.gameObject.activeSelf) rian.gameObject.SetActive(true);
                    thirdPersonCamera.target = rian.transform;
                }
            }
            thirdPersonCamera.enabled = true;
            thirdPersonCamera.SnapToTarget();
        }
    }

    private System.Collections.IEnumerator RestoreCameraAfterFrames()
    {
        for (int i = 0; i < 2; i++)
        {
            yield return new WaitForEndOfFrame();
            if (isPlaying) yield break; // cutscene diputar ulang
            RestoreGameplayCamera();
        }

        if (thirdPersonCamera != null)
        {
            Transform t = thirdPersonCamera.target;
            Debug.Log($"[RianDeliveryCutscene] 🎥 Kamera gameplay: enabled={thirdPersonCamera.enabled}, mode={thirdPersonCamera.viewMode}, " +
                      $"target={(t != null ? t.name : "NULL")}, brainAktif={(cinemachineBrain != null && cinemachineBrain.enabled)}");

            if (t == null)
            {
                // Diagnosa: kenapa Rian tidak ditemukan?
                var all = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
                Debug.LogWarning($"[RianDeliveryCutscene] 🔍 Rian tidak ditemukan. playerController ref={(playerController != null ? playerController.name : "NULL")}, jumlah PlayerController di scene={all.Length}");
                foreach (var pc in all)
                {
                    Transform p = pc.transform.parent;
                    Debug.LogWarning($"[RianDeliveryCutscene] 🔍 PlayerController '{pc.name}': activeSelf={pc.gameObject.activeSelf}, activeInHierarchy={pc.gameObject.activeInHierarchy}, " +
                                     $"tag={pc.tag}, parent={(p != null ? p.name : "-")}, pos={pc.transform.position}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[RianDeliveryCutscene] ThirdPersonCamera tidak ditemukan pada Main Camera — kamera tidak bisa kembali ke Rian.");
        }
    }

    private void OnGUI()
    {
        // Cutscene dihapus dari game: tidak menampilkan UI skip atau subtitle
    }

    private void OnDestroy()
    {
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= OnDirectorStopped;
        }

        if (cutsceneRoutine != null)
        {
            StopCoroutine(cutsceneRoutine);
            cutsceneRoutine = null;
        }
    }
}

