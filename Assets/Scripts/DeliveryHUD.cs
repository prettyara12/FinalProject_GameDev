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
    private GameObject penaltyFlashRoot;
    private GameObject bumpNoticeRoot;
    private Text bumpNoticeText;
    private float bumpNoticeTimer = 0f;
    private RectTransform packageSegmentsRoot;
    private readonly List<Image> packageSegments = new List<Image>();
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

    // Layar Transisi Antar Level
    private GameObject transitionOverlayObj;
    private Text transitionTitleText;
    private Text transitionSubText;
    private Text transitionStatusText;
    private bool isTransitioning = false;

    // Radar HUD Kompas Dinamis (Pojok Kanan Atas)
    private GameObject radarObj;
    private DeliveryRadarHUD radarComponent;

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
        }

        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2" || isAirportLevel2)
        {
            instance.isMainMenuOpen = false;
        }

        if (instance.timerText == null || instance.missionTitleText == null || instance.dashboardObj == null || instance.radarObj == null)
        {
            instance.BuildUI();
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

        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2" || isAirportLevel2)
        {
            isMainMenuOpen = false;
        }

        BuildUI();
    }

    private void Start()
    {
        BindGameManager();
        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2" || isAirportLevel2)
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
                ch.SetParent(null);
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
        BuildTransitionOverlay();
        BuildMainMenu();
        BuildHowToPlayModal();
        BuildRadarHUD();
    }

    private void BuildDashboardPanel()
    {
        const float W = 400f, H = 206f;

        dashboardObj = new GameObject("DeliveryDashboard", typeof(RectTransform));
        dashboardObj.transform.SetParent(transform, false);
        RectTransform dbRT = dashboardObj.GetComponent<RectTransform>();
        HudStyle.Place(dbRT, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(88f, -76f), new Vector2(W, H));
        dbRT.localScale = new Vector3(0.85f, 0.85f, 1f); // ringkas + digeser ke kanan-bawah agar isinya tidak terpotong di tepi Game view

        // Bayangan lembut + panel kaca gelap
        Image shadow = HudStyle.AddImage(dbRT, "Shadow", HudStyle.Glow(16f, 18f), new Color(0f, 0f, 0f, 0.45f));
        HudStyle.Stretch(shadow.rectTransform, -18f, -24f, -18f, -12f);
        Image panel = HudStyle.AddPanel(dbRT, "Panel", 16f, HudStyle.Panel, HudStyle.Stroke);
        HudStyle.Stretch(panel.rectTransform);

        // --- Baris 1: chip timer (kiri) & chip paket (kanan) ---
        Image timerChip = HudStyle.AddPanel(panel.transform, "TimerChip", 11f, HudStyle.Chip, Color.clear);
        HudStyle.Place(timerChip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(132f, 40f));
        Image clockIcon = HudStyle.AddImage(timerChip.transform, "ClockIcon", HudStyle.Icon("clock"), HudStyle.TextSecondary, false);
        HudStyle.Place(clockIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(18f, 18f));

        timerText = HudStyle.AddLabel(timerChip.transform, "TimerText", "06:00", 24, Color.white, TextAnchor.MiddleLeft, mainFont);
        HudStyle.Place(timerText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(90f, 34f));

        Image pkgChip = HudStyle.AddPanel(panel.transform, "PackageChip", 11f, HudStyle.Chip, Color.clear);
        HudStyle.Place(pkgChip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(112f, 40f));
        Image pkgIcon = HudStyle.AddImage(pkgChip.transform, "PackageIcon", HudStyle.Icon("package"), HudStyle.Accent, false);
        HudStyle.Place(pkgIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(18f, 18f));

        packageProgressText = HudStyle.AddLabel(pkgChip.transform, "PackageCounterText", "0 / 5", 18, HudStyle.Accent, TextAnchor.MiddleLeft, mainFont);
        HudStyle.Place(packageProgressText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(68f, 34f));

        // --- Baris 2: progres paket (segmen) ---
        packageSegmentsRoot = HudStyle.NewRect("PackageSegments", panel.transform);
        HudStyle.Place(packageSegmentsRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -66f), new Vector2(W - 32f, 6f));
        HorizontalLayoutGroup segLayout = packageSegmentsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        segLayout.spacing = 5f;
        segLayout.childControlWidth = true;
        segLayout.childControlHeight = true;
        segLayout.childForceExpandWidth = true;
        segLayout.childForceExpandHeight = true;
        RebuildPackageSegments(5, 0);

        // --- Blok misi aktif ---
        Image questBar = HudStyle.AddImage(panel.transform, "MissionBar", HudStyle.Rounded(1.5f), HudStyle.Accent);
        HudStyle.Place(questBar.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -88f), new Vector2(3f, 82f));

        Text goalLabel = HudStyle.AddLabel(panel.transform, "MissionLabel", "TUJUAN SAAT INI", 10, HudStyle.Accent, TextAnchor.MiddleLeft, mainFont);
        HudStyle.Place(goalLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -86f), new Vector2(300f, 14f));

        missionTitleText = HudStyle.AddLabel(panel.transform, "MissionTitleText", "Temui Pak Beni di Pos Pengiriman", 16, HudStyle.TextPrimary, TextAnchor.UpperLeft, mainFont, FontStyle.Bold, true);
        HudStyle.Place(missionTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -102f), new Vector2(356f, 22f));

        missionClueText = HudStyle.AddLabel(panel.transform, "MissionClueText",
            "Ambil 5 paket terakhir dari Pak Beni di dekat mobil pengiriman dan dengarkan instruksi batas waktu.",
            12, HudStyle.TextSecondary, TextAnchor.UpperLeft, mainFont, FontStyle.Normal, true);
        missionClueText.lineSpacing = 1.12f;
        HudStyle.Place(missionClueText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -126f), new Vector2(356f, 48f));

        // --- Footer: petunjuk peta ---
        RectTransform mapKey = HudStyle.AddKeycap(panel.transform, "MapKeycap", "M", new Vector2(24f, 24f), mainFont, 12);
        HudStyle.Place(mapKey, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 12f), new Vector2(24f, 24f));
        Text mapHintText = HudStyle.AddLabel(panel.transform, "MapHintText", "Denah Kota", 11, HudStyle.TextSecondary, TextAnchor.MiddleLeft, mainFont);
        HudStyle.Place(mapHintText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 12f), new Vector2(200f, 24f));

        // --- Notifikasi penalti (pill merah di tengah atas) ---
        penaltyFlashRoot = new GameObject("PenaltyFlash", typeof(RectTransform));
        penaltyFlashRoot.transform.SetParent(transform, false);
        RectTransform penRT = (RectTransform)penaltyFlashRoot.transform;
        HudStyle.Place(penRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(200f, 44f));
        Image penBg = penaltyFlashRoot.AddComponent<Image>();
        penBg.sprite = HudStyle.Rounded(14f);
        penBg.type = Image.Type.Sliced;
        penBg.color = new Color(0.22f, 0.05f, 0.07f, 0.95f);
        penBg.raycastTarget = false;
        Image penStroke = HudStyle.AddImage(penRT, "Stroke", HudStyle.RoundedStroke(14f, 1.2f), new Color(HudStyle.Danger.r, HudStyle.Danger.g, HudStyle.Danger.b, 0.7f));
        HudStyle.Stretch(penStroke.rectTransform);
        penStroke.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        HorizontalLayoutGroup penLayout = penaltyFlashRoot.AddComponent<HorizontalLayoutGroup>();
        penLayout.padding = new RectOffset(22, 22, 10, 10);
        penLayout.childAlignment = TextAnchor.MiddleCenter;
        penLayout.childControlWidth = true;
        penLayout.childControlHeight = true;
        penLayout.childForceExpandWidth = false;
        penLayout.childForceExpandHeight = false;
        ContentSizeFitter penFit = penaltyFlashRoot.AddComponent<ContentSizeFitter>();
        penFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        penFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        penaltyFlashText = HudStyle.AddLabel(penRT, "PenaltyFlashText", "", 18, new Color(1f, 0.55f, 0.55f, 1f), TextAnchor.MiddleCenter, mainFont);
        penaltyFlashRoot.SetActive(false);

        // --- Pemberitahuan tabrakan ("Anda menabrak ...") di bawah notifikasi penalti ---
        bumpNoticeRoot = new GameObject("BumpNotice", typeof(RectTransform));
        bumpNoticeRoot.transform.SetParent(transform, false);
        RectTransform bumpRT = (RectTransform)bumpNoticeRoot.transform;
        HudStyle.Place(bumpRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(200f, 38f));
        Image bumpBg = bumpNoticeRoot.AddComponent<Image>();
        bumpBg.sprite = HudStyle.Rounded(14f);
        bumpBg.type = Image.Type.Sliced;
        bumpBg.color = new Color(0.05f, 0.066f, 0.098f, 0.94f);
        bumpBg.raycastTarget = false;
        Image bumpStroke = HudStyle.AddImage(bumpRT, "Stroke", HudStyle.RoundedStroke(14f, 1.2f),
            new Color(HudStyle.Warning.r, HudStyle.Warning.g, HudStyle.Warning.b, 0.6f));
        HudStyle.Stretch(bumpStroke.rectTransform);
        bumpStroke.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        HorizontalLayoutGroup bumpLayout = bumpNoticeRoot.AddComponent<HorizontalLayoutGroup>();
        bumpLayout.padding = new RectOffset(16, 18, 8, 8);
        bumpLayout.spacing = 10f;
        bumpLayout.childAlignment = TextAnchor.MiddleCenter;
        bumpLayout.childControlWidth = true;
        bumpLayout.childControlHeight = true;
        bumpLayout.childForceExpandWidth = false;
        bumpLayout.childForceExpandHeight = false;
        ContentSizeFitter bumpFit = bumpNoticeRoot.AddComponent<ContentSizeFitter>();
        bumpFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        bumpFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image bumpDot = HudStyle.AddImage(bumpRT, "Dot", HudStyle.Circle(), HudStyle.Warning, false);
        LayoutElement bumpDotLE = bumpDot.gameObject.AddComponent<LayoutElement>();
        bumpDotLE.preferredWidth = 9f;
        bumpDotLE.preferredHeight = 9f;
        bumpNoticeText = HudStyle.AddLabel(bumpRT, "BumpNoticeText", "", 15, HudStyle.TextPrimary, TextAnchor.MiddleCenter, mainFont);
        bumpNoticeRoot.SetActive(false);
    }

    private void RebuildPackageSegments(int total, int delivered)
    {
        if (packageSegmentsRoot == null) return;
        total = Mathf.Max(1, total);

        if (packageSegments.Count != total)
        {
            foreach (Image s in packageSegments)
            {
                if (s != null)
                {
                    if (Application.isPlaying) Destroy(s.gameObject); else DestroyImmediate(s.gameObject);
                }
            }
            packageSegments.Clear();
            for (int i = 0; i < total; i++)
            {
                Image seg = HudStyle.AddImage(packageSegmentsRoot, "Segment_" + i, HudStyle.Rounded(3f), HudStyle.Chip);
                packageSegments.Add(seg);
            }
        }

        for (int i = 0; i < packageSegments.Count; i++)
        {
            packageSegments[i].color = i < delivered ? HudStyle.Accent : new Color(1f, 1f, 1f, 0.13f);
        }
    }

    private void BuildInteractPrompt()
    {
        interactPromptObj = new GameObject("InteractPromptBox", typeof(RectTransform));
        interactPromptObj.transform.SetParent(transform, false);

        RectTransform ipRT = (RectTransform)interactPromptObj.transform;
        HudStyle.Place(ipRT, new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 56f));

        Image ipBg = interactPromptObj.AddComponent<Image>();
        ipBg.sprite = HudStyle.Rounded(18f);
        ipBg.type = Image.Type.Sliced;
        ipBg.color = new Color(0.05f, 0.066f, 0.098f, 0.93f);

        Image ipStroke = HudStyle.AddImage(ipRT, "Stroke", HudStyle.RoundedStroke(18f, 1.2f), new Color(HudStyle.Accent.r, HudStyle.Accent.g, HudStyle.Accent.b, 0.5f));
        HudStyle.Stretch(ipStroke.rectTransform);
        ipStroke.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        Shadow ipShadow = interactPromptObj.AddComponent<Shadow>();
        ipShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        ipShadow.effectDistance = new Vector2(0f, -4f);

        Button ipBtn = interactPromptObj.AddComponent<Button>();
        ipBtn.targetGraphic = ipBg;
        ColorBlock ipCb = ipBtn.colors;
        ipCb.normalColor = Color.white;
        ipCb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        ipCb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        ipCb.selectedColor = Color.white;
        ipBtn.colors = ipCb;
        ipBtn.onClick.AddListener(() =>
        {
            GameAudioManager.Instance?.PlayUIClick();
            currentInteractCallback?.Invoke();
        });

        HorizontalLayoutGroup ipLayout = interactPromptObj.AddComponent<HorizontalLayoutGroup>();
        ipLayout.padding = new RectOffset(10, 22, 8, 8);
        ipLayout.spacing = 14f;
        ipLayout.childAlignment = TextAnchor.MiddleLeft;
        ipLayout.childControlWidth = true;
        ipLayout.childControlHeight = true;
        ipLayout.childForceExpandWidth = false;
        ipLayout.childForceExpandHeight = false;

        ContentSizeFitter ipFit = interactPromptObj.AddComponent<ContentSizeFitter>();
        ipFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ipFit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        RectTransform keycap = HudStyle.AddKeycap(ipRT, "KeyCap", "E", new Vector2(36f, 38f), mainFont, 19);
        LayoutElement keyLE = keycap.gameObject.AddComponent<LayoutElement>();
        keyLE.preferredWidth = 36f;
        keyLE.preferredHeight = 38f;

        interactPromptText = HudStyle.AddLabel(ipRT, "PromptText", "Bicara", 17, Color.white, TextAnchor.MiddleLeft, mainFont);

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
        bg.color = new Color(HudStyle.Ink.r, HudStyle.Ink.g, HudStyle.Ink.b, 0.90f);

        // Container Peta di Tengah
        GameObject cardGo = new GameObject("MapCard");
        cardGo.transform.SetParent(mapModalObj.transform, false);
        RectTransform cardRT = cardGo.AddComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.5f, 0.5f);
        cardRT.anchorMax = new Vector2(0.5f, 0.5f);
        cardRT.pivot = new Vector2(0.5f, 0.5f);
        cardRT.sizeDelta = new Vector2(680f, 520f);

        Image cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = HudStyle.Rounded(20f);
        cardImg.type = Image.Type.Sliced;
        cardImg.color = HudStyle.PanelSolid;
        Image cardStroke = HudStyle.AddImage(cardRT, "Stroke", HudStyle.RoundedStroke(20f, 1f), new Color(1f, 1f, 1f, 0.09f));
        HudStyle.Stretch(cardStroke.rectTransform);
        Shadow cardShadow = cardGo.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        cardShadow.effectDistance = new Vector2(0f, -8f);

        // Header Title
        GameObject titleGo = new GameObject("MapTitle");
        titleGo.transform.SetParent(cardGo.transform, false);
        RectTransform titleRT = titleGo.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 1f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -24f);
        titleRT.sizeDelta = new Vector2(0f, 36f);

        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2";

        Text titleTxt = titleGo.AddComponent<Text>();
        titleTxt.font = mainFont;
        titleTxt.fontSize = 22;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = HudStyle.TextPrimary;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.text = isAirportLevel2
            ? "DENAH AREA BANDARA — LEVEL 2"
            : (isLevel2 ? "DENAH KOTA — LEVEL 2 (RUTE & TATA LETAK BARU)" : "DENAH KAWASAN KOTA — KURIR TERAKHIR");

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
        layoutTxt.color = HudStyle.TextPrimary;
        layoutTxt.supportRichText = false;
        layoutTxt.lineSpacing = 1.35f;
        layoutTxt.alignment = TextAnchor.UpperLeft;
        layoutTxt.text = isAirportLevel2
            ? "ALUR PENGANTARAN PAKET AREA BANDARA:\n\n" +
              "1. PAK BENI 2 (Pusat Pengiriman):\n" +
              "   • Temui Pak Beni 2 untuk mengambil daftar instruksi 5 paket bandara.\n\n" +
              "2. NAYA 2 (Paket 1 - Dokumen Penerbangan):\n" +
              "   • Penerima paket pertama di area terminal bandara.\n\n" +
              "3. DIMAS 2 (Paket 2 - Suku Cadang Bagasi):\n" +
              "   • Teknisi penanganan bagasi penerima paket kedua.\n\n" +
              "4. TUKANG PARKIR 2 (Paket 3 - Rompi & Bendera Apron):\n" +
              "   • Petugas parkir apron bandara penerima paket ketiga.\n\n" +
              "5. PAK JOKO 2 (Paket 4 - Logistik Kargo Bandara):\n" +
              "   • Penanggung jawab kargo bandara penerima paket keempat.\n\n" +
              "6. CLARA 2 (Paket 5 - Cinderamata Penerbangan):\n" +
              "   • Penumpang/pengunjung penerima paket cinderamata kelima.\n\n" +
              "7. KEMBALI KE PAK BENI 2:\n" +
              "   • Lapor seluruh tugas selesai untuk menyelesaikan misi & mengakhiri permainan!"
            : (isLevel2
                ? "AREA UTAMA KOTA LEVEL 2 (TATA LETAK BARU):\n\n" +
                  "1. POS PUSAT PENGIRIMAN (Center Plaza - Depan Mega Mall):\n" +
                  "   • Titik Awal Rian & Pos Pak Beni (Misi Awal & Akhir)\n\n" +
                  "2. COFFEE SHOP (Koridor Utara Barat):\n" +
                  "   • Kedai kopi tempat Clara bertugas (Paket 1)\n\n" +
                  "3. DRUG STORE (Koridor Selatan Barat):\n" +
                  "   • Toko obat farmasi cabang milik Pak Joko (Paket 2)\n\n" +
                  "4. FIRE STATION (Koridor Utara Timur):\n" +
                  "   • Pos pemadam kebakaran tempat Dimas bertugas (Paket 3)\n\n" +
                  "5. PERSIMPANGAN TENGAH & PAKET JATUH:\n" +
                  "   • Petugas Parkir (Info) & Paket Jatuh di Gang Tengah dekat Donut Shop (Misi 4)\n\n" +
                  "6. GEDUNG APARTEMEN MERAH (Koridor Selatan Timur):\n" +
                  "   • Gedung apartemen tempat Naya menunggu dokumen (Paket 5)\n\n" +
                  "AWAS: Mobil melaju kencang di Koridor Utara & Selatan! Gunakan gang pejalan kaki."
                : "AREA UTAMA KOTA UNTUK PENGIRIMAN:\n\n" +
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
                  "   • Gedung bertingkat tinggi tempat Naya menunggu dokumen (Paket 5)");

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
        footTxt.color = HudStyle.Accent;
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
            StartLevel2Transition();
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
        nextTxt.text = "Lanjut ke Level 2 (Bandara) ▶";

        nextLevelButton.gameObject.SetActive(false);

        endingModalObj.SetActive(false);
    }

    private void BuildTransitionOverlay()
    {
        transitionOverlayObj = new GameObject("TransitionOverlay", typeof(RectTransform));
        transitionOverlayObj.transform.SetParent(transform, false);

        RectTransform toRT = transitionOverlayObj.GetComponent<RectTransform>();
        toRT.anchorMin = Vector2.zero;
        toRT.anchorMax = Vector2.one;
        toRT.offsetMin = Vector2.zero;
        toRT.offsetMax = Vector2.zero;

        Image bg = transitionOverlayObj.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.05f, 0.08f, 0.98f);

        GameObject centerCard = new GameObject("TransitionCard", typeof(RectTransform));
        centerCard.transform.SetParent(transitionOverlayObj.transform, false);
        RectTransform cRT = centerCard.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0.5f, 0.5f);
        cRT.anchorMax = new Vector2(0.5f, 0.5f);
        cRT.pivot = new Vector2(0.5f, 0.5f);
        cRT.sizeDelta = new Vector2(620f, 260f);

        Image cImg = centerCard.AddComponent<Image>();
        cImg.color = new Color(0.06f, 0.09f, 0.15f, 0.98f);

        Outline cOutline = centerCard.AddComponent<Outline>();
        cOutline.effectColor = new Color(0.18f, 0.65f, 0.40f, 0.7f);
        cOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Header Strip Emerald
        GameObject strip = new GameObject("Strip", typeof(RectTransform));
        strip.transform.SetParent(centerCard.transform, false);
        RectTransform sRT = strip.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 1f);
        sRT.anchorMax = new Vector2(1f, 1f);
        sRT.pivot = new Vector2(0.5f, 1f);
        sRT.anchoredPosition = Vector2.zero;
        sRT.sizeDelta = new Vector2(0f, 4f);
        Image sImg = strip.AddComponent<Image>();
        sImg.color = new Color(0.18f, 0.78f, 0.40f, 1f);

        // Judul Transisi
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform));
        titleGo.transform.SetParent(centerCard.transform, false);
        RectTransform tRT = titleGo.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 1f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -38f);
        tRT.sizeDelta = new Vector2(0f, 46f);

        transitionTitleText = titleGo.AddComponent<Text>();
        transitionTitleText.font = mainFont;
        transitionTitleText.fontSize = 24;
        transitionTitleText.fontStyle = FontStyle.Bold;
        transitionTitleText.color = new Color(0.25f, 0.88f, 0.55f, 1f);
        transitionTitleText.alignment = TextAnchor.MiddleCenter;
        transitionTitleText.text = "✈️ MENUJU BANDARA KOTA...";

        // Subteks Deskripsi
        GameObject subGo = new GameObject("SubText", typeof(RectTransform));
        subGo.transform.SetParent(centerCard.transform, false);
        RectTransform subRT = subGo.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0f, 1f);
        subRT.anchorMax = new Vector2(1f, 1f);
        subRT.pivot = new Vector2(0.5f, 1f);
        subRT.anchoredPosition = new Vector2(0f, -95f);
        subRT.sizeDelta = new Vector2(0f, 48f);

        transitionSubText = subGo.AddComponent<Text>();
        transitionSubText.font = mainFont;
        transitionSubText.fontSize = 15;
        transitionSubText.color = new Color(0.85f, 0.90f, 0.96f, 1f);
        transitionSubText.alignment = TextAnchor.MiddleCenter;
        transitionSubText.text = "Selamat! Seluruh paket di Kota berhasil terkirim tepat waktu.\nMenyiapkan rute tugas pengantaran Level 2 di Bandara...";

        // Status Loading Berjalan
        GameObject statusGo = new GameObject("StatusText", typeof(RectTransform));
        statusGo.transform.SetParent(centerCard.transform, false);
        RectTransform stRT = statusGo.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0f);
        stRT.anchorMax = new Vector2(1f, 0f);
        stRT.pivot = new Vector2(0.5f, 0f);
        stRT.anchoredPosition = new Vector2(0f, 24f);
        stRT.sizeDelta = new Vector2(0f, 30f);

        transitionStatusText = statusGo.AddComponent<Text>();
        transitionStatusText.font = mainFont;
        transitionStatusText.fontSize = 14;
        transitionStatusText.fontStyle = FontStyle.Italic;
        transitionStatusText.color = new Color(0.60f, 0.72f, 0.84f, 1f);
        transitionStatusText.alignment = TextAnchor.MiddleCenter;
        transitionStatusText.text = "Memuat scene Bandara, harap tunggu...";

        transitionOverlayObj.SetActive(false);
    }

    public void StartLevel2Transition()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (nextLevelButton != null) nextLevelButton.interactable = false;
        GameAudioManager.Instance?.PlayUIClick();

        if (transitionOverlayObj != null)
        {
            transitionOverlayObj.transform.SetAsLastSibling();
            transitionOverlayObj.SetActive(true);
        }

        StartCoroutine(TransitionToLevel2Routine());
    }

    private System.Collections.IEnumerator TransitionToLevel2Routine()
    {
        Time.timeScale = 1f;

        // Animasikan status loading selama ~1 detik
        float elapsed = 0f;
        string[] dots = new string[] { ".", "..", "..." };
        int dotIdx = 0;

        while (elapsed < 1.0f)
        {
            yield return new WaitForSecondsRealtime(0.25f);
            elapsed += 0.25f;
            dotIdx = (dotIdx + 1) % dots.Length;
            if (transitionStatusText != null)
            {
                transitionStatusText.text = "Memuat scene Bandara" + dots[dotIdx];
            }
        }

        string targetScene = "Assets/Scenes/Level 2 saya/Level 2 saya.unity";
        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            targetScene = "Level 2 saya";
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
    }

    private void BuildMainMenu()
    {
        mainMenuObj = new GameObject("MainMenuOverlay", typeof(RectTransform), typeof(CanvasGroup));
        mainMenuObj.transform.SetParent(transform, false);
        RectTransform mmRT = (RectTransform)mainMenuObj.transform;
        HudStyle.Stretch(mmRT);

        // Latar: redam adegan 3D, gradasi gelap dari kiri, dan bayangan bawah
        Color ink = HudStyle.Ink;
        Image dim = HudStyle.AddImage(mmRT, "Dim", null, new Color(ink.r, ink.g, ink.b, 0.45f), false, true);
        HudStyle.Stretch(dim.rectTransform);
        Image fadeLeft = HudStyle.AddImage(mmRT, "FadeLeft", HudStyle.FadeLeftToRight(), new Color(ink.r, ink.g, ink.b, 0.97f), false);
        HudStyle.Stretch(fadeLeft.rectTransform);
        Image fadeBottom = HudStyle.AddImage(mmRT, "FadeBottom", HudStyle.FadeBottomToTop(), new Color(ink.r, ink.g, ink.b, 0.85f), false);
        fadeBottom.rectTransform.anchorMin = new Vector2(0f, 0f);
        fadeBottom.rectTransform.anchorMax = new Vector2(1f, 0.38f);
        fadeBottom.rectTransform.offsetMin = Vector2.zero;
        fadeBottom.rectTransform.offsetMax = Vector2.zero;

        RectTransform content = HudStyle.NewRect("MenuContainer", mmRT);
        HudStyle.Stretch(content);

        Vector2 TL = new Vector2(0f, 1f);

        // Chip eyebrow
        Color acc = HudStyle.Accent;
        Image chip = HudStyle.AddPanel(content, "EyebrowChip", 14f, new Color(acc.r, acc.g, acc.b, 0.12f), new Color(acc.r, acc.g, acc.b, 0.38f));
        HudStyle.Place(chip.rectTransform, TL, TL, new Vector2(96f, -76f), new Vector2(344f, 28f));
        Image dot = HudStyle.AddImage(chip.transform, "Dot", HudStyle.Circle(), acc, false);
        HudStyle.Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(13f, 0f), new Vector2(8f, 8f));
        Text eye = HudStyle.AddLabel(chip.transform, "EyebrowText", "KOTA WONDERLAND  •  SIMULASI PENGANTARAN", 11, acc, TextAnchor.MiddleLeft, mainFont);
        HudStyle.Stretch(eye.rectTransform, 30f, 0f, 8f, 0f);

        // Judul besar
        Text title = HudStyle.AddLabel(content, "TitleText", "KURIR\n<color=#FFB020>TERAKHIR</color>", 92, HudStyle.TextPrimary, TextAnchor.UpperLeft, mainFont);
        title.lineSpacing = 0.86f;
        HudStyle.Place(title.rectTransform, TL, TL, new Vector2(92f, -118f), new Vector2(640f, 200f));
        Shadow titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        titleShadow.effectDistance = new Vector2(0f, -4f);

        Text sub = HudStyle.AddLabel(content, "SubtitleText", "Antarkan Seluruh Paket Sebelum Batas Waktu Habis", 18, HudStyle.TextSecondary, TextAnchor.UpperLeft, mainFont, FontStyle.Normal, true);
        HudStyle.Place(sub.rectTransform, TL, TL, new Vector2(96f, -346f), new Vector2(480f, 28f));

        // Tombol
        Button play = HudStyle.AddButton(content, "Btn_Play", "Mulai Permainan", HudStyle.ButtonKind.Primary, new Vector2(330f, 58f), mainFont, 19, () =>
        {
            GameAudioManager.Instance?.PlayUIClick();
            bool isAirport = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
            if (!isAirport && !KurirIntroCutscene.HasPlayedThisSession)
            {
                // Putar cutscene pembuka 15 detik dulu, gameplay dimulai setelah selesai / dilewati
                isMainMenuOpen = false;
                if (mainMenuObj != null) mainMenuObj.SetActive(false);
                if (howToPlayModalObj != null) howToPlayModalObj.SetActive(false);
                SetDashboardVisible(false);
                SetRadarVisible(false);
                KurirIntroCutscene.Play(() =>
                {
                    HideMainMenu();
                    if (DeliveryGameManager.Instance != null)
                    {
                        DeliveryGameManager.Instance.StartGame();
                    }
                });
                return;
            }

            HideMainMenu();
            if (DeliveryGameManager.Instance != null)
            {
                DeliveryGameManager.Instance.StartGame();
            }
        }, true);
        HudStyle.Place((RectTransform)play.transform, TL, TL, new Vector2(96f, -414f), new Vector2(330f, 58f));

        Button how = HudStyle.AddButton(content, "Btn_HowToPlay", "Cara Bermain", HudStyle.ButtonKind.Secondary, new Vector2(330f, 52f), mainFont, 16, () =>
        {
            GameAudioManager.Instance?.PlayUIClick();
            ShowHowToPlay(true);
        });
        HudStyle.Place((RectTransform)how.transform, TL, TL, new Vector2(96f, -486f), new Vector2(330f, 52f));

        Button quit = HudStyle.AddButton(content, "Btn_Quit", "Keluar", HudStyle.ButtonKind.Ghost, new Vector2(330f, 46f), mainFont, 15, () =>
        {
            GameAudioManager.Instance?.PlayUIClick();
            QuitGame();
        });
        HudStyle.Place((RectTransform)quit.transform, TL, TL, new Vector2(96f, -552f), new Vector2(330f, 46f));

        // Footer kiri
        Text footer = HudStyle.AddLabel(content, "FooterText", "Final Project  •  Introduction to Game Development", 12, HudStyle.TextMuted, TextAnchor.MiddleLeft, mainFont, FontStyle.Normal);
        HudStyle.Place(footer.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(96f, 30f), new Vector2(520f, 22f));

        // Panel petunjuk kontrol (kanan bawah)
        Image hint = HudStyle.AddPanel(content, "ControlsHint", 16f, HudStyle.Panel, HudStyle.Stroke);
        hint.rectTransform.GetChild(0).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        HudStyle.Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-56f, 44f), new Vector2(400f, 58f));
        HorizontalLayoutGroup hintLayout = hint.gameObject.AddComponent<HorizontalLayoutGroup>();
        hintLayout.padding = new RectOffset(20, 20, 12, 12);
        hintLayout.spacing = 6f;
        hintLayout.childAlignment = TextAnchor.MiddleCenter;
        hintLayout.childControlWidth = true;
        hintLayout.childControlHeight = true;
        hintLayout.childForceExpandWidth = false;
        hintLayout.childForceExpandHeight = false;
        ContentSizeFitter hintFit = hint.gameObject.AddComponent<ContentSizeFitter>();
        hintFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        hintFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (string k in new[] { "W", "A", "S", "D" })
        {
            RectTransform kc = HudStyle.AddKeycap(hint.transform, "Key_" + k, k, new Vector2(28f, 30f), mainFont, 14);
            LayoutElement le = kc.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = 28f; le.preferredHeight = 30f;
        }
        AddHintLabel(hint.transform, "Gerak", 12f);
        RectTransform keyE = HudStyle.AddKeycap(hint.transform, "Key_E", "E", new Vector2(28f, 30f), mainFont, 14);
        LayoutElement leE = keyE.gameObject.AddComponent<LayoutElement>(); leE.preferredWidth = 28f; leE.preferredHeight = 30f;
        AddHintLabel(hint.transform, "Bicara", 12f);
        RectTransform keyM = HudStyle.AddKeycap(hint.transform, "Key_M", "M", new Vector2(28f, 30f), mainFont, 14);
        LayoutElement leM = keyM.gameObject.AddComponent<LayoutElement>(); leM.preferredWidth = 28f; leM.preferredHeight = 30f;
        AddHintLabel(hint.transform, "Peta", 0f);

        // Animasi masuk
        HudFadeIn fade = mainMenuObj.AddComponent<HudFadeIn>();
        fade.slideTarget = content;
        fade.slide = new Vector2(-28f, 0f);
        fade.duration = 0.5f;
        fade.enabled = false;
        fade.enabled = true;
    }

    private void AddHintLabel(Transform parent, string text, float spaceAfter)
    {
        Text t = HudStyle.AddLabel(parent, "Hint_" + text, text, 13, HudStyle.TextSecondary, TextAnchor.MiddleLeft, mainFont);
        t.gameObject.AddComponent<LayoutElement>().minWidth = 0f;
        if (spaceAfter > 0f)
        {
            RectTransform gap = HudStyle.NewRect("Gap", parent);
            LayoutElement ge = gap.gameObject.AddComponent<LayoutElement>();
            ge.preferredWidth = spaceAfter; ge.preferredHeight = 1f;
        }
    }

    private void BuildHowToPlayModal()
    {
        howToPlayModalObj = new GameObject("HowToPlayModal", typeof(RectTransform), typeof(CanvasGroup));
        howToPlayModalObj.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)howToPlayModalObj.transform;
        HudStyle.Stretch(rt);

        Color ink = HudStyle.Ink;
        Image dim = HudStyle.AddImage(rt, "Dim", null, new Color(ink.r, ink.g, ink.b, 0.90f), false, true);
        HudStyle.Stretch(dim.rectTransform);

        const float CW = 800f, CH = 556f;
        Vector2 C = new Vector2(0.5f, 0.5f);
        Vector2 TL = new Vector2(0f, 1f);

        RectTransform card = HudStyle.NewRect("Card", rt);
        HudStyle.Place(card, C, C, Vector2.zero, new Vector2(CW, CH));
        Image cardShadow = HudStyle.AddImage(card, "Shadow", HudStyle.Glow(20f, 26f), new Color(0f, 0f, 0f, 0.55f));
        HudStyle.Stretch(cardShadow.rectTransform, -26f, -34f, -26f, -18f);
        Image cardBg = HudStyle.AddPanel(card, "Panel", 20f, HudStyle.PanelSolid, HudStyle.Stroke);
        HudStyle.Stretch(cardBg.rectTransform);
        Transform cp = cardBg.transform;

        // Header
        Text title = HudStyle.AddLabel(cp, "Title", "Cara Bermain", 28, HudStyle.TextPrimary, TextAnchor.UpperLeft, mainFont);
        HudStyle.Place(title.rectTransform, TL, TL, new Vector2(36f, -28f), new Vector2(500f, 36f));
        Text subtitle = HudStyle.AddLabel(cp, "Subtitle", "Panduan singkat sebelum mulai mengantar paket", 13, HudStyle.TextSecondary, TextAnchor.UpperLeft, mainFont, FontStyle.Normal);
        HudStyle.Place(subtitle.rectTransform, TL, TL, new Vector2(36f, -66f), new Vector2(500f, 20f));
        Image divider = HudStyle.AddImage(cp, "Divider", null, new Color(1f, 1f, 1f, 0.09f), false);
        HudStyle.Place(divider.rectTransform, TL, TL, new Vector2(36f, -102f), new Vector2(CW - 72f, 1f));

        // Kolom kiri: kontrol
        Text ctrlLabel = HudStyle.AddLabel(cp, "ControlsLabel", "KONTROL KARAKTER", 11, HudStyle.Accent, TextAnchor.MiddleLeft, mainFont);
        HudStyle.Place(ctrlLabel.rectTransform, TL, TL, new Vector2(36f, -122f), new Vector2(300f, 16f));

        string[][] controls =
        {
            new[] { "W|A|S|D", "Bergerak melintasi jalan dan trotoar kota" },
            new[] { "Shift",   "Berlari lebih cepat (Sprint)" },
            new[] { "Mouse",   "Mengarahkan kamera pandangan Rian" },
            new[] { "E",       "Berbicara dengan warga, bertanya arah, dan mengambil paket" },
            new[] { "M",       "Membuka denah peta kota & lokasi pertokoan" },
        };
        for (int i = 0; i < controls.Length; i++)
        {
            float y = -152f - i * 54f;
            RectTransform row = HudStyle.NewRect("ControlRow_" + i, cp);
            HudStyle.Place(row, TL, TL, new Vector2(36f, y), new Vector2(350f, 46f));
            string[] keys = controls[i][0].Split('|');
            float x = 0f;
            foreach (string k in keys)
            {
                float w = k.Length > 1 ? 14f + k.Length * 9f : 24f;
                RectTransform kc = HudStyle.AddKeycap(row, "Key_" + k, k, new Vector2(w, 30f), mainFont, 13);
                HudStyle.Place(kc, TL, TL, new Vector2(x, -8f), new Vector2(w, 30f));
                x += w + 3f;
            }
            Text d = HudStyle.AddLabel(row, "Desc", controls[i][1], 12, HudStyle.TextPrimary, TextAnchor.MiddleLeft, mainFont, FontStyle.Normal, true);
            HudStyle.Place(d.rectTransform, TL, TL, new Vector2(122f, 0f), new Vector2(226f, 46f));
        }

        // Kolom kanan: aturan
        Text ruleLabel = HudStyle.AddLabel(cp, "RulesLabel", "ATURAN & MISI", 11, HudStyle.Accent, TextAnchor.MiddleLeft, mainFont);
        HudStyle.Place(ruleLabel.rectTransform, TL, TL, new Vector2(432f, -122f), new Vector2(300f, 16f));

        string[] rules =
        {
            "Ambil 5 paket dari Pak Beni di depan Mall untuk memulai pengantaran.",
            "Antarkan seluruh 5 paket ke toko yang sesuai sebelum batas waktu 6 menit habis.",
            "Hati-hati menyeberang jalan raya! Terserempet mobil mengurangi sisa waktu (-5 dtk).",
            "Salah memberikan paket ke orang yang salah akan terkena penalti waktu (-20 dtk).",
            "Jika ada paket terjatuh di jalan, tanyakan lokasinya ke Petugas Parkir!",
        };
        for (int i = 0; i < rules.Length; i++)
        {
            float y = -152f - i * 62f;
            RectTransform row = HudStyle.NewRect("RuleRow_" + i, cp);
            HudStyle.Place(row, TL, TL, new Vector2(432f, y), new Vector2(332f, 54f));
            Image badge = HudStyle.AddImage(row, "Badge", HudStyle.Circle(), HudStyle.Accent, false);
            HudStyle.Place(badge.rectTransform, TL, TL, new Vector2(0f, -2f), new Vector2(26f, 26f));
            Text n = HudStyle.AddLabel(badge.transform, "Number", (i + 1).ToString(), 14, HudStyle.OnAccent, TextAnchor.MiddleCenter, mainFont);
            HudStyle.Stretch(n.rectTransform);
            Text r = HudStyle.AddLabel(row, "Text", rules[i], 12, HudStyle.TextPrimary, TextAnchor.UpperLeft, mainFont, FontStyle.Normal, true);
            r.lineSpacing = 1.1f;
            HudStyle.Place(r.rectTransform, TL, TL, new Vector2(40f, 0f), new Vector2(292f, 54f));
        }

        // Tombol tutup
        Button close = HudStyle.AddButton(cp, "Btn_Close", "Tutup", HudStyle.ButtonKind.Secondary, new Vector2(180f, 44f), mainFont, 15, () =>
        {
            GameAudioManager.Instance?.PlayUIClick();
            ShowHowToPlay(false);
        });
        HudStyle.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(180f, 44f));

        HudFadeIn fade = howToPlayModalObj.AddComponent<HudFadeIn>();
        fade.duration = 0.2f;
        fade.enabled = false;
        fade.enabled = true;

        howToPlayModalObj.SetActive(false);
    }

    public void ShowHowToPlay(bool show)
    {
        if (howToPlayModalObj != null)
        {
            howToPlayModalObj.SetActive(show);
        }
    }

    private void BuildRadarHUD()
    {
        // Langsung buat DeliveryRadarHUD baru di bawah Canvas ini
        GameObject radarGo = new GameObject("DeliveryRadarHUD", typeof(RectTransform));
        radarGo.transform.SetParent(transform, false);
        radarComponent = radarGo.AddComponent<DeliveryRadarHUD>();
        radarComponent.BuildUI(mainFont);
        radarObj = radarGo;
        radarObj.SetActive(!isMainMenuOpen);
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
        if (radarObj != null) radarObj.SetActive(false);
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
        if (radarObj != null)
        {
            radarObj.SetActive(true);
        }
        if (howToPlayModalObj != null) howToPlayModalObj.SetActive(false);

        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.SetMovementFrozen(false);
        }
    }

    public void SetDashboardVisible(bool visible)
    {
        if (dashboardObj != null) dashboardObj.SetActive(visible);
    }

    public void SetRadarVisible(bool visible)
    {
        if (radarObj != null) radarObj.SetActive(visible);
    }

    public void OnCutsceneFinished()
    {
        isMainMenuOpen = false;
        if (mainMenuObj != null) mainMenuObj.SetActive(false);
        if (dashboardObj != null) dashboardObj.SetActive(true);
        if (radarObj != null) radarObj.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

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
        if (bumpNoticeTimer > 0f)
        {
            bumpNoticeTimer -= Time.unscaledDeltaTime;
            if (bumpNoticeTimer <= 0f && bumpNoticeRoot != null)
            {
                bumpNoticeRoot.SetActive(false);
            }
        }

        if (penaltyFlashTimer > 0f)
        {
            penaltyFlashTimer -= Time.unscaledDeltaTime;
            if (penaltyFlashTimer <= 0f && penaltyFlashText != null)
            {
                penaltyFlashRoot.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        if (isMainMenuOpen || KurirIntroCutscene.IsPlaying)
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
                    interactPromptText.text = currentInteractableText;
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
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

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
            packageProgressText.text = $"{delivered} / {total}";
            RebuildPackageSegments(total, delivered);
        }
    }

    public void UpdateMissionUI(DeliveryMissionState state)
    {
        if (missionTitleText == null || missionClueText == null) return;

        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");

        if (isAirportLevel2)
        {
            switch (state)
            {
                case DeliveryMissionState.Prolog:
                    missionTitleText.text = "Temui Pak Beni 2 di Area Bandara";
                    missionClueText.text = "Bicara dengan Pak Beni 2 untuk mengambil daftar pengantaran 5 paket bandara.";
                    break;
                case DeliveryMissionState.Package1_CoffeeShop:
                    missionTitleText.text = "Antar Paket 1 ke Naya 2";
                    missionClueText.text = "Antarkan paket dokumen penerbangan pertama kepada Naya 2 di area terminal bandara.";
                    break;
                case DeliveryMissionState.Package2_DrugStore:
                    missionTitleText.text = "Antar Paket 2 ke Dimas 2";
                    missionClueText.text = "Antarkan paket suku cadang bagasi kedua kepada Dimas 2.";
                    break;
                case DeliveryMissionState.Package3_HardwareStore:
                    missionTitleText.text = "Antar Paket 3 ke Tukang Parkir 2";
                    missionClueText.text = "Antarkan paket perlengkapan rompi dan bendera pemandu pesawat ke Tukang Parkir 2.";
                    break;
                case DeliveryMissionState.Package4_DroppedPackage:
                    missionTitleText.text = "Antar Paket 4 ke Pak Joko 2";
                    missionClueText.text = "Antarkan paket logistik kargo keempat kepada Pak Joko 2.";
                    break;
                case DeliveryMissionState.Package5_Apartment:
                    missionTitleText.text = "Antar Paket 5 ke Clara 2";
                    missionClueText.text = "Antarkan paket cinderamata terakhir kepada Clara 2.";
                    break;
                case DeliveryMissionState.Final_ReturnToHub:
                    missionTitleText.text = "Lapor Kembali ke Pak Beni 2";
                    missionClueText.text = "Seluruh 5 paket berhasil diantar! Kembali temui Pak Beni 2 untuk menyelesaikan misi!";
                    break;
                case DeliveryMissionState.Completed:
                    missionTitleText.text = "Misi Selesai: Kurir Terpercaya!";
                    missionClueText.text = "Selamat! Semua paket telah sampai dengan selamat tepat waktu.";
                    break;
                case DeliveryMissionState.Failed:
                    missionTitleText.text = "Misi Berakhir: Waktu Habis!";
                    missionClueText.text = "Batas waktu pengiriman di bandara telah habis.";
                    break;
            }
            return;
        }

        bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2";

        if (isLevel2)
        {
            switch (state)
            {
                case DeliveryMissionState.Prolog:
                    missionTitleText.text = "Temui Pak Beni di Pos Pengiriman (Level 2)";
                    missionClueText.text = "Ambil 5 paket baru dari Pak Beni di Center Plaza depan Mall. Perhatikan denah koridor baru [M] dan selesaikan sebelum batas waktu 6 menit habis!";
                    break;
                case DeliveryMissionState.Package1_CoffeeShop:
                    missionTitleText.text = "Clara di Coffee Shop (Koridor Utara Barat)";
                    missionClueText.text = "Label basah: 'C...ra' - Kedai kopi di trotoar Koridor Utara sebelah barat.";
                    break;
                case DeliveryMissionState.Package2_DrugStore:
                    missionTitleText.text = "Pak Joko di Drug Store (Koridor Selatan Barat)";
                    missionClueText.text = "Label basah: 'Pak J...o' - Apotek cabang selatan di trotoar Koridor Selatan sebelah barat.";
                    break;
                case DeliveryMissionState.Package3_HardwareStore:
                    missionTitleText.text = "Dimas di Fire Station (Koridor Utara Timur)";
                    missionClueText.text = "Label basah: 'Di...s' - Pos pemadam kebakaran di trotoar Koridor Utara sebelah timur.";
                    break;
                case DeliveryMissionState.Package4_DroppedPackage:
                    if (DeliveryGameManager.Instance != null && !DeliveryGameManager.Instance.IsDroppedPackageRevealed)
                    {
                        missionTitleText.text = "Tanya Petugas Parkir (Persimpangan Tengah)";
                        missionClueText.text = "Gawat! Satu paket terjatuh di jalan. Tanyakan lokasinya ke Petugas Parkir di persimpangan tengah!";
                    }
                    else
                    {
                        missionTitleText.text = "Ambil Paket yang Terjatuh!";
                        missionClueText.text = "Kotak paket kardus terlihat di jalan aspal gang tengah dekat toko donat. Dekati dan tekan [E] untuk memungutnya!";
                    }
                    break;
                case DeliveryMissionState.Package5_Apartment:
                    missionTitleText.text = "Naya di Apartemen (Koridor Selatan Timur)";
                    missionClueText.text = "Label basah: 'N...a' - Pengiriman dokumen darurat ke gedung apartemen merah di Koridor Selatan sebelah timur.";
                    break;
                case DeliveryMissionState.Final_ReturnToHub:
                    missionTitleText.text = "Kembali ke Pak Beni di Center Plaza!";
                    missionClueText.text = "Seluruh 5 paket Level 2 selesai dikirim! Segera kembali ke Pak Beni di Center Plaza depan Mall sebelum batas waktu berakhir!";
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
                missionTitleText.text = "Temui Pak Beni di Pos Pengiriman";
                missionClueText.text = "Ambil 5 paket terakhir dari Pak Beni di dekat mobil pengiriman dan dengarkan instruksi batas waktu.";
                break;
            case DeliveryMissionState.Package1_CoffeeShop:
                missionTitleText.text = "Clara di Coffee Shop (Area Timur)";
                missionClueText.text = "Label basah: 'C...ra' - Pegawai kedai kopi beraroma harum di trotoar timur sebelum toko tutup.";
                break;
            case DeliveryMissionState.Package2_DrugStore:
                missionTitleText.text = "Pak Joko di Drug Store (Sudut Toko)";
                missionClueText.text = "Label basah: 'Pak J...o' - Pemilik toko obat dan farmasi di sudut deretan pertokoan barat.";
                break;
            case DeliveryMissionState.Package3_HardwareStore:
                missionTitleText.text = "Dimas di Fire Station (Pos Pemadam)";
                missionClueText.text = "Label basah: 'Di...s' - Pegawai pos pemadam kebakaran (Fire Station) di sebelah barat deretan toko.";
                break;
            case DeliveryMissionState.Package4_DroppedPackage:
                if (DeliveryGameManager.Instance != null && !DeliveryGameManager.Instance.IsDroppedPackageRevealed)
                {
                    missionTitleText.text = "Tanya Petugas Parkir (Area Parkir)";
                    missionClueText.text = "Gawat! Satu paket terjatuh di jalan. Dimas menyuruhmu bertanya ke Petugas Parkir untuk mengetahui lokasi persis paketmu!";
                }
                else
                {
                    missionTitleText.text = "Ambil Paket yang Terjatuh!";
                    missionClueText.text = "Kotak paket kardus terlihat di jalan aspal dekat pembatas beton sebelah Fire Station. Dekati dan tekan [E] untuk memungutnya!";
                }
                break;
            case DeliveryMissionState.Package5_Apartment:
                missionTitleText.text = "Naya di Gedung Apartemen";
                missionClueText.text = "Label basah: 'N...a' - Penghuni gedung apartemen bertingkat tinggi di ujung kawasan kota.";
                break;
            case DeliveryMissionState.Final_ReturnToHub:
                missionTitleText.text = "Kembali ke Pak Beni di Pos Pengiriman!";
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

    /// <summary>Pemberitahuan singkat saat Rian menabrak benda, mis. "Anda menabrak mobil".</summary>
    public void ShowBumpNotice(string message)
    {
        if (bumpNoticeRoot == null || bumpNoticeText == null) return;
        bumpNoticeText.text = message;
        bumpNoticeRoot.SetActive(true);
        bumpNoticeTimer = 1.5f;
    }

    public void ShowPenaltyFlash(float secondsDeducted, string message)
    {
        if (penaltyFlashText != null)
        {
            penaltyFlashText.text = message;
            penaltyFlashRoot.SetActive(true);
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

        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2" || isAirportLevel2;

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
                        endingTitleText.text = isAirportLevel2 ? "🏆 TAMAT: KURIR TERPERCAYA BANDARA!" : "🏆 TAMAT: MASTER KURIR KOTA!";
                        endingTitleText.color = new Color(1f, 0.85f, 0.2f, 1f);
                    }
                    if (endingStoryText != null)
                    {
                        endingStoryText.text = isAirportLevel2
                            ? "Semua lima paket di Bandara berhasil diantarkan dengan sempurna kepada Naya 2, Dimas 2, Tukang Parkir 2, Pak Joko 2, dan Clara 2!\n\n" +
                              "Rian telah melapor kembali ke Pak Beni 2 tepat waktu.\n" +
                              "Selamat! Kamu berhasil menyelesaikan misi pengantaran dan menamatkan permainan!"
                            : "Semua lima paket Level 2 berhasil diantarkan dengan sempurna melintasi rute baru!\n\n" +
                              "Rian membuktikan keahlian navigasi terbaiknya dalam menaklukkan tata letak kota yang berubah dan rintangan barikade jalan.\n" +
                              "Selamat! Kamu telah menamatkan seluruh tantangan dan resmi dinobatkan sebagai Master Kurir Kota!";
                    }
                    break;

                case GameEnding.Ending2_PengirimanTerlambat:
                    if (endingTitleText != null)
                    {
                        endingTitleText.text = isAirportLevel2 ? "⚠️ BANDARA: PENGIRIMAN TERLAMBAT" : "⚠️ LEVEL 2: PENGIRIMAN TERLAMBAT";
                        endingTitleText.color = new Color(1f, 0.55f, 0.15f, 1f);
                    }
                    if (endingStoryText != null)
                    {
                        endingStoryText.text = isAirportLevel2
                            ? "Waktu pengiriman di bandara telah habis sebelum semua paket sampai!\n\n" +
                              "Coba perhatikan radar dan kompas untuk navigasi lebih cepat di area bandara!"
                            : "Waktu pengiriman di kota baru telah habis sebelum semua paket sampai!\n\n" +
                              "Barikade jalan dan rute baru membuat perjalanan lebih menantang. " +
                              "Coba lagi untuk menguasai rute kota Level 2!";
                    }
                    break;

                case GameEnding.Ending3_GagalBertugas:
                    if (endingTitleText != null)
                    {
                        endingTitleText.text = isAirportLevel2 ? "❌ BANDARA: GAGAL BERTUGAS" : "❌ LEVEL 2: GAGAL BERTUGAS";
                        endingTitleText.color = new Color(1f, 0.3f, 0.3f, 1f);
                    }
                    if (endingStoryText != null)
                    {
                        endingStoryText.text = isAirportLevel2
                            ? "Waktu habis dan kamu belum berhasil menyelesaikan misi pengiriman bandara.\n\n" +
                              "Gunakan radar dan perhatikan urutan penerima paket untuk menghemat waktu!"
                            : "Waktu habis dan kamu belum berhasil menyelesaikan misi Level 2.\n\n" +
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
                bool isAirport = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
                bool hasStage2 = !isAirport && (
                    Application.CanStreamedLevelBeLoaded("Assets/Scenes/Level 2 saya/Level 2 saya.unity") ||
                    Application.CanStreamedLevelBeLoaded("Level 2 saya") ||
                    Application.CanStreamedLevelBeLoaded("Module 2_Stage2")
                );
                if (nextLevelButton != null)
                {
                    nextLevelButton.gameObject.SetActive(hasStage2);
                    nextLevelButton.interactable = true;
                    if (nextLevelButtonRect != null && hasStage2)
                    {
                        nextLevelButtonRect.anchoredPosition = new Vector2(-225f, 32f);
                        nextLevelButtonRect.sizeDelta = new Vector2(215f, 48f);
                    }
                }
                if (restartButtonRect != null)
                {
                    restartButtonRect.anchoredPosition = hasStage2 ? new Vector2(0f, 32f) : new Vector2(-115f, 32f);
                    restartButtonRect.sizeDelta = hasStage2 ? new Vector2(185f, 48f) : new Vector2(210f, 48f);
                }
                if (mainMenuButtonRect != null)
                {
                    mainMenuButtonRect.anchoredPosition = hasStage2 ? new Vector2(225f, 32f) : new Vector2(115f, 32f);
                    mainMenuButtonRect.sizeDelta = hasStage2 ? new Vector2(185f, 48f) : new Vector2(210f, 48f);
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
                        "Rian menyelesaikan tugas malam di Kota dengan sempurna dan membuktikan dedikasinya.\n" +
                        "Tugas pengantaran berikutnya telah menanti: Level 2 Bandara!\n" +
                        "Klik tombol 'Lanjut ke Level 2 (Bandara)' di bawah untuk memulai tugas pengantaran di bandara.";
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

