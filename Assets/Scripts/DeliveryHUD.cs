using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// DeliveryHUD: Sistem antarmuka grafis (UI) game Kurir Terakhir berbasis Screen-Space Overlay.
/// Membangun dashboard misi, timer, kartu catatan alamat, prompt interaksi [E],
/// overlay denah peta kota [M], serta modal ringkasan Ending 1 / 2 / 3 secara programatis.
/// Mematuhi standar antislop: kontras tinggi, tipografi tajam, supersampling 2.0x, dan tata letak terstruktur.
/// </summary>
public class DeliveryHUD : MonoBehaviour
{
    private static DeliveryHUD instance;
    public static DeliveryHUD Instance => instance;

    [Header("=== UI Elements ===")]
    private Canvas canvas;
    private Font mainFont;

    // Dashboard Elemen (Kiri Atas)
    private GameObject dashboardObj;
    private Text timerText;
    private Text packageProgressText;
    private Text missionTitleText;
    private Text missionClueText;
    private Text penaltyFlashText;
    private float penaltyFlashTimer = 0f;

    // Prompt Interaksi (Tengah Bawah)
    private GameObject interactPromptObj;
    private Text interactPromptText;
    private bool hasInteractableThisFrame = false;
    private string currentInteractableText = "";
    private System.Action currentInteractCallback;
    private int currentInteractablePriority = 0;

    // Peta Kota Sederhana (Modal [M])
    private GameObject mapModalObj;
    private bool isMapOpen = false;

    // Ending Modal (Layar Akhir)
    private GameObject endingModalObj;
    private Text endingTitleText;
    private Text endingStoryText;
    private Text endingStatsText;
    private Button restartButton;
    private Button mainMenuButton;
    private Button nextLevelButton;
    private RectTransform restartButtonRect;
    private RectTransform mainMenuButtonRect;
    private RectTransform nextLevelButtonRect;
    private float endingModalOpenTime = 0f;

    // Main Menu Elemen
    private GameObject mainMenuObj;
    private GameObject howToPlayModalObj;
    private bool isMainMenuOpen = true;
    public bool IsMainMenuOpen => isMainMenuOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    public static void EnsureInstance()
    {
        EnsureEventSystem();
        GameObject go = GameObject.Find("DeliveryHUDSystem");
        if (go == null)
        {
            go = new GameObject("DeliveryHUDSystem");
            instance = go.AddComponent<DeliveryHUD>();
        }
        else
        {
            instance = go.GetComponent<DeliveryHUD>();
            if (instance == null) instance = go.AddComponent<DeliveryHUD>();
            if (instance.timerText == null || instance.missionTitleText == null || instance.dashboardObj == null)
            {
                instance.BuildUI();
            }
        }
    }

    private void Awake()
    {
        EnsureEventSystem();
        if (instance == null) instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BuildUI();
    }

    private void Start()
    {
        BindGameManager();
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2")
        {
            HideMainMenu();
            if (DeliveryGameManager.Instance != null)
            {
                DeliveryGameManager.Instance.StartGame();
            }
        }
        else
        {
            ShowMainMenu();
        }
    }

    private void BindGameManager()
    {
        if (DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.OnTimerTick += UpdateTimerUI;
            DeliveryGameManager.Instance.OnMissionChanged += UpdateMissionUI;
            DeliveryGameManager.Instance.OnPackageProgressChanged += UpdatePackageProgressUI;
            DeliveryGameManager.Instance.OnPenaltyApplied += ShowPenaltyFlash;
            DeliveryGameManager.Instance.OnGameEnded += ShowEndingModal;

            UpdateMissionUI(DeliveryGameManager.Instance.CurrentState);
            UpdatePackageProgressUI(DeliveryGameManager.Instance.PackagesDelivered, DeliveryGameManager.Instance.TotalPackages);
            UpdateTimerUI(DeliveryGameManager.Instance.TimeRemaining);
        }
    }

    private void OnDestroy()
    {
        if (DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.OnTimerTick -= UpdateTimerUI;
            DeliveryGameManager.Instance.OnMissionChanged -= UpdateMissionUI;
            DeliveryGameManager.Instance.OnPackageProgressChanged -= UpdatePackageProgressUI;
            DeliveryGameManager.Instance.OnPenaltyApplied -= ShowPenaltyFlash;
            DeliveryGameManager.Instance.OnGameEnded -= ShowEndingModal;
        }
        if (instance == this) instance = null;
    }

    private Font GetBestFont()
    {
        Font font = null;
#if UNITY_EDITOR
        font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/Develop 3D Mobile Games in Unity/Assets/MobileUI/fonts/Roboto-Bold.ttf");
#endif
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI Bold", "Segoe UI", "Arial" }, 24);
        return font;
    }

