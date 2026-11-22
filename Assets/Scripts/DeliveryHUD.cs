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
    private Image thirstFill;
    private Image thirstIcon;
    private Text thirstLabel;
    private PlayerController thirstPlayer;
    private float thirstTrackWidth = 110f;
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
    private Text endingEyebrowText;
    private Image endingDotImage;
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
        HudStyle.Place(dbRT, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(88f, -60f), new Vector2(W, H));
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

        // --- Indikator haus (kanan bawah panel): ikon tetes + bar hidrasi ---
        Image thirstTrack = HudStyle.AddImage(panel.transform, "ThirstTrack", HudStyle.Rounded(3f), new Color(1f, 1f, 1f, 0.10f));
        HudStyle.Place(thirstTrack.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 21f), new Vector2(thirstTrackWidth, 6f));
        thirstFill = HudStyle.AddImage(thirstTrack.transform, "ThirstFill", HudStyle.Rounded(3f), HudStyle.Teal);
        thirstFill.rectTransform.anchorMin = new Vector2(0f, 0f);
        thirstFill.rectTransform.anchorMax = new Vector2(0f, 1f);
        thirstFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        thirstFill.rectTransform.offsetMin = Vector2.zero;
        thirstFill.rectTransform.offsetMax = new Vector2(thirstTrackWidth, 0f);
        thirstIcon = HudStyle.AddImage(panel.transform, "ThirstIcon", HudStyle.Icon("drop"), HudStyle.Teal, false);
        HudStyle.Place(thirstIcon.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f - thirstTrackWidth - 8f, 15f), new Vector2(18f, 18f));
        thirstLabel = HudStyle.AddLabel(panel.transform, "ThirstLabel", "", 11, HudStyle.Danger, TextAnchor.MiddleRight, mainFont);
        HudStyle.Place(thirstLabel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f - thirstTrackWidth - 30f, 14f), new Vector2(90f, 20f));

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

        // BuildUI bisa dipanggil ulang (mis. saat setup di Editor) dan menghancurkan segmen lama:
        // buang referensi yang sudah hancur agar tidak memicu MissingReferenceException.
        packageSegments.RemoveAll(seg => seg == null);

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
        endingModalObj = new GameObject("EndingModal", typeof(RectTransform), typeof(CanvasGroup));
        endingModalObj.transform.SetParent(transform, false);
        RectTransform emRT = (RectTransform)endingModalObj.transform;
        HudStyle.Stretch(emRT);

        Color ink = HudStyle.Ink;
        Image bg = HudStyle.AddImage(emRT, "Dim", null, new Color(ink.r, ink.g, ink.b, 0.92f), false, true);
        HudStyle.Stretch(bg.rectTransform);

        const float CW = 760f, CH = 500f;
        Vector2 C = new Vector2(0.5f, 0.5f);
        Vector2 TC = new Vector2(0.5f, 1f);

        RectTransform card = HudStyle.NewRect("EndingCard", emRT);
        HudStyle.Place(card, C, C, Vector2.zero, new Vector2(CW, CH));
        Image cardShadow = HudStyle.AddImage(card, "Shadow", HudStyle.Glow(22f, 28f), new Color(0f, 0f, 0f, 0.55f));
        HudStyle.Stretch(cardShadow.rectTransform, -28f, -36f, -28f, -20f);
        Image cardBg = HudStyle.AddPanel(card, "Panel", 22f, HudStyle.PanelSolid, HudStyle.Stroke);
        HudStyle.Stretch(cardBg.rectTransform);
        Transform cp = cardBg.transform;

        // Eyebrow: "ENDING 1" dalam pill
        Image chip = HudStyle.AddPanel(cp, "EndingChip", 13f, new Color(1f, 1f, 1f, 0.05f), HudStyle.Stroke);
        HudStyle.Place(chip.rectTransform, TC, TC, new Vector2(0f, -26f), new Vector2(250f, 50f));
        endingDotImage = HudStyle.AddImage(chip.transform, "Dot", HudStyle.Circle(), HudStyle.Accent, false);
        HudStyle.Place(endingDotImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(12f, 12f));
        endingEyebrowText = HudStyle.AddLabel(chip.transform, "EndingEyebrow", "ENDING 1", 26, HudStyle.Accent, TextAnchor.MiddleCenter, mainFont);
        HudStyle.Stretch(endingEyebrowText.rectTransform, 34f, 0f, 14f, 0f);

        // Judul
        endingTitleText = HudStyle.AddLabel(cp, "EndingTitle", "Kurir Terpercaya", 34, HudStyle.TextPrimary, TextAnchor.MiddleCenter, mainFont);
        HudStyle.Place(endingTitleText.rectTransform, TC, TC, new Vector2(0f, -100f), new Vector2(CW - 80f, 50f));
        Shadow titleShadow = endingTitleText.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        titleShadow.effectDistance = new Vector2(0f, -3f);

        // Narasi
        endingStoryText = HudStyle.AddLabel(cp, "EndingStory", "Epilog permainan...", 15, HudStyle.TextSecondary, TextAnchor.UpperCenter, mainFont, FontStyle.Normal, true);
        endingStoryText.lineSpacing = 1.3f;
        HudStyle.Place(endingStoryText.rectTransform, TC, TC, new Vector2(0f, -162f), new Vector2(CW - 120f, 128f));

        // Statistik
        Image statsBg = HudStyle.AddPanel(cp, "EndingStatsPanel", 14f, new Color(1f, 1f, 1f, 0.04f), HudStyle.Stroke);
        HudStyle.Place(statsBg.rectTransform, TC, TC, new Vector2(0f, -318f), new Vector2(CW - 120f, 52f));
        endingStatsText = HudStyle.AddLabel(statsBg.transform, "EndingStats", "Sisa waktu 00:00   Paket 5/5   Salah alamat 0", 14, HudStyle.TextPrimary, TextAnchor.MiddleCenter, mainFont);
        HudStyle.Stretch(endingStatsText.rectTransform, 12f, 0f, 12f, 0f);

        // Tombol: Main Lagi (utama), Menu Utama, Lanjut ke Bandara (hijau)
        Vector2 BC = new Vector2(0.5f, 0f);
        restartButton = HudStyle.AddButton(cp, "RestartButton", "Main Lagi", HudStyle.ButtonKind.Primary, new Vector2(210f, 50f), mainFont, 15, () =>
        {
            if (Time.unscaledTime - endingModalOpenTime < 0.6f) return;
            GameAudioManager.Instance?.PlayUIClick();
            RestartGame();
        });
        restartButtonRect = (RectTransform)restartButton.transform;
        HudStyle.Place(restartButtonRect, BC, BC, new Vector2(-115f, 34f), new Vector2(210f, 50f));

        mainMenuButton = HudStyle.AddButton(cp, "MainMenuButton", "Menu Utama", HudStyle.ButtonKind.Secondary, new Vector2(210f, 50f), mainFont, 15, () =>
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
        mainMenuButtonRect = (RectTransform)mainMenuButton.transform;
        HudStyle.Place(mainMenuButtonRect, BC, BC, new Vector2(115f, 34f), new Vector2(210f, 50f));

        nextLevelButton = HudStyle.AddButton(cp, "NextLevelButton", "Lanjut ke Bandara", HudStyle.ButtonKind.Success, new Vector2(215f, 50f), mainFont, 15, () =>
        {
            if (Time.unscaledTime - endingModalOpenTime < 0.6f) return;
            StartLevel2Transition();
        }, true);
        nextLevelButtonRect = (RectTransform)nextLevelButton.transform;
        HudStyle.Place(nextLevelButtonRect, BC, BC, new Vector2(-225f, 34f), new Vector2(215f, 50f));
        nextLevelButton.gameObject.SetActive(false);

        HudFadeIn fade = endingModalObj.AddComponent<HudFadeIn>();
        fade.duration = 0.35f;
        fade.enabled = false;
        fade.enabled = true;

        endingModalObj.SetActive(false);
    }

    private void BuildTransitionOverlay()
    {
        transitionOverlayObj = new GameObject("TransitionOverlay", typeof(RectTransform), typeof(CanvasGroup));
        transitionOverlayObj.transform.SetParent(transform, false);
        RectTransform toRT = (RectTransform)transitionOverlayObj.transform;
        HudStyle.Stretch(toRT);

        Color ink = HudStyle.Ink;
        Image bg = HudStyle.AddImage(toRT, "Dim", null, new Color(ink.r, ink.g, ink.b, 0.98f), false, true);
        HudStyle.Stretch(bg.rectTransform);

        Vector2 C = new Vector2(0.5f, 0.5f);
        Vector2 TC = new Vector2(0.5f, 1f);
        RectTransform card = HudStyle.NewRect("TransitionCard", toRT);
        HudStyle.Place(card, C, C, Vector2.zero, new Vector2(640f, 290f));
        Image cardShadow = HudStyle.AddImage(card, "Shadow", HudStyle.Glow(22f, 28f), new Color(0f, 0f, 0f, 0.5f));
        HudStyle.Stretch(cardShadow.rectTransform, -28f, -36f, -28f, -20f);
        Image cardBg = HudStyle.AddPanel(card, "Panel", 22f, HudStyle.PanelSolid, HudStyle.Stroke);
        HudStyle.Stretch(cardBg.rectTransform);
        Transform cp = cardBg.transform;

        Color acc = HudStyle.Accent;
        Image chip = HudStyle.AddPanel(cp, "Chip", 13f, new Color(acc.r, acc.g, acc.b, 0.12f), new Color(acc.r, acc.g, acc.b, 0.38f));
        HudStyle.Place(chip.rectTransform, TC, TC, new Vector2(0f, -34f), new Vector2(170f, 26f));
        Image dot = HudStyle.AddImage(chip.transform, "Dot", HudStyle.Circle(), acc, false);
        HudStyle.Place(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(8f, 8f));
        Text chipText = HudStyle.AddLabel(chip.transform, "ChipText", "LEVEL 2  •  BANDARA", 11, acc, TextAnchor.MiddleCenter, mainFont);
        HudStyle.Stretch(chipText.rectTransform, 22f, 0f, 8f, 0f);

        transitionTitleText = HudStyle.AddLabel(cp, "TitleText", "Menuju Bandara Wonderland", 30, HudStyle.TextPrimary, TextAnchor.MiddleCenter, mainFont);
        HudStyle.Place(transitionTitleText.rectTransform, TC, TC, new Vector2(0f, -82f), new Vector2(580f, 46f));

        transitionSubText = HudStyle.AddLabel(cp, "SubText", "Semua paket kota sudah terkirim tepat waktu.\nRute pengantaran di bandara sedang disiapkan.",
            15, HudStyle.TextSecondary, TextAnchor.UpperCenter, mainFont, FontStyle.Normal, true);
        transitionSubText.lineSpacing = 1.3f;
        HudStyle.Place(transitionSubText.rectTransform, TC, TC, new Vector2(0f, -136f), new Vector2(560f, 56f));

        // Bar pemuatan (indeterminate)
        Image track = HudStyle.AddImage(cp, "LoadingTrack", HudStyle.Rounded(3f), new Color(1f, 1f, 1f, 0.07f));
        HudStyle.Place(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 74f), new Vector2(360f, 6f));
        track.gameObject.AddComponent<RectMask2D>();
        Image runner = HudStyle.AddImage(track.transform, "LoadingRunner", HudStyle.Rounded(3f), HudStyle.Accent);
        HudStyle.Place(runner.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(110f, 6f));
        HudLoadingBar bar = runner.gameObject.AddComponent<HudLoadingBar>();
        bar.track = track.rectTransform;

        transitionStatusText = HudStyle.AddLabel(cp, "StatusText", "Memuat area bandara...", 13, HudStyle.TextMuted, TextAnchor.MiddleCenter, mainFont, FontStyle.Normal);
        HudStyle.Place(transitionStatusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(500f, 24f));

        transitionOverlayObj.SetActive(false);
    }

    public void StartLevel2Transition()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (nextLevelButton != null) nextLevelButton.interactable = false;
        GameAudioManager.Instance?.PlayUIClick();
        GameAudioManager.Instance?.PlayWhoosh();

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
                transitionStatusText.text = "Memuat area bandara" + dots[dotIdx];
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

        UpdateThirstUI();

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

    private void UpdateThirstUI()
    {
        if (thirstFill == null) return;
        if (thirstPlayer == null) thirstPlayer = Object.FindAnyObjectByType<PlayerController>();
        if (thirstPlayer == null) return;

        float h = thirstPlayer.Hydration01;
        thirstFill.rectTransform.offsetMax = new Vector2(h * thirstTrackWidth, 0f);

        Color c = h > 0.4f ? HudStyle.Teal : (h > 0.15f ? HudStyle.Warning : HudStyle.Danger);
        if (thirstPlayer.IsExhausted)
        {
            // berkedip saat benar-benar kehausan
            float blink = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
            c = new Color(HudStyle.Danger.r, HudStyle.Danger.g, HudStyle.Danger.b, blink);
        }
        thirstFill.color = c;
        thirstIcon.color = c;
        thirstLabel.text = thirstPlayer.IsExhausted ? "KEHAUSAN" : (thirstPlayer.IsThirsty ? "HAUS" : "");
        thirstLabel.color = c;
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

    private void SetEndingContent(string eyebrow, string title, Color color, string story)
    {
        if (endingEyebrowText != null) { endingEyebrowText.text = eyebrow; endingEyebrowText.color = color; }
        if (endingDotImage != null) endingDotImage.color = color;
        if (endingTitleText != null) { endingTitleText.text = title; endingTitleText.color = color; }
        if (endingStoryText != null) endingStoryText.text = story;
    }

    private void LayoutEndingButtons(bool withNextLevel)
    {
        if (withNextLevel)
        {
            if (nextLevelButtonRect != null)
            {
                nextLevelButtonRect.anchoredPosition = new Vector2(-225f, 34f);
                nextLevelButtonRect.sizeDelta = new Vector2(215f, 50f);
            }
            if (restartButtonRect != null)
            {
                restartButtonRect.anchoredPosition = new Vector2(0f, 34f);
                restartButtonRect.sizeDelta = new Vector2(185f, 50f);
            }
            if (mainMenuButtonRect != null)
            {
                mainMenuButtonRect.anchoredPosition = new Vector2(225f, 34f);
                mainMenuButtonRect.sizeDelta = new Vector2(185f, 50f);
            }
        }
        else
        {
            if (restartButtonRect != null)
            {
                restartButtonRect.anchoredPosition = new Vector2(-115f, 34f);
                restartButtonRect.sizeDelta = new Vector2(210f, 50f);
            }
            if (mainMenuButtonRect != null)
            {
                mainMenuButtonRect.anchoredPosition = new Vector2(115f, 34f);
                mainMenuButtonRect.sizeDelta = new Vector2(210f, 50f);
            }
        }
    }

    public void ShowEndingModal(GameEnding ending)
    {
        if (endingModalObj == null) return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        endingModalOpenTime = Time.unscaledTime;

        endingModalObj.SetActive(true);
        GameAudioManager.Instance?.PlayEnding(ending == GameEnding.Ending1_KurirTerpercaya);

        float timeLeft = DeliveryGameManager.Instance != null ? DeliveryGameManager.Instance.TimeRemaining : 0f;
        int delivered = DeliveryGameManager.Instance != null ? DeliveryGameManager.Instance.PackagesDelivered : 0;
        int wrong = DeliveryGameManager.Instance != null ? DeliveryGameManager.Instance.WrongDeliveryCount : 0;

        int min = Mathf.FloorToInt(timeLeft / 60f);
        int sec = Mathf.FloorToInt(timeLeft % 60f);

        if (endingStatsText != null)
        {
            endingStatsText.text =
                $"<color=#727E92>Sisa waktu</color>  <b>{min:00}:{sec:00}</b>        " +
                $"<color=#727E92>Paket terkirim</color>  <b>{delivered}/5</b>        " +
                $"<color=#727E92>Salah alamat</color>  <b>{wrong}x</b>";
        }

        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        bool isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Module 2_Stage2" || isAirportLevel2;

        Color gold = HudStyle.Accent;
        Color orange = new Color32(0xFF, 0x8A, 0x3D, 255);
        Color red = HudStyle.Danger;

        if (isLevel2)
        {
            // Level 2: dua tombol (Main Lagi & Menu Utama)
            if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
            LayoutEndingButtons(false);

            switch (ending)
            {
                case GameEnding.Ending1_KurirTerpercaya:
                    SetEndingContent("TAMAT", isAirportLevel2 ? "Kurir Terpercaya Bandara" : "Master Kurir Kota", gold,
                        isAirportLevel2
                            ? "Kelima paket bandara sudah sampai ke Naya, Dimas, Tukang Parkir, Pak Joko, dan Clara.\n\n" +
                              "Rian lalu melapor ke Pak Beni tepat waktu. Kepercayaan itu terbayar: " +
                              "ia resmi jadi kurir tetap, dan ceritanya berakhir di sini."
                            : "Kelima paket berhasil diantar lewat rute baru, melewati barikade dan jalanan yang berubah.\n\n" +
                              "Rian membuktikan bahwa ia bisa membaca kota dengan cepat. Gelar Master Kurir Kota jadi miliknya.");
                    break;

                case GameEnding.Ending2_PengirimanTerlambat:
                    SetEndingContent("HAMPIR BERHASIL", isAirportLevel2 ? "Terlambat di Bandara" : "Terlambat di Level 2", orange,
                        isAirportLevel2
                            ? "Waktu habis sebelum semua paket tiba di tujuan.\n\n" +
                              "Radar di pojok layar menunjukkan arah penerima berikutnya. Pakai itu untuk memangkas jarak di percobaan selanjutnya."
                            : "Waktu habis sebelum semua paket tiba.\n\n" +
                              "Barikade dan rute baru membuat perjalanan lebih panjang. Lihat peta dengan tombol [M], lalu coba lagi.");
                    break;

                default:
                    SetEndingContent("BELUM BERHASIL", isAirportLevel2 ? "Misi Bandara Gagal" : "Misi Level 2 Gagal", red,
                        isAirportLevel2
                            ? "Waktu habis dan masih banyak paket yang belum terkirim.\n\n" +
                              "Ikuti urutan penerima di panel tujuan dan percayakan arah pada radar. Kamu pasti bisa lebih cepat."
                            : "Waktu habis dan misi belum selesai.\n\n" +
                              "Hindari barikade jalan dan cek peta [M] lebih sering supaya tidak buang waktu.");
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
                }
                LayoutEndingButtons(hasStage2);

                SetEndingContent("ENDING 1", "Kurir Terpercaya", gold,
                    "Lima paket terakhir sudah sampai ke tangan yang tepat sebelum waktu habis.\n\n" +
                    "Malam ini Rian membuktikan bahwa ia layak jadi kurir tetap. " +
                    "Tapi tugasnya belum selesai: bandara kota sedang menunggu paket berikutnya.");
            }
            else
            {
                // Kalah di Level 1: dua tombol (Main Lagi & Menu Utama)
                if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(false);
                LayoutEndingButtons(false);

                if (ending == GameEnding.Ending2_PengirimanTerlambat)
                {
                    SetEndingContent("ENDING 2", "Pengiriman Terlambat", orange,
                        "Waktu habis sebelum semua paket sampai.\n\n" +
                        "Sebagian besar paket sudah tiba di tujuan, tapi keterlambatan ini jadi pelajaran. " +
                        "Hafalkan rute kota dan atur waktu lebih baik di percobaan berikutnya.");
                }
                else
                {
                    SetEndingContent("ENDING 3", "Tugas Belum Selesai", red,
                        "Waktu habis dan sebagian besar toko sudah menutup pintunya.\n\n" +
                        "Paket yang tersisa dikembalikan ke pusat pengiriman. " +
                        "Pelajari lagi jalanan kota, lalu coba antar semuanya tepat waktu.");
                }
            }
        }
    }
}

