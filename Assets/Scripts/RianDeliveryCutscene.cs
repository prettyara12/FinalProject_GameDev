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

    private string currentSpeaker = "";
    private string currentDialogue = "";

    private void Awake()
    {
        _instance = this;

        if (cutsceneDirector == null)
        {
            cutsceneDirector = GetComponent<PlayableDirector>();
        }
    }

    private void Start()
    {
        FindRequiredReferences();

        if (playOnStart && !hasPlayed)
        {
            StartCutscene();
        }
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
    /// Memulai pemutaran Cutscene Sinematik.
    /// </summary>
    public void StartCutscene()
    {
        if (isPlaying) return;
        if (playOnce && hasPlayed) return;

        FindRequiredReferences();

        isPlaying = true;

        // Bekukan kontrol pemain selama cutscene
        if (playerController != null)
        {
            playerController.SetMovementFrozen(true);
        }

        // Matikan ThirdPersonCamera sementara agar Cinemachine memegang kendali kamera utama
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = false;
        }

        // Aktifkan CinemachineBrain
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = true;
        }

        // Sembunyikan dashboard HUD & Radar selama cutscene diputar
        if (DeliveryHUD.Instance != null)
        {
            DeliveryHUD.Instance.SetDashboardVisible(false);
            DeliveryHUD.Instance.SetRadarVisible(false);
        }

        // Mulai PlayableDirector
        if (cutsceneDirector != null)
        {
            cutsceneDirector.time = 0;
            cutsceneDirector.stopped -= OnDirectorStopped;
            cutsceneDirector.stopped += OnDirectorStopped;
            cutsceneDirector.Play();
        }

        Debug.Log("[RianDeliveryCutscene] 🎬 Cutscene Rian Mengantar Paket Dimulai (Durasi: 10 Detik)!");
    }

    private void Update()
    {
        if (!isPlaying) return;

        // 1. Deteksi input Skip ([Spasi] atau [Esc])
        if (allowSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)))
        {
            SkipCutscene();
            return;
        }

        // 2. Sinkronisasi Teks Subtitle Dialog berdasarkan waktu pemutaran (Timeline Time)
        if (cutsceneDirector != null && showSubtitles)
        {
            double t = cutsceneDirector.time;

            if (t >= 0.0 && t < 3.5)
            {
                // Shot 1: Pak Beni menyerahkan paket
                currentSpeaker = "Pak Beni (Pusat Pengiriman)";
                currentDialogue = "\"Rian! Ini lima paket terakhir malam ini. Antarkan segera sebelum semua toko dan apartemen tutup!\"";
            }
            else if (t >= 3.5 && t < 7.0)
            {
                // Shot 2: Clara di Coffee Shop menerima paket
                currentSpeaker = "Clara (Coffee Shop)";
                currentDialogue = "\"Terima kasih banyak, Mas Rian! Paket biji kopinya sampai tepat waktu sebelum kedai kami tutup.\"";
            }
            else if (t >= 7.0 && t <= 10.0)
            {
                // Shot 3: Rian berlari melintasi jalan kota melanjutkan misi
                currentSpeaker = "Rian (Kurir)";
                currentDialogue = "\"Siap, Pak! Seluruh paket warga kota akan saya antarkan tepat waktu malam ini!\"";
            }
            else
            {
                currentSpeaker = "";
                currentDialogue = "";
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

        // Nonaktifkan CinemachineBrain
        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = false;
        }

        // Pulihkan ThirdPersonCamera
        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled = true;
            thirdPersonCamera.SnapToTarget();
        }

        // Pulihkan kendali Player
        if (playerController != null)
        {
            playerController.SetMovementFrozen(false);
        }

        // Mulai gameplay & aktifkan timer DeliveryGameManager
        if (DeliveryGameManager.Instance != null && !DeliveryGameManager.Instance.IsGameStarted)
        {
            DeliveryGameManager.Instance.StartGame();
        }

        // Tampilkan dashboard dan radar HUD
        if (DeliveryHUD.Instance != null)
        {
            DeliveryHUD.Instance.OnCutsceneFinished();
        }

        Debug.Log("[RianDeliveryCutscene] ✅ Cutscene Selesai! Kamera dan kendali pemain telah dipulihkan.");
    }

    private void OnGUI()
    {
        if (!isPlaying) return;

        // 1. Tampilkan Tombol / Petunjuk Skip di Kanan Atas
        if (allowSkip)
        {
            GUIStyle skipStyle = new GUIStyle(GUI.skin.box);
            skipStyle.fontSize = 13;
            skipStyle.fontStyle = FontStyle.Bold;
            skipStyle.normal.textColor = Color.yellow;
            skipStyle.alignment = TextAnchor.MiddleCenter;

            Rect skipRect = new Rect(Screen.width - 200, 18, 180, 36);
            if (GUI.Button(skipRect, "⏩ Lewati [Spasi]", skipStyle))
            {
                SkipCutscene();
                return;
            }
        }

        // 2. Tampilkan Kotak Subtitle Sinematik di Bagian Bawah Layar
        if (showSubtitles && !string.IsNullOrEmpty(currentDialogue))
        {
            float boxWidth = Mathf.Min(820f, Screen.width - 40f);
            float boxHeight = 75f;
            float boxX = (Screen.width - boxWidth) * 0.5f;
            float boxY = Screen.height - boxHeight - 28f;

            // Latar belakang transparan gelap
            GUIStyle bgStyle = new GUIStyle(GUI.skin.box);
            Texture2D blackTex = Texture2D.blackTexture;
            bgStyle.normal.background = blackTex;

            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), GUIContent.none, bgStyle);
            GUI.color = Color.white;

            // Nama Pembicara
            GUIStyle nameStyle = new GUIStyle();
            nameStyle.fontSize = 14;
            nameStyle.fontStyle = FontStyle.Bold;
            nameStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            nameStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(boxX + 16, boxY + 8, boxWidth - 32, 20), currentSpeaker, nameStyle);

            // Isi Dialog
            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = 14;
            textStyle.wordWrap = true;
            textStyle.normal.textColor = Color.white;
            textStyle.alignment = TextAnchor.UpperLeft;
            GUI.Label(new Rect(boxX + 16, boxY + 30, boxWidth - 32, 40), currentDialogue, textStyle);
        }
    }
}