    public void BuildUI()
    {
        mainFont = GetBestFont();

        // 1. Bersihkan UI anak lama jika ada agar tidak ada panel duplikat/usang
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform ch = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(ch.gameObject);
            }
            else
            {
                DestroyImmediate(ch.gameObject);
            }
        }

        // 2. Setup Canvas ScreenSpaceOverlay
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // Berada tepat di bawah dialog box (999)

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 2.0f;

        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        BuildDashboardPanel();
        BuildInteractPrompt();
        BuildMapModal();
        BuildEndingModal();
        BuildMainMenu();
        BuildHowToPlayModal();
    }

    private void BuildDashboardPanel()
    {
        dashboardObj = new GameObject("DeliveryDashboard", typeof(RectTransform));
        dashboardObj.transform.SetParent(transform, false);

        RectTransform dbRT = dashboardObj.GetComponent<RectTransform>();
        dbRT.anchorMin = new Vector2(0f, 1f);
        dbRT.anchorMax = new Vector2(0f, 1f);
        dbRT.pivot = new Vector2(0f, 1f);
        dbRT.anchoredPosition = new Vector2(80f, -60f);
        dbRT.sizeDelta = new Vector2(380f, 160f);

        Image bg = dashboardObj.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.08f, 0.16f, 0.94f);

        Outline outline = dashboardObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.5f, 0.9f, 0.7f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Header Strip (Cyan)
        GameObject strip = new GameObject("HeaderStrip", typeof(RectTransform));
        strip.transform.SetParent(dashboardObj.transform, false);
        RectTransform stripRT = strip.GetComponent<RectTransform>();
        stripRT.anchorMin = new Vector2(0f, 1f);
        stripRT.anchorMax = new Vector2(1f, 1f);
        stripRT.pivot = new Vector2(0.5f, 1f);
        stripRT.anchoredPosition = Vector2.zero;
        stripRT.sizeDelta = new Vector2(0f, 3f);
        Image stripImg = strip.AddComponent<Image>();
        stripImg.color = new Color(0.25f, 0.8f, 1f, 1f);

        // Timer Text (Besar & Tegas)
        GameObject timerGo = new GameObject("TimerText", typeof(RectTransform));
        timerGo.transform.SetParent(dashboardObj.transform, false);
        RectTransform timerRT = timerGo.GetComponent<RectTransform>();
        timerRT.anchorMin = new Vector2(0f, 1f);
        timerRT.anchorMax = new Vector2(0f, 1f);
        timerRT.pivot = new Vector2(0f, 1f);
        timerRT.anchoredPosition = new Vector2(16f, -10f);
        timerRT.sizeDelta = new Vector2(170f, 26f);

        timerText = timerGo.AddComponent<Text>();
        timerText.font = mainFont;
        timerText.fontSize = 19;
        timerText.fontStyle = FontStyle.Bold;
        timerText.color = Color.white;
        timerText.alignment = TextAnchor.MiddleLeft;
        timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        timerText.verticalOverflow = VerticalWrapMode.Overflow;
        timerText.text = "⏱ 10:00";

        // Package Counter Text
        GameObject pkgGo = new GameObject("PackageCounterText", typeof(RectTransform));
        pkgGo.transform.SetParent(dashboardObj.transform, false);
        RectTransform pkgRT = pkgGo.GetComponent<RectTransform>();
        pkgRT.anchorMin = new Vector2(1f, 1f);
        pkgRT.anchorMax = new Vector2(1f, 1f);
        pkgRT.pivot = new Vector2(1f, 1f);
        pkgRT.anchoredPosition = new Vector2(-16f, -10f);
        pkgRT.sizeDelta = new Vector2(150f, 26f);

        packageProgressText = pkgGo.AddComponent<Text>();
        packageProgressText.font = mainFont;
        packageProgressText.fontSize = 14;
        packageProgressText.fontStyle = FontStyle.Bold;
        packageProgressText.color = new Color(1f, 0.85f, 0.2f, 1f);
        packageProgressText.alignment = TextAnchor.MiddleRight;
        packageProgressText.horizontalOverflow = HorizontalWrapMode.Overflow;
        packageProgressText.verticalOverflow = VerticalWrapMode.Overflow;
        packageProgressText.text = "📦 Paket: 0 / 5";

        // Mission Title Text
        GameObject titleGo = new GameObject("MissionTitleText", typeof(RectTransform));
        titleGo.transform.SetParent(dashboardObj.transform, false);
        RectTransform titleRT = titleGo.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(0f, 1f);
        titleRT.pivot = new Vector2(0f, 1f);
        titleRT.anchoredPosition = new Vector2(16f, -40f);
        titleRT.sizeDelta = new Vector2(348f, 24f);

        missionTitleText = titleGo.AddComponent<Text>();
        missionTitleText.font = mainFont;
        missionTitleText.fontSize = 13;
        missionTitleText.fontStyle = FontStyle.Bold;
        missionTitleText.color = new Color(0.35f, 0.85f, 1f, 1f);
        missionTitleText.alignment = TextAnchor.MiddleLeft;
        missionTitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        missionTitleText.verticalOverflow = VerticalWrapMode.Overflow;
        missionTitleText.text = "Tujuan: Temui Pak Beni di Pos Pengiriman";

        // Mission Clue Text
        GameObject clueGo = new GameObject("MissionClueText", typeof(RectTransform));
        clueGo.transform.SetParent(dashboardObj.transform, false);
        RectTransform clueRT = clueGo.GetComponent<RectTransform>();
        clueRT.anchorMin = new Vector2(0f, 1f);
        clueRT.anchorMax = new Vector2(0f, 1f);
        clueRT.pivot = new Vector2(0f, 1f);
        clueRT.anchoredPosition = new Vector2(16f, -66f);
        clueRT.sizeDelta = new Vector2(348f, 56f);

        missionClueText = clueGo.AddComponent<Text>();
        missionClueText.font = mainFont;
        missionClueText.fontSize = 12;
        missionClueText.fontStyle = FontStyle.Normal;
        missionClueText.color = new Color(0.9f, 0.92f, 0.95f, 0.95f);
        missionClueText.alignment = TextAnchor.UpperLeft;
        missionClueText.lineSpacing = 1.1f;
        missionClueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        missionClueText.verticalOverflow = VerticalWrapMode.Overflow;
        missionClueText.text = "Ambil 5 paket terakhir dari Pak Beni di dekat mobil pengiriman dan dengarkan instruksi batas waktu.";

        // Map Hint Footer
        GameObject mapHintGo = new GameObject("MapHintText", typeof(RectTransform));
        mapHintGo.transform.SetParent(dashboardObj.transform, false);
        RectTransform mhRT = mapHintGo.GetComponent<RectTransform>();
        mhRT.anchorMin = new Vector2(0f, 1f);
        mhRT.anchorMax = new Vector2(0f, 1f);
        mhRT.pivot = new Vector2(0f, 1f);
        mhRT.anchoredPosition = new Vector2(16f, -130f);
        mhRT.sizeDelta = new Vector2(348f, 20f);

        Text mapHintText = mapHintGo.AddComponent<Text>();
        mapHintText.font = mainFont;
        mapHintText.fontSize = 11;
        mapHintText.fontStyle = FontStyle.Bold;
        mapHintText.color = new Color(0.6f, 0.75f, 0.95f, 0.85f);
        mapHintText.alignment = TextAnchor.MiddleLeft;
        mapHintText.horizontalOverflow = HorizontalWrapMode.Overflow;
        mapHintText.verticalOverflow = VerticalWrapMode.Overflow;
        mapHintText.text = "Tekan [M] untuk melihat Denah Kota";

        // Penalty Flash Text (Overlay Notifikasi di Kanan Layar)
        GameObject penGo = new GameObject("PenaltyFlashText", typeof(RectTransform));
        penGo.transform.SetParent(transform, false);
        RectTransform penRT = penGo.GetComponent<RectTransform>();
        penRT.anchorMin = new Vector2(1f, 1f);
        penRT.anchorMax = new Vector2(1f, 1f);
        penRT.pivot = new Vector2(1f, 1f);
        penRT.anchoredPosition = new Vector2(-60f, -60f);
        penRT.sizeDelta = new Vector2(400f, 40f);

        penaltyFlashText = penGo.AddComponent<Text>();
        penaltyFlashText.font = mainFont;
        penaltyFlashText.fontSize = 18;
        penaltyFlashText.fontStyle = FontStyle.Bold;
        penaltyFlashText.color = new Color(1f, 0.25f, 0.25f, 1f);
        penaltyFlashText.alignment = TextAnchor.MiddleRight;
        penaltyFlashText.horizontalOverflow = HorizontalWrapMode.Overflow;
        penaltyFlashText.verticalOverflow = VerticalWrapMode.Overflow;
        penaltyFlashText.text = "";
        penaltyFlashText.gameObject.SetActive(false);

        Shadow penShadow = penGo.AddComponent<Shadow>();
        penShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        penShadow.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private void BuildInteractPrompt()
    {
        interactPromptObj = new GameObject("InteractPromptBox");
        interactPromptObj.transform.SetParent(transform, false);

        RectTransform ipRT = interactPromptObj.AddComponent<RectTransform>();
        ipRT.anchorMin = new Vector2(0.5f, 0.16f);
        ipRT.anchorMax = new Vector2(0.5f, 0.16f);
        ipRT.pivot = new Vector2(0.5f, 0.5f);
        ipRT.sizeDelta = new Vector2(380f, 44f);

        Image ipBg = interactPromptObj.AddComponent<Image>();
        ipBg.color = new Color(0.04f, 0.08f, 0.18f, 0.95f);

        Outline ipOutline = interactPromptObj.AddComponent<Outline>();
        ipOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.8f);
        ipOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button ipBtn = interactPromptObj.AddComponent<Button>();
        ipBtn.onClick.AddListener(() =>
        {
            GameAudioManager.Instance?.PlayUIClick();
            currentInteractCallback?.Invoke();
        });

        GameObject ipTextGo = new GameObject("PromptText");
        ipTextGo.transform.SetParent(interactPromptObj.transform, false);
        RectTransform txtRT = ipTextGo.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        interactPromptText = ipTextGo.AddComponent<Text>();
        interactPromptText.font = mainFont;
        interactPromptText.fontSize = 16;
        interactPromptText.fontStyle = FontStyle.Bold;
        interactPromptText.color = Color.white;
        interactPromptText.alignment = TextAnchor.MiddleCenter;
        interactPromptText.text = "[E] Bicara";

        interactPromptObj.SetActive(false);
    }

    private void BuildMapModal()
    {
        mapModalObj = new GameObject("CityMapModal");
        mapModalObj.transform.SetParent(transform, false);

        RectTransform mmRT = mapModalObj.AddComponent<RectTransform>();
        mmRT.anchorMin = Vector2.zero;
        mmRT.anchorMax = Vector2.one;
        mmRT.offsetMin = Vector2.zero;
        mmRT.offsetMax = Vector2.zero;

        // Backdrop gelap
        Image bg = mapModalObj.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.04f, 0.08f, 0.88f);

        // Container Peta di Tengah
        GameObject cardGo = new GameObject("MapCard");
        cardGo.transform.SetParent(mapModalObj.transform, false);
        RectTransform cardRT = cardGo.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(680f, 520f);

        Image cardImg = cardGo.AddComponent<Image>();
        cardImg.color = new Color(0.06f, 0.1f, 0.18f, 0.98f);

        Outline cardOutline = cardGo.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.3f, 0.7f, 1f, 0.9f);
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // Header Title
        GameObject titleGo = new GameObject("MapTitle");
        titleGo.transform.SetParent(cardGo.transform, false);
        RectTransform titleRT = titleGo.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -24f);
        titleRT.sizeDelta = new Vector2(0f, 36f);

        bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2";

        Text titleTxt = titleGo.AddComponent<Text>();
        titleTxt.font = mainFont;
        titleTxt.fontSize = 22;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = new Color(0.35f, 0.85f, 1f, 1f);
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.text = isLevel2
            ? "🗺️ DENAH KOTA — LEVEL 2 (RUTE & TATA LETAK BARU)"
            : "🗺️ DENAH KAWASAN KOTA — KURIR TERAKHIR";

        // Layout Lokasi Kota (Grid Panduan Bersih)
        GameObject layoutGo = new GameObject("MapLayoutText");
        layoutGo.transform.SetParent(cardGo.transform, false);
        RectTransform layoutRT = layoutGo.AddComponent<RectTransform>();
        layoutRT.anchorMin = new Vector2(0f, 0f);
        layoutRT.anchorMax = new Vector2(1f, 1f);
        layoutRT.offsetMin = new Vector2(36f, 60f);
        layoutRT.offsetMax = new Vector2(-36f, -60f);

        Text layoutTxt = layoutGo.AddComponent<Text>();
        layoutTxt.font = mainFont;
        layoutTxt.fontSize = 15;
        layoutTxt.fontStyle = FontStyle.Normal;
        layoutTxt.color = Color.white;
        layoutTxt.lineSpacing = 1.35f;
        layoutTxt.alignment = TextAnchor.UpperLeft;
        layoutTxt.text = isLevel2
            ? "📍 AREA UTAMA KOTA LEVEL 2 (TATA LETAK BARU):\n\n" +
              "1. POS PUSAT PENGIRIMAN (Depan Mega Mall):\n" +
              "   • Titik Awal Rian & Pos Pak Beni (Misi Awal & Akhir)\n\n" +
              "2. COFFEE SHOP (Barat Laut):\n" +
              "   • Konter kedai kopi tempat Clara bertugas (Paket 1)\n\n" +
              "3. DRUG STORE (Pertokoan Selatan):\n" +
              "   • Toko obat farmasi cabang selatan milik Pak Joko (Paket 2)\n\n" +
              "4. POS KEAMANAN (Timur Laut dekat Donut Shop):\n" +
              "   • Pos Dimas (Paket 3) & Titik Paket Jatuh di Aspal (Misi 4)\n\n" +
              "5. PERSIMPANGAN TIMUR & HUNIAN BARAT:\n" +
              "   • Petugas Parkir untuk info paket & Naya di hunian barat (Paket 5)\n\n" +
              "⚠️ AWAS: Barikade pekerjaan jalan menutup jalur lurus! Manfaatkan lorong & trotoar."
            : "📍 AREA UTAMA KOTA UNTUK PENGIRIMAN:\n\n" +
              "1. POS PUSAT PENGIRIMAN & AREA PARKIR:\n" +
              "   • Titik Awal Rian & Pos Pak Beni (di Depan Mega Mall)\n" +
              "   • Pos Petugas Parkir (Info Jalur/Lalu Lintas) & Paket Jatuh (Misi 4)\n\n" +
              "2. COFFEE SHOP (Timur Trotoar):\n" +
              "   • Konter kedai kopi tempat Clara bertugas (Paket 1)\n\n" +
              "3. DRUG STORE (Sudut Pertokoan Barat):\n" +
              "   • Toko obat dengan plang nama farmasi milik Pak Joko (Paket 2)\n\n" +
              "4. FIRE STATION / POS PEMADAM (Barat):\n" +
              "   • Pos pemadam kebakaran tempat Dimas bertugas (Paket 3)\n\n" +
              "5. GEDUNG APARTEMEN (Utara Ujung Jalan):\n" +
              "   • Gedung bertingkat tinggi tempat Naya menunggu dokumen (Paket 5)";

        // Footer Tutup
        GameObject footGo = new GameObject("MapFooter");
        footGo.transform.SetParent(cardGo.transform, false);
        RectTransform footRT = footGo.AddComponent<RectTransform>();
        footRT.anchorMin = new Vector2(0f, 0f);
        footRT.anchorMax = new Vector2(1f, 0f);
        footRT.anchoredPosition = new Vector2(0f, 20f);
        footRT.sizeDelta = new Vector2(0f, 30f);

        Text footTxt = footGo.AddComponent<Text>();
        footTxt.font = mainFont;
        footTxt.fontSize = 14;
        footTxt.fontStyle = FontStyle.Bold;
        footTxt.color = new Color(1f, 0.85f, 0.2f, 0.9f);
        footTxt.alignment = TextAnchor.MiddleCenter;
        footTxt.text = "Tekan [M] atau [Escape] untuk Menutup Peta";

        mapModalObj.SetActive(false);
    }

    private void BuildEndingModal()
    {
        endingModalObj = new GameObject("EndingModal");
        endingModalObj.transform.SetParent(transform, false);

        RectTransform emRT = endingModalObj.AddComponent<RectTransform>();
        emRT.anchorMin = Vector2.zero;
        emRT.anchorMax = Vector2.one;
        emRT.offsetMin = Vector2.zero;
        emRT.offsetMax = Vector2.zero;

        // Backdrop gelap
        Image bg = endingModalObj.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.04f, 0.07f, 0.95f);

        GameObject card = new GameObject("EndingCard");
        card.transform.SetParent(endingModalObj.transform, false);
        RectTransform cRT = card.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.5f, 0.5f);
        cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta = new Vector2(720f, 480f);

        Image cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.07f, 0.09f, 0.14f, 0.98f);

        Outline cOutline = card.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.18f, 0.24f, 0.33f, 0.9f);
        cOutline.effectDistance = new Vector2(1f, -1f);

        // Header Strip Amber Accent
        GameObject strip = new GameObject("EndingStrip", typeof(RectTransform));
        strip.transform.SetParent(card.transform, false);
        RectTransform srt = strip.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(0f, 3f);
        Image stripImg = strip.AddComponent<Image>();
        stripImg.color = new Color(0.96f, 0.58f, 0.12f, 1f);

        // Judul Ending
        GameObject titleGo = new GameObject("EndingTitle");
        titleGo.transform.SetParent(card.transform, false);
        RectTransform tRT = titleGo.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -32f);
        tRT.sizeDelta = new Vector2(0f, 42f);

        endingTitleText = titleGo.AddComponent<Text>();
        endingTitleText.font = mainFont;
        endingTitleText.fontSize = 24;
        endingTitleText.fontStyle = FontStyle.Bold;
        endingTitleText.alignment = TextAnchor.MiddleCenter;
        endingTitleText.color = new Color(0.98f, 0.98f, 1f, 1f);
        endingTitleText.text = "ENDING 1: KURIR TERPERCAYA";

        // Narasi Epilog
        GameObject storyGo = new GameObject("EndingStory");
        storyGo.transform.SetParent(card.transform, false);
        RectTransform sRT = storyGo.AddComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 1f);
        sRT.anchorMax = new Vector2(1f, 1f);
        sRT.pivot = new Vector2(0.5f, 1f);
        sRT.anchoredPosition = new Vector2(0f, -80f);
        sRT.sizeDelta = new Vector2(-80f, 130f);

        endingStoryText = storyGo.AddComponent<Text>();
        endingStoryText.font = mainFont;
        endingStoryText.fontSize = 15;
        endingStoryText.fontStyle = FontStyle.Normal;
        endingStoryText.color = new Color(0.88f, 0.90f, 0.94f, 1f);
        endingStoryText.lineSpacing = 1.35f;
        endingStoryText.alignment = TextAnchor.MiddleCenter;
        endingStoryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        endingStoryText.verticalOverflow = VerticalWrapMode.Overflow;
        endingStoryText.text = "Epilog permainan...";

        // Statistik Misi
        GameObject statsGo = new GameObject("EndingStats");
        statsGo.transform.SetParent(card.transform, false);
        RectTransform stRT = statsGo.AddComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 1f);
        stRT.anchorMax = new Vector2(1f, 1f);
        stRT.pivot = new Vector2(0.5f, 1f);
        stRT.anchoredPosition = new Vector2(0f, -225f);
        stRT.sizeDelta = new Vector2(-80f, 45f);

        endingStatsText = statsGo.AddComponent<Text>();
        endingStatsText.font = mainFont;
        endingStatsText.fontSize = 15;
        endingStatsText.fontStyle = FontStyle.Bold;
        endingStatsText.color = new Color(0.96f, 0.65f, 0.20f, 1f);
        endingStatsText.lineSpacing = 1.25f;
        endingStatsText.alignment = TextAnchor.MiddleCenter;
        endingStatsText.text = "Waktu Tersisa: 00:00 | Paket: 5/5 | Salah Antar: 0";

        // Tombol 1: Main Lagi (Kiri)
        GameObject btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(card.transform, false);
        restartButtonRect = btnGo.AddComponent<RectTransform>();
        restartButtonRect.anchorMin = new Vector2(0.5f, 0f);
        restartButtonRect.anchorMax = new Vector2(0.5f, 0f);
        restartButtonRect.pivot = new Vector2(0.5f, 0f);
        restartButtonRect.anchoredPosition = new Vector2(-115f, 32f);
        restartButtonRect.sizeDelta = new Vector2(210f, 48f);

        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = Color.white;

        Outline btnOutline = btnGo.AddComponent<Outline>();
        btnOutline.effectColor = new Color(1f, 0.75f, 0.35f, 0.35f);
        btnOutline.effectDistance = new Vector2(1f, -1f);

        restartButton = btnGo.AddComponent<Button>();
        restartButton.targetGraphic = btnImg;
        ColorBlock rcb = restartButton.colors;
        rcb.colorMultiplier = 1f;
        rcb.fadeDuration = 0.08f;
        rcb.normalColor = new Color(0.85f, 0.45f, 0.08f, 1f);
        rcb.highlightedColor = new Color(0.96f, 0.58f, 0.12f, 1f);
        rcb.pressedColor = new Color(0.68f, 0.32f, 0.04f, 1f);
        rcb.selectedColor = rcb.normalColor;
        restartButton.colors = rcb;

        restartButton.onClick.AddListener(() =>
        {
            if (Time.unscaledTime - endingModalOpenTime < 0.6f) return;
            GameAudioManager.Instance?.PlayUIClick();
            RestartGame();
        });

        GameObject btnTxtGo = new GameObject("ButtonText");
        btnTxtGo.transform.SetParent(btnGo.transform, false);
        RectTransform btRT = btnTxtGo.AddComponent<RectTransform>();
        btRT.anchorMin = Vector2.zero;
        btRT.anchorMax = Vector2.one;
        btRT.offsetMin = Vector2.zero;
        btRT.offsetMax = Vector2.zero;

        Text btnTxt = btnTxtGo.AddComponent<Text>();
        btnTxt.font = mainFont;
        btnTxt.fontSize = 16;
        btnTxt.fontStyle = FontStyle.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.text = "Main Lagi";

        // Tombol 2: Menu Utama (Kanan)
        GameObject menuBtnGo = new GameObject("MainMenuButton");
        menuBtnGo.transform.SetParent(card.transform, false);
        mainMenuButtonRect = menuBtnGo.AddComponent<RectTransform>();
        mainMenuButtonRect.anchorMin = new Vector2(0.5f, 0f);
        mainMenuButtonRect.anchorMax = new Vector2(0.5f, 0f);
        mainMenuButtonRect.pivot = new Vector2(0.5f, 0f);
        mainMenuButtonRect.anchoredPosition = new Vector2(115f, 32f);
        mainMenuButtonRect.sizeDelta = new Vector2(210f, 48f);

        Image menuBtnImg = menuBtnGo.AddComponent<Image>();
        menuBtnImg.color = Color.white;

        Outline menuBtnOutline = menuBtnGo.AddComponent<Outline>();
        menuBtnOutline.effectColor = new Color(0.28f, 0.36f, 0.48f, 0.6f);
        menuBtnOutline.effectDistance = new Vector2(1f, -1f);

        mainMenuButton = menuBtnGo.AddComponent<Button>();
        mainMenuButton.targetGraphic = menuBtnImg;
        ColorBlock mcb = mainMenuButton.colors;
        mcb.colorMultiplier = 1f;
        mcb.fadeDuration = 0.08f;
        mcb.normalColor = new Color(0.14f, 0.18f, 0.25f, 1f);
        mcb.highlightedColor = new Color(0.22f, 0.28f, 0.38f, 1f);
        mcb.pressedColor = new Color(0.10f, 0.13f, 0.18f, 1f);
        mcb.selectedColor = mcb.normalColor;
        mainMenuButton.colors = mcb;

        mainMenuButton.onClick.AddListener(() =>
        {
            if (Time.unscaledTime - endingModalOpenTime < 0.6f) return;
            GameAudioManager.Instance?.PlayUIClick();
            if (DeliveryGameManager.Instance != null)
            {
                DeliveryGameManager.Instance.GoToMainMenu();
            }
            else
            {
                ShowMainMenu();
            }
        });

        GameObject menuTxtGo = new GameObject("ButtonText");
        menuTxtGo.transform.SetParent(menuBtnGo.transform, false);
        RectTransform mtRT = menuTxtGo.AddComponent<RectTransform>();
        mtRT.anchorMin = Vector2.zero;
        mtRT.anchorMax = Vector2.one;
        mtRT.offsetMin = Vector2.zero;
        mtRT.offsetMax = Vector2.zero;

        Text menuTxt = menuTxtGo.AddComponent<Text>();
        menuTxt.font = mainFont;
        menuTxt.fontSize = 16;
        menuTxt.fontStyle = FontStyle.Bold;
        menuTxt.color = Color.white;
        menuTxt.alignment = TextAnchor.MiddleCenter;
        menuTxt.text = "Menu Utama";

        // Tombol 3: Lanjut ke Level 2 (Khusus saat menang di Level 1)
        GameObject nextBtnGo = new GameObject("NextLevelButton");
        nextBtnGo.transform.SetParent(card.transform, false);
        nextLevelButtonRect = nextBtnGo.AddComponent<RectTransform>();
        nextLevelButtonRect.anchorMin = new Vector2(0.5f, 0f);
        nextLevelButtonRect.anchorMax = new Vector2(0.5f, 0f);
        nextLevelButtonRect.pivot = new Vector2(0.5f, 0f);
        nextLevelButtonRect.anchoredPosition = new Vector2(-225f, 32f);
        nextLevelButtonRect.sizeDelta = new Vector2(205f, 48f);

        Image nextBtnImg = nextBtnGo.AddComponent<Image>();
        nextBtnImg.color = Color.white;

        Outline nextOutline = nextBtnGo.AddComponent<Outline>();
        nextOutline.effectColor = new Color(0.2f, 0.8f, 0.4f, 0.5f);
        nextOutline.effectDistance = new Vector2(1f, -1f);

        nextLevelButton = nextBtnGo.AddComponent<Button>();
        nextLevelButton.targetGraphic = nextBtnImg;
        ColorBlock ncb = nextLevelButton.colors;
        ncb.colorMultiplier = 1f;
        ncb.fadeDuration = 0.08f;
        ncb.normalColor = new Color(0.12f, 0.62f, 0.32f, 1f);
        ncb.highlightedColor = new Color(0.18f, 0.78f, 0.40f, 1f);
        ncb.pressedColor = new Color(0.08f, 0.45f, 0.22f, 1f);
        ncb.selectedColor = ncb.normalColor;
        nextLevelButton.colors = ncb;

        nextLevelButton.onClick.AddListener(() =>
        {
            if (Time.unscaledTime - endingModalOpenTime < 0.6f) return;
            GameAudioManager.Instance?.PlayUIClick();
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Module 2_Stage2");
        });

        GameObject nextTxtGo = new GameObject("ButtonText");
        nextTxtGo.transform.SetParent(nextBtnGo.transform, false);
        RectTransform nbtRT = nextTxtGo.AddComponent<RectTransform>();
        nbtRT.anchorMin = Vector2.zero;
        nbtRT.anchorMax = Vector2.one;
        nbtRT.offsetMin = Vector2.zero;
        nbtRT.offsetMax = Vector2.zero;

        Text nextTxt = nextTxtGo.AddComponent<Text>();
        nextTxt.font = mainFont;
        nextTxt.fontSize = 15;
        nextTxt.fontStyle = FontStyle.Bold;
        nextTxt.color = Color.white;
        nextTxt.alignment = TextAnchor.MiddleCenter;
        nextTxt.text = "Lanjut ke Level 2 ▶";

        nextLevelButton.gameObject.SetActive(false);

        endingModalObj.SetActive(false);
    }

    private void BuildMainMenu()
    {
        mainMenuObj = new GameObject("MainMenuOverlay", typeof(RectTransform));
        mainMenuObj.transform.SetParent(transform, false);

        RectTransform mmRT = mainMenuObj.GetComponent<RectTransform>();
        mmRT.anchorMin = Vector2.zero;
        mmRT.anchorMax = Vector2.one;
        mmRT.offsetMin = Vector2.zero;
        mmRT.offsetMax = Vector2.zero;

        Image bg = mainMenuObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);

        // Kartu Kontainer Menu
        GameObject card = new GameObject("MenuContainer", typeof(RectTransform));
        card.transform.SetParent(mainMenuObj.transform, false);
        RectTransform cRT = card.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.5f, 0.5f);
        cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta = new Vector2(540f, 470f);

        Image cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.07f, 0.09f, 0.14f, 0.98f);

        Outline cOutline = card.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.18f, 0.24f, 0.33f, 0.9f);
        cOutline.effectDistance = new Vector2(1f, -1f);

        // Header Strip Amber Accent
        GameObject strip = new GameObject("MenuStrip", typeof(RectTransform));
        strip.transform.SetParent(card.transform, false);
        RectTransform sRT = strip.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 1f);
        sRT.anchorMax = new Vector2(1f, 1f);
        sRT.pivot = new Vector2(0.5f, 1f);
        sRT.anchoredPosition = Vector2.zero;
        sRT.sizeDelta = new Vector2(0f, 3f);
        Image sImg = strip.AddComponent<Image>();
        sImg.color = new Color(0.96f, 0.58f, 0.12f, 1f);

        // Eyebrow Tagline
        GameObject eyeGo = new GameObject("EyebrowText", typeof(RectTransform));
        eyeGo.transform.SetParent(card.transform, false);
        RectTransform eyeRT = eyeGo.GetComponent<RectTransform>();
        eyeRT.anchorMin = new Vector2(0f, 1f);
        eyeRT.anchorMax = new Vector2(1f, 1f);
        eyeRT.pivot = new Vector2(0.5f, 1f);
        eyeRT.anchoredPosition = new Vector2(0f, -28f);
        eyeRT.sizeDelta = new Vector2(0f, 20f);

        Text eyeTxt = eyeGo.AddComponent<Text>();
        eyeTxt.font = mainFont;
        eyeTxt.fontSize = 11;
        eyeTxt.fontStyle = FontStyle.Bold;
        eyeTxt.alignment = TextAnchor.MiddleCenter;
        eyeTxt.color = new Color(0.55f, 0.62f, 0.72f, 1f);
        eyeTxt.text = "KOTA METROPOLIS  •  SIMULASI PENGANTARAN";

        // Judul Game: KURIR TERAKHIR
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform));
        titleGo.transform.SetParent(card.transform, false);
        RectTransform tRT = titleGo.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -54f);
        tRT.sizeDelta = new Vector2(0f, 46f);

        Text titleTxt = titleGo.AddComponent<Text>();
        titleTxt.font = mainFont;
        titleTxt.fontSize = 34;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.98f, 0.98f, 1f, 1f);
        titleTxt.text = "KURIR TERAKHIR";

        Outline tOutline = titleGo.AddComponent<Outline>();
        tOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
        tOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Subtitle
        GameObject subGo = new GameObject("SubtitleText", typeof(RectTransform));
        subGo.transform.SetParent(card.transform, false);
        RectTransform subRT = subGo.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0f, 1f);
        subRT.anchorMax = new Vector2(1f, 1f);
        subRT.pivot = new Vector2(0.5f, 1f);
        subRT.anchoredPosition = new Vector2(0f, -98f);
        subRT.sizeDelta = new Vector2(0f, 22f);

        Text subTxt = subGo.AddComponent<Text>();
        subTxt.font = mainFont;
        subTxt.fontSize = 13;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color = new Color(0.62f, 0.70f, 0.80f, 1f);
        subTxt.text = "Antarkan Seluruh Paket Sebelum Batas Waktu Habis";

        // Tombol 1: Mulai Permainan (Primary Amber)
        CreateMenuButton(card.transform, "Btn_Play", "Mulai Permainan", new Vector2(0f, -156f), new Vector2(290f, 50f),
            new Color(0.85f, 0.45f, 0.08f, 1f), new Color(0.96f, 0.58f, 0.12f, 1f), new Color(0.68f, 0.32f, 0.04f, 1f),
            new Color(1f, 0.75f, 0.35f, 0.35f), 16, () =>
            {
                if (DeliveryGameManager.Instance != null)
                {
                    DeliveryGameManager.Instance.StartGame();
                }
                else
                {
                    HideMainMenu();
                }
            });

        // Tombol 2: Cara Bermain (Secondary Dark Slate)
        CreateMenuButton(card.transform, "Btn_HowToPlay", "Cara Bermain", new Vector2(0f, -218f), new Vector2(290f, 46f),
            new Color(0.14f, 0.18f, 0.25f, 1f), new Color(0.22f, 0.28f, 0.38f, 1f), new Color(0.10f, 0.13f, 0.18f, 1f),
            new Color(0.28f, 0.36f, 0.48f, 0.6f), 15, () =>
            {
                ShowHowToPlay(true);
            });

        // Tombol 3: Keluar (Tertiary Dark)
        CreateMenuButton(card.transform, "Btn_Quit", "Keluar", new Vector2(0f, -276f), new Vector2(290f, 44f),
            new Color(0.09f, 0.12f, 0.16f, 0.95f), new Color(0.35f, 0.12f, 0.15f, 1f), new Color(0.22f, 0.08f, 0.10f, 1f),
            new Color(0.22f, 0.26f, 0.34f, 0.5f), 14, () =>
            {
                QuitGame();
            });

        // Footer Credit
        GameObject footerGo = new GameObject("FooterText", typeof(RectTransform));
        footerGo.transform.SetParent(card.transform, false);
        RectTransform fRT = footerGo.GetComponent<RectTransform>();
        fRT.anchorMin = new Vector2(0f, 0f);
        fRT.anchorMax = new Vector2(1f, 0f);
        fRT.pivot = new Vector2(0.5f, 0f);
        fRT.anchoredPosition = new Vector2(0f, 16f);
        fRT.sizeDelta = new Vector2(0f, 20f);

        Text fTxt = footerGo.AddComponent<Text>();
        fTxt.font = mainFont;
        fTxt.fontSize = 11;
        fTxt.alignment = TextAnchor.MiddleCenter;
        fTxt.color = new Color(0.40f, 0.46f, 0.55f, 0.85f);
        fTxt.text = "Final Project  •  Introduction to Game Development";
    }

    private Button CreateMenuButton(Transform parent, string goName, string text, Vector2 anchoredPos, Vector2 size,
        Color normalColor, Color hoverColor, Color pressedColor, Color outlineColor, int fontSize, System.Action onClick)
    {
        GameObject btnGo = new GameObject(goName, typeof(RectTransform));
        btnGo.transform.SetParent(parent, false);

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = btnGo.AddComponent<Image>();
        img.color = Color.white;

        if (outlineColor.a > 0.01f)
        {
            Outline outline = btnGo.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        Button btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        cb.normalColor = normalColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor = pressedColor;
        cb.selectedColor = normalColor;
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        btn.colors = cb;

        btn.onClick.AddListener(() =>
        {
            GameAudioManager.Instance?.PlayUIClick();
            onClick?.Invoke();
        });

        GameObject txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(btnGo.transform, false);
        RectTransform txtRT = txtGo.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        Text t = txtGo.AddComponent<Text>();
        t.font = mainFont;
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = text;

        return btn;
    }

    private void BuildHowToPlayModal()
    {
        howToPlayModalObj = new GameObject("HowToPlayModal", typeof(RectTransform));
        howToPlayModalObj.transform.SetParent(transform, false);

        RectTransform rt = howToPlayModalObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = howToPlayModalObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.10f, 0.90f);

        GameObject card = new GameObject("Card", typeof(RectTransform));
        card.transform.SetParent(howToPlayModalObj.transform, false);
        RectTransform cRT = card.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.5f, 0.5f);
        cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta = new Vector2(660f, 480f);

        Image cImg = card.AddComponent<Image>();
        cImg.color = new Color(0.07f, 0.09f, 0.14f, 0.98f);

        Outline cOutline = card.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.18f, 0.24f, 0.33f, 0.9f);
        cOutline.effectDistance = new Vector2(1f, -1f);

        // Header Strip Amber Accent
        GameObject strip = new GameObject("HowToPlayStrip", typeof(RectTransform));
        strip.transform.SetParent(card.transform, false);
        RectTransform sRT = strip.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 1f);
        sRT.anchorMax = new Vector2(1f, 1f);
        sRT.pivot = new Vector2(0.5f, 1f);
        sRT.anchoredPosition = Vector2.zero;
        sRT.sizeDelta = new Vector2(0f, 3f);
        Image sImg = strip.AddComponent<Image>();
        sImg.color = new Color(0.96f, 0.58f, 0.12f, 1f);

        // Judul Modal
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(card.transform, false);
        RectTransform tRT = titleGo.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -24f);
        tRT.sizeDelta = new Vector2(0f, 32f);

        Text titleTxt = titleGo.AddComponent<Text>();
        titleTxt.font = mainFont;
        titleTxt.fontSize = 20;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.98f, 0.98f, 1f, 1f);
        titleTxt.text = "PANDUAN CARA BERMAIN";

        // Teks Kontrol & Aturan (diletakkan rapi di dalam card, margin kiri-kanan jelas)
        GameObject bodyGo = new GameObject("BodyText");
        bodyGo.transform.SetParent(card.transform, false);
        RectTransform bRT = bodyGo.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0f, 1f);
        bRT.anchorMax = new Vector2(0f, 1f);
        bRT.pivot = new Vector2(0f, 1f);
        bRT.anchoredPosition = new Vector2(45f, -65f);
        bRT.sizeDelta = new Vector2(570f, 340f);

        Text bodyTxt = bodyGo.AddComponent<Text>();
        bodyTxt.font = mainFont;
        bodyTxt.fontSize = 12;
        bodyTxt.lineSpacing = 1.35f;
        bodyTxt.color = new Color(0.90f, 0.92f, 0.95f, 1f);
        bodyTxt.alignment = TextAnchor.UpperLeft;
        bodyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyTxt.verticalOverflow = VerticalWrapMode.Overflow;
        bodyTxt.supportRichText = true;
        bodyTxt.text =
            "<b><color=#F59E0B>KONTROL KARAKTER:</color></b>\n" +
            "  • <b>[W] [A] [S] [D]</b>  : Bergerak & berjalan melintasi jalan dan trotoar kota\n" +
            "  • <b>[Left Shift]</b>      : Berlari lebih cepat (Sprint)\n" +
            "  • <b>[Mouse]</b>           : Mengarahkan kamera pandangan Rian\n" +
            "  • <b>[E]</b>               : Berbicara dengan warga, bertanya arah, dan mengambil paket\n" +
            "  • <b>[M]</b>               : Membuka denah peta kota & lokasi pertokoan\n\n" +
            "<b><color=#F59E0B>ATURAN PERMAINAN & MISI:</color></b>\n" +
            "  1. Ambil 5 paket dari Pak Beni di depan Mall untuk memulai pengantaran.\n" +
            "  2. Antarkan seluruh 5 paket ke toko yang sesuai sebelum batas waktu 10 menit habis.\n" +
            "  3. Hati-hati menyeberang jalan raya! Terserempet mobil mengurangi sisa waktu (-5 dtk).\n" +
            "  4. Salah memberikan paket ke orang yang salah akan terkena penalti waktu (-20 dtk).\n" +
            "  5. Jika ada paket terjatuh di jalan, tanyakan lokasinya ke Petugas Parkir!";

        // Tombol Tutup
        CreateMenuButton(card.transform, "Btn_Close", "Tutup", new Vector2(0f, -422f), new Vector2(170f, 40f),
            new Color(0.14f, 0.18f, 0.25f, 1f), new Color(0.22f, 0.28f, 0.38f, 1f), new Color(0.10f, 0.13f, 0.18f, 1f),
            new Color(0.28f, 0.36f, 0.48f, 0.6f), 14, () =>
            {
                ShowHowToPlay(false);
            });

        howToPlayModalObj.SetActive(false);
    }

    public void ShowHowToPlay(bool show)
    {
        if (howToPlayModalObj != null)
        {
            howToPlayModalObj.SetActive(show);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ShowMainMenu()
    {
        isMainMenuOpen = true;
        if (mainMenuObj != null) mainMenuObj.SetActive(true);
        if (dashboardObj != null) dashboardObj.SetActive(false);
        if (interactPromptObj != null) interactPromptObj.SetActive(false);
        if (howToPlayModalObj != null) howToPlayModalObj.SetActive(false);
        if (mapModalObj != null) mapModalObj.SetActive(false);
        if (endingModalObj != null) endingModalObj.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.SetMovementFrozen(true);
    }

    public void HideMainMenu()
    {
        isMainMenuOpen = false;
        if (mainMenuObj != null) mainMenuObj.SetActive(false);
        if (dashboardObj != null) dashboardObj.SetActive(true);
        if (howToPlayModalObj != null) howToPlayModalObj.SetActive(false);

        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null) pc.SetMovementFrozen(false);
    }

    public void HideEndingModal()
    {
        if (endingModalObj != null)
        {
            endingModalObj.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.RestartGame();
        }
    }

    private void Update()
    {
        // Jika Main Menu sedang aktif, kunci HUD gameplay & kursor bebas bergerak
        if (isMainMenuOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (interactPromptObj != null) interactPromptObj.SetActive(false);
            return;
        }

        // Toggle Peta [M]
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
        else if (isMapOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMap();
        }

        // Update Penalty Flash
        if (penaltyFlashTimer > 0f)
        {
            penaltyFlashTimer -= Time.unscaledDeltaTime;
            if (penaltyFlashTimer <= 0f && penaltyFlashText != null)
            {
                penaltyFlashText.gameObject.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        if (isMainMenuOpen)
        {
            if (interactPromptObj != null) interactPromptObj.SetActive(false);
            hasInteractableThisFrame = false;
            currentInteractablePriority = 0;
            return;
        }

        // Tampilkan/sembunyikan prompt [E]
        if (interactPromptObj != null)
        {
            if (hasInteractableThisFrame && !isMapOpen && (endingModalObj == null || !endingModalObj.activeSelf))
            {
                interactPromptObj.SetActive(true);
                if (interactPromptText != null)
                {
                    interactPromptText.text = "[E] " + currentInteractableText;
                }

                // Eksekusi interaksi tombol [E] secara terpusat untuk aksi yang sedang aktif tampil di layar
                if (Input.GetKeyDown(KeyCode.E) && !NPCDialogueOverlay.IsDialogueActive)
                {
                    GameAudioManager.Instance?.PlayUIClick();
                    currentInteractCallback?.Invoke();
                }
            }
            else
            {
                interactPromptObj.SetActive(false);
            }
        }

        hasInteractableThisFrame = false;
        currentInteractablePriority = 0;
    }

    public void RegisterNearbyInteractable(string actionLabel, System.Action onInteract, int priority = 0)
    {
        if (!hasInteractableThisFrame || priority >= currentInteractablePriority)
        {
            hasInteractableThisFrame = true;
            currentInteractableText = actionLabel;
            currentInteractCallback = onInteract;
            currentInteractablePriority = priority;
        }
    }

    public void ToggleMap()
    {
        if (mapModalObj == null) return;
        isMapOpen = !isMapOpen;
        GameAudioManager.Instance?.PlayUIClick();
        mapModalObj.SetActive(isMapOpen);
    }

    public void UpdateTimerUI(float secondsRemaining)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(secondsRemaining / 60f);
        int seconds = Mathf.FloorToInt(secondsRemaining % 60f);
        timerText.text = string.Format("⏱ {0:00}:{1:00}", minutes, seconds);

        if (secondsRemaining <= 60f)
        {
            timerText.color = new Color(1f, 0.25f, 0.25f, 1f); // Merah peringatan
        }
        else if (secondsRemaining <= 180f)
        {
            timerText.color = new Color(1f, 0.85f, 0.25f, 1f); // Kuning waspada
        }
        else
        {
            timerText.color = Color.white;
        }
    }

    public void UpdatePackageProgressUI(int delivered, int total)
    {
        if (packageProgressText != null)
        {
            packageProgressText.text = $"📦 Paket: {delivered} / {total}";
        }
    }

    public void UpdateMissionUI(DeliveryMissionState state)
    {
        if (missionTitleText == null || missionClueText == null) return;

        bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2";

        if (isLevel2)
        {
            switch (state)
            {
                case DeliveryMissionState.Prolog:
                    missionTitleText.text = "Tujuan: Temui Pak Beni di Pos Pengiriman (Level 2)";
                    missionClueText.text = "Ambil 5 paket baru dari Pak Beni di depan Mall. Perhatikan denah rute baru [M] dan selesaikan sebelum batas waktu 7 menit habis!";
                    break;
                case DeliveryMissionState.Package1_CoffeeShop:
                    missionTitleText.text = "Tujuan: Clara di Coffee Shop (Barat Laut)";
                    missionClueText.text = "Label basah: 'C...ra' - Kedai kopi cabang barat di dekat bundaran jalan barat laut.";
                    break;
                case DeliveryMissionState.Package2_DrugStore:
                    missionTitleText.text = "Tujuan: Pak Joko di Drug Store (Selatan)";
                    missionClueText.text = "Label basah: 'Pak J...o' - Apotek cabang selatan di sudut deretan pertokoan bawah.";
                    break;
                case DeliveryMissionState.Package3_HardwareStore:
                    missionTitleText.text = "Tujuan: Dimas di Pos Keamanan (Timur Laut)";
                    missionClueText.text = "Label basah: 'Di...s' - Pos penjagaan timur laut di samping deretan toko donat.";
                    break;
                case DeliveryMissionState.Package4_DroppedPackage:
                    if (DeliveryGameManager.Instance != null && !DeliveryGameManager.Instance.IsDroppedPackageRevealed)
                    {
                        missionTitleText.text = "Tujuan: Tanya Petugas Parkir (Persimpangan Timur)";
                        missionClueText.text = "Gawat! Satu paket terjatuh di jalan gang. Tanyakan lokasinya ke Petugas Parkir di persimpangan timur!";
                    }
                    else
                    {
                        missionTitleText.text = "Tujuan: Ambil Paket yang Terjatuh!";
                        missionClueText.text = "Kotak paket kardus terlihat di jalan aspal dekat trotoar toko donat sebelah utara. Dekati dan tekan [E] untuk memungutnya!";
                    }
                    break;
                case DeliveryMissionState.Package5_Apartment:
                    missionTitleText.text = "Tujuan: Naya di Hunian Barat";
                    missionClueText.text = "Label basah: 'N...a' - Pengiriman dokumen darurat ke gedung hunian di kawasan barat kota.";
                    break;
                case DeliveryMissionState.Final_ReturnToHub:
                    missionTitleText.text = "Tujuan: Kembali ke Pak Beni di Depan Mall!";
                    missionClueText.text = "Seluruh 5 paket Level 2 selesai dikirim! Segera kembali ke Pak Beni sebelum batas waktu berakhir!";
                    break;
                case DeliveryMissionState.Completed:
                    missionTitleText.text = "Level 2 Selesai!";
                    missionClueText.text = "Seluruh paket Level 2 berhasil sampai ke tangan penerima di rute baru.";
                    break;
                case DeliveryMissionState.Failed:
                    missionTitleText.text = "Waktu Habis!";
                    missionClueText.text = "Batas waktu pengiriman malam ini telah berakhir.";
                    break;
            }
            return;
        }

        switch (state)
        {
            case DeliveryMissionState.Prolog:
                missionTitleText.text = "Tujuan: Temui Pak Beni di Pos Pengiriman";
                missionClueText.text = "Ambil 5 paket terakhir dari Pak Beni di dekat mobil pengiriman dan dengarkan instruksi batas waktu.";
                break;
            case DeliveryMissionState.Package1_CoffeeShop:
                missionTitleText.text = "Tujuan: Clara di Coffee Shop (Area Timur)";
                missionClueText.text = "Label basah: 'C...ra' - Pegawai kedai kopi beraroma harum di trotoar timur sebelum toko tutup.";
                break;
            case DeliveryMissionState.Package2_DrugStore:
                missionTitleText.text = "Tujuan: Pak Joko di Drug Store (Sudut Toko)";
                missionClueText.text = "Label basah: 'Pak J...o' - Pemilik toko obat dan farmasi di sudut deretan pertokoan barat.";
                break;
            case DeliveryMissionState.Package3_HardwareStore:
                missionTitleText.text = "Tujuan: Dimas di Fire Station (Pos Pemadam)";
                missionClueText.text = "Label basah: 'Di...s' - Pegawai pos pemadam kebakaran (Fire Station) di sebelah barat deretan toko.";
                break;
            case DeliveryMissionState.Package4_DroppedPackage:
                if (DeliveryGameManager.Instance != null && !DeliveryGameManager.Instance.IsDroppedPackageRevealed)
                {
                    missionTitleText.text = "Tujuan: Tanya Petugas Parkir (Area Parkir)";
                    missionClueText.text = "Gawat! Satu paket terjatuh di jalan. Dimas menyuruhmu bertanya ke Petugas Parkir untuk mengetahui lokasi persis paketmu!";
                }
                else
                {
                    missionTitleText.text = "Tujuan: Ambil Paket yang Terjatuh!";
                    missionClueText.text = "Kotak paket kardus terlihat di jalan aspal dekat pembatas beton sebelah Fire Station. Dekati dan tekan [E] untuk memungutnya!";
                }
                break;
            case DeliveryMissionState.Package5_Apartment:
                missionTitleText.text = "Tujuan: Naya di Gedung Apartemen";
                missionClueText.text = "Label basah: 'N...a' - Penghuni gedung apartemen bertingkat tinggi di ujung kawasan kota.";
                break;
            case DeliveryMissionState.Final_ReturnToHub:
                missionTitleText.text = "Tujuan: Kembali ke Pak Beni di Pos Pengiriman!";
                missionClueText.text = "Seluruh 5 paket berhasil dikirim! Segera kembali ke Pak Beni sebelum batas waktu berakhir!";
                break;
            case DeliveryMissionState.Completed:
                missionTitleText.text = "Misi Selesai!";
                missionClueText.text = "Seluruh paket berhasil sampai ke tangan penerima yang benar.";
                break;
            case DeliveryMissionState.Failed:
                missionTitleText.text = "Waktu Habis!";
                missionClueText.text = "Batas waktu pengiriman malam ini telah berakhir.";
                break;
        }
    }

    public void ShowPenaltyFlash(float secondsDeducted, string message)
    {
        if (penaltyFlashText != null)
        {
            penaltyFlashText.text = message;
            penaltyFlashText.gameObject.SetActive(true);
            penaltyFlashTimer = 1.6f;
        }
    }

    public void ShowEndingModal(GameEnding ending)
    {
        if (endingModalObj == null) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        endingModalOpenTime = Time.unscaledTime;

        endingModalObj.SetActive(true);

        float timeLeft = DeliveryGameManager.Instance != null ? DeliveryGameManager.Instance.TimeRemaining : 0f;
        int delivered = DeliveryGameManager.Instance != null ? DeliveryGameManager.Instance.PackagesDelivered : 0;
        int wrong = DeliveryGameManager.Instance != null ? DeliveryGameManager.Instance.WrongDeliveryCount : 0;

        int min = Mathf.FloorToInt(timeLeft / 60f);
        int sec = Mathf.FloorToInt(timeLeft % 60f);

        if (endingStatsText != null)
        {
            endingStatsText.text = $"Sisa Waktu: {min:00}:{sec:00}  |  Paket Terkirim: {delivered}/5  |  Salah Alamat: {wrong} kali";
        }

        bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2";

        if (isLevel2)
        {
            // Level 2 Ending Modal: 2 tombol (Main Lagi & Menu Utama)
            if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
            if (restartButtonRect != null)
            {
                restartButtonRect.anchoredPosition = new Vector2(-115f, 32f);
                restartButtonRect.sizeDelta = new Vector2(210f, 48f);
            }
            if (mainMenuButtonRect != null)
            {
                mainMenuButtonRect.anchoredPosition = new Vector2(115f, 32f);
                mainMenuButtonRect.sizeDelta = new Vector2(210f, 48f);
            }

            switch (ending)
            {
                case GameEnding.Ending1_KurirTerpercaya:
                    if (endingTitleText != null)
                    {
                        endingTitleText.text = "🏆 TAMAT: MASTER KURIR KOTA!";
                        endingTitleText.color = new Color(1f, 0.85f, 0.2f, 1f);
                    }
                    if (endingStoryText != null)
                    {
                        endingStoryText.text =
                            "Semua lima paket Level 2 berhasil diantarkan dengan sempurna melintasi rute baru!\n\n" +
                            "Rian membuktikan keahlian navigasi terbaiknya dalam menaklukkan tata letak kota yang berubah dan rintangan barikade jalan.\n" +
                            "Selamat! Kamu telah menamatkan seluruh tantangan dan resmi dinobatkan sebagai Master Kurir Kota!";
                    }
                    break;

                case GameEnding.Ending2_PengirimanTerlambat:
                    if (endingTitleText != null)
                    {
                        endingTitleText.text = "⚠️ LEVEL 2: PENGIRIMAN TERLAMBAT";
                        endingTitleText.color = new Color(1f, 0.55f, 0.15f, 1f);
                    }
                    if (endingStoryText != null)
                    {
                        endingStoryText.text =
                            "Waktu pengiriman di kota baru telah habis sebelum semua paket sampai!\n\n" +
                            "Barikade jalan dan rute baru membuat perjalanan lebih menantang. " +
                            "Coba lagi untuk menguasai rute kota Level 2!";
                    }
                    break;

                case GameEnding.Ending3_GagalBertugas:
                    if (endingTitleText != null)
                    {
                        endingTitleText.text = "❌ LEVEL 2: GAGAL BERTUGAS";
                        endingTitleText.color = new Color(1f, 0.3f, 0.3f, 1f);
                    }
                    if (endingStoryText != null)
                    {
                        endingStoryText.text =
                            "Waktu habis dan kamu belum berhasil menyelesaikan misi Level 2.\n\n" +
                            "Perhatikan denah peta [M] dan hindari menabrak rintangan barikade jalan untuk menghemat waktu!";
                    }
                    break;
            }
        }
        else
        {
            // Level 1 (Module 2)
            if (ending == GameEnding.Ending1_KurirTerpercaya)
            {
                // Menang di Level 1: Tampilkan 3 tombol (Lanjut ke Level 2, Main Lagi, Menu Utama)
                if (nextLevelButton != null)
                {
                    nextLevelButton.gameObject.SetActive(true);
                    if (nextLevelButtonRect != null)
                    {
                        nextLevelButtonRect.anchoredPosition = new Vector2(-225f, 32f);
                        nextLevelButtonRect.sizeDelta = new Vector2(205f, 48f);
                    }
                }
                if (restartButtonRect != null)
                {
                    restartButtonRect.anchoredPosition = new Vector2(0f, 32f);
                    restartButtonRect.sizeDelta = new Vector2(185f, 48f);
                }
                if (mainMenuButtonRect != null)
                {
                    mainMenuButtonRect.anchoredPosition = new Vector2(215f, 32f);
                    mainMenuButtonRect.sizeDelta = new Vector2(185f, 48f);
                }

                if (endingTitleText != null)
                {
                    endingTitleText.text = "🏆 ENDING 1: KURIR TERPERCAYA";
                    endingTitleText.color = new Color(1f, 0.85f, 0.2f, 1f);
                }
                if (endingStoryText != null)
                {
                    endingStoryText.text =
                        "Semua lima paket berhasil diantarkan dengan benar sebelum waktu habis!\n\n" +
                        "Rian menyelesaikan tugas malam itu dengan sempurna dan membuktikan dedikasinya.\n" +
                        "Mulai besok, Rian resmi mendapat kepercayaan penuh untuk diangkat menjadi kurir tetap di kawasan kota ini!";
                }
            }
            else
            {
                // Kalah di Level 1: Tampilkan 2 tombol (Main Lagi & Menu Utama)
                if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
                if (restartButtonRect != null)
                {
                    restartButtonRect.anchoredPosition = new Vector2(-115f, 32f);
                    restartButtonRect.sizeDelta = new Vector2(210f, 48f);
                }
                if (mainMenuButtonRect != null)
                {
                    mainMenuButtonRect.anchoredPosition = new Vector2(115f, 32f);
                    mainMenuButtonRect.sizeDelta = new Vector2(210f, 48f);
                }

                if (ending == GameEnding.Ending2_PengirimanTerlambat)
                {
                    if (endingTitleText != null)
                    {
                        endingTitleText.text = "⚠️ ENDING 2: PENGIRIMAN TERLAMBAT";
                        endingTitleText.color = new Color(1f, 0.55f, 0.15f, 1f);
                    }
                    if (endingStoryText != null)
                    {
                        endingStoryText.text =
                            "Waktu pengiriman telah habis sebelum semua paket sampai!\n\n" +
                            "Sebagian besar paket berhasil sampai kepada penerimanya, namun keterlambatan ini menjadi evaluasi berharga. " +
                            "Rian harus terus mengasah kemampuannya dalam membaca rute kota dan mengelola waktu di kesempatan berikutnya.";
                    }
                }
                else
                {
                    if (endingTitleText != null)
                    {
                        endingTitleText.text = "❌ ENDING 3: GAGAL BERTUGAS";
                        endingTitleText.color = new Color(1f, 0.3f, 0.3f, 1f);
                    }
                    if (endingStoryText != null)
                    {
                        endingStoryText.text =
                            "Waktu telah habis dan sebagian besar toko telah menutup pintunya.\n\n" +
                            "Paket-paket yang belum terkirim harus dikembalikan ke pusat pengiriman dan tugas malam ini dinyatakan belum berhasil. " +
                            "Rian perlu berlatih kembali menghafal jalanan kota agar dapat menyelesaikan pengiriman tepat waktu.";
                    }
                }
            }
        }
    }
}

