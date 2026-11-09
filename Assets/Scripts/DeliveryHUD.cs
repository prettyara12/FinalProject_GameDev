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

    // Peta Kota Sederhana (Modal [M])
    private GameObject mapModalObj;
    private bool isMapOpen = false;

    // Ending Modal (Layar Akhir)
    private GameObject endingModalObj;
    private Text endingTitleText;
    private Text endingStoryText;
    private Text endingStatsText;
    private Button restartButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    public static void EnsureInstance()
    {
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

        // Penalty Flash Text (Overlay Notifikasi)
        GameObject penGo = new GameObject("PenaltyFlashText", typeof(RectTransform));
        penGo.transform.SetParent(dashboardObj.transform, false);
        RectTransform penRT = penGo.GetComponent<RectTransform>();
        penRT.anchorMin = new Vector2(0.5f, 0.5f);
        penRT.anchorMax = new Vector2(0.5f, 0.5f);
        penRT.pivot = new Vector2(0.5f, 0.5f);
        penRT.anchoredPosition = Vector2.zero;
        penRT.sizeDelta = new Vector2(320f, 36f);

        penaltyFlashText = penGo.AddComponent<Text>();
        penaltyFlashText.font = mainFont;
        penaltyFlashText.fontSize = 18;
        penaltyFlashText.fontStyle = FontStyle.Bold;
        penaltyFlashText.color = new Color(1f, 0.2f, 0.2f, 1f);
        penaltyFlashText.alignment = TextAnchor.MiddleCenter;
        penaltyFlashText.horizontalOverflow = HorizontalWrapMode.Overflow;
        penaltyFlashText.verticalOverflow = VerticalWrapMode.Overflow;
        penaltyFlashText.text = "";
        penaltyFlashText.gameObject.SetActive(false);
    }

    private void BuildInteractPrompt()
    {
        interactPromptObj = new GameObject("InteractPromptBox");
        interactPromptObj.transform.SetParent(transform, false);

        RectTransform ipRT = interactPromptObj.AddComponent<RectTransform>();
        ipRT.anchorMin = new Vector2(0.5f, 0.16f);
        ipRT.anchorMax = new Vector2(0.5f, 0.16f);
        ipRT.pivot = new Vector2(0.5f, 0.5f);
        ipRT.sizeDelta = new Vector2(340f, 44f);

        Image ipBg = interactPromptObj.AddComponent<Image>();
        ipBg.color = new Color(0.04f, 0.08f, 0.18f, 0.95f);

        Outline ipOutline = interactPromptObj.AddComponent<Outline>();
        ipOutline.effectColor = new Color(0.2f, 0.8f, 1f, 0.8f);
        ipOutline.effectDistance = new Vector2(1.5f, -1.5f);

        Button ipBtn = interactPromptObj.AddComponent<Button>();
        ipBtn.onClick.AddListener(() =>
        {
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

        Text titleTxt = titleGo.AddComponent<Text>();
        titleTxt.font = mainFont;
        titleTxt.fontSize = 22;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = new Color(0.35f, 0.85f, 1f, 1f);
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.text = "🗺️ DENAH KAWASAN KOTA — KURIR TERAKHIR";

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
        layoutTxt.text =
            "📍 AREA UTAMA KOTA UNTUK PENGIRIMAN:\n\n" +
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
        cImg.color = new Color(0.05f, 0.08f, 0.16f, 0.98f);

        Outline cOutline = card.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.3f, 0.7f, 1f, 0.9f);
        cOutline.effectDistance = new Vector2(2f, -2f);

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
        endingTitleText.color = new Color(1f, 0.85f, 0.2f, 1f);
        endingTitleText.text = "ENDING 1: KURIR TERPERCAYA";

        // Narasi Epilog
        GameObject storyGo = new GameObject("EndingStory");
        storyGo.transform.SetParent(card.transform, false);
        RectTransform sRT = storyGo.AddComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 1f);
        sRT.anchorMax = new Vector2(1f, 1f);
        sRT.anchoredPosition = new Vector2(0f, -140f);
        sRT.sizeDelta = new Vector2(640f, 140f);

        endingStoryText = storyGo.AddComponent<Text>();
        endingStoryText.font = mainFont;
        endingStoryText.fontSize = 16;
        endingStoryText.fontStyle = FontStyle.Normal;
        endingStoryText.color = Color.white;
        endingStoryText.lineSpacing = 1.3f;
        endingStoryText.alignment = TextAnchor.MiddleCenter;
        endingStoryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        endingStoryText.text = "Epilog permainan...";

        // Statistik Misi
        GameObject statsGo = new GameObject("EndingStats");
        statsGo.transform.SetParent(card.transform, false);
        RectTransform stRT = statsGo.AddComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 1f);
        stRT.anchorMax = new Vector2(1f, 1f);
        stRT.anchoredPosition = new Vector2(0f, -260f);
        stRT.sizeDelta = new Vector2(640f, 70f);

        endingStatsText = statsGo.AddComponent<Text>();
        endingStatsText.font = mainFont;
        endingStatsText.fontSize = 15;
        endingStatsText.fontStyle = FontStyle.Bold;
        endingStatsText.color = new Color(0.4f, 0.85f, 1f, 1f);
        endingStatsText.lineSpacing = 1.25f;
        endingStatsText.alignment = TextAnchor.MiddleCenter;
        endingStatsText.text = "Waktu Tersisa: 00:00 | Paket: 5/5 | Salah Antar: 0";

        // Tombol Main Lagi
        GameObject btnGo = new GameObject("RestartButton");
        btnGo.transform.SetParent(card.transform, false);
        RectTransform bRT = btnGo.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0f);
        bRT.anchorMax = new Vector2(0.5f, 0f);
        bRT.pivot = new Vector2(0.5f, 0f);
        bRT.anchoredPosition = new Vector2(0f, 32f);
        bRT.sizeDelta = new Vector2(220f, 50f);

        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.45f, 0.9f, 1f);

        Outline btnOutline = btnGo.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.3f, 0.8f, 1f, 0.9f);
        btnOutline.effectDistance = new Vector2(1.5f, -1.5f);

        restartButton = btnGo.AddComponent<Button>();
        restartButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
        btnTxt.fontSize = 17;
        btnTxt.fontStyle = FontStyle.Bold;
        btnTxt.color = Color.white;
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.text = "Main Lagi (Restart)";

        endingModalObj.SetActive(false);
    }

    private void Update()
    {
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
            }
            else
            {
                interactPromptObj.SetActive(false);
            }
        }

        hasInteractableThisFrame = false;
    }

    public void RegisterNearbyInteractable(string actionLabel, System.Action onInteract)
    {
        hasInteractableThisFrame = true;
        currentInteractableText = actionLabel;
        currentInteractCallback = onInteract;
    }

    public void ToggleMap()
    {
        if (mapModalObj == null) return;
        isMapOpen = !isMapOpen;
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
                missionTitleText.text = "Tujuan: Ambil Paket yang Terjatuh!";
                missionClueText.text = "Gawat! Satu paket terjatuh di jalan. Dekati kotak kardus cokelat di depanmu dan tekan [E] untuk memungutnya!";
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

        switch (ending)
        {
            case GameEnding.Ending1_KurirTerpercaya:
                if (endingTitleText != null)
                {
                    endingTitleText.text = "🏆 ENDING 1: KURIR TERPERCAYA";
                    endingTitleText.color = new Color(1f, 0.85f, 0.2f, 1f);
                }
                if (endingStoryText != null)
                {
                    endingStoryText.text =
                        "Semua lima paket berhasil diantarkan dengan benar sebelum waktu habis!\n\n" +
                        "Rian menyelesaikan tugas malam itu dengan sempurna dan membuktikan dedikasinya. " +
                        "Mulai besok, Rian resmi mendapat kepercayaan penuh untuk diangkat menjadi kurir tetap di kawasan kota ini!";
                }
                break;

            case GameEnding.Ending2_PengirimanTerlambat:
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
                break;

            case GameEnding.Ending3_GagalBertugas:
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
                break;
        }
    }
}

