using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DeliveryRadarHUD: Radar kompas HUD dinamis untuk navigasi pengantaran paket (Module 2).
/// Mendukung interaksi klik:
/// - Klik piringan radar untuk membuka Peta Kota [M].
/// - Klik badge jarak bawah untuk mengganti zoom jangkauan radar (35m -> 60m -> 100m).
/// - Klik tombol [-]/[+] di header untuk minimize / maximize radar.
/// </summary>
public class DeliveryRadarHUD : MonoBehaviour
{
    private static DeliveryRadarHUD instance;
    public static DeliveryRadarHUD Instance => instance;

    [Header("=== Radar Settings ===")]
    public float radarRadius = 62f;
    public float radarWorldRange = 60f;
    public bool isVisible = true;
    public bool isMinimized = false;

    private RectTransform rootRect;
    private GameObject discGo;
    private GameObject distGo;
    private RectTransform playerPointerRect;
    private Text targetDistanceText;
    private Text headerText;
    private Image minIconImg;
    private GameObject discGlowGo;
    private static readonly Vector2 ExpandedSize = new Vector2(200f, 206f);
    private static readonly Vector2 CollapsedSize = new Vector2(200f, 24f);
    private Text clickHintText;
    private Image outerRingImg;

    private readonly List<RadarBlip> blipPool = new List<RadarBlip>();
    private Transform playerTransform;

    private static Sprite circleSprite;
    private static Sprite ringSprite;
    private static Sprite arrowSprite;

    private class RadarBlip
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image image;
        public Outline outline;
    }

    public static DeliveryRadarHUD Ensure(Transform parentCanvas, Font font)
    {
        if (parentCanvas == null)
        {
            DeliveryHUD hud = DeliveryHUD.Instance;
            if (hud != null) parentCanvas = hud.transform;
        }

        if (parentCanvas != null)
        {
            foreach (Transform t in parentCanvas)
            {
                DeliveryRadarHUD child = t.GetComponent<DeliveryRadarHUD>();
                if (child != null && child.playerPointerRect != null && child.discGo != null)
                {
                    child.gameObject.SetActive(true);
                    return child;
                }
            }
        }

        // Buat baru jika belum ada
        GameObject radarGo = new GameObject("DeliveryRadarHUD", typeof(RectTransform));
        if (parentCanvas != null)
        {
            radarGo.transform.SetParent(parentCanvas, false);
        }
        DeliveryRadarHUD comp = radarGo.AddComponent<DeliveryRadarHUD>();
        comp.BuildUI(font);
        radarGo.SetActive(true);
        return comp;
    }

    private void Awake()
    {
        instance = this;
        GenerateSprites();
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
        }
        else
        {
            GameObject pGo = GameObject.Find("Player") ?? GameObject.Find("Rian") ?? GameObject.Find("Rian (kurir)");
            if (pGo != null) playerTransform = pGo.transform;
        }
    }

    private void GenerateSprites()
    {
        if (circleSprite != null && ringSprite != null && arrowSprite != null) return;

        // 1. Circle Sprite (128x128 anti-aliased)
        int size = 128;
        Texture2D circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleTex.wrapMode = TextureWrapMode.Clamp;
        circleTex.filterMode = FilterMode.Bilinear;
        Color32[] circleCols = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - dist) + 0.5f);
                circleCols[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        circleTex.SetPixels32(circleCols);
        circleTex.Apply();
        circleSprite = Sprite.Create(circleTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        // 2. Ring Sprite (128x128 hollow border)
        Texture2D ringTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        ringTex.wrapMode = TextureWrapMode.Clamp;
        ringTex.filterMode = FilterMode.Bilinear;
        Color32[] ringCols = new Color32[size * size];
        float innerRadius = radius - 2.6f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float outerAlpha = Mathf.Clamp01((radius - dist) + 0.5f);
                float innerAlpha = Mathf.Clamp01((dist - innerRadius) + 0.5f);
                float alpha = outerAlpha * innerAlpha;
                ringCols[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        ringTex.SetPixels32(ringCols);
        ringTex.Apply();
        ringSprite = Sprite.Create(ringTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));

        // 3. Arrow Pointer Sprite (64x64 chevron/triangle)
        int aSize = 64;
        Texture2D arrowTex = new Texture2D(aSize, aSize, TextureFormat.RGBA32, false);
        arrowTex.wrapMode = TextureWrapMode.Clamp;
        arrowTex.filterMode = FilterMode.Bilinear;
        Color32[] arrowCols = new Color32[aSize * aSize];

        for (int y = 0; y < aSize; y++)
        {
            for (int x = 0; x < aSize; x++)
            {
                float nx = (x - (aSize * 0.5f)) / (aSize * 0.5f);
                float ny = (y - 8f) / (aSize - 16f);

                bool inside = (ny >= 0f && ny <= 1f && Mathf.Abs(nx) <= (1f - ny) * 0.85f);
                if (inside && ny < 0.28f && Mathf.Abs(nx) < 0.35f)
                {
                    inside = false;
                }

                arrowCols[y * aSize + x] = inside ? new Color(1f, 1f, 1f, 1f) : new Color(0f, 0f, 0f, 0f);
            }
        }
        arrowTex.SetPixels32(arrowCols);
        arrowTex.Apply();
        arrowSprite = Sprite.Create(arrowTex, new Rect(0, 0, aSize, aSize), new Vector2(0.5f, 0.5f));
    }

    public void BuildUI(Font font)
    {
        GenerateSprites();

        rootRect = GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(1f, 1f);
        rootRect.anchoredPosition = new Vector2(-28f, -28f);
        rootRect.sizeDelta = ExpandedSize;

        Font uiFont = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bool isAirport = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");

        // Header: titik status + judul + tombol minimize
        GameObject headGo = new GameObject("RadarHeader", typeof(RectTransform));
        headGo.transform.SetParent(transform, false);
        RectTransform headRT = headGo.GetComponent<RectTransform>();
        HudStyle.Place(headRT, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(200f, 24f));

        Image headDot = HudStyle.AddImage(headGo.transform, "StatusDot", HudStyle.Circle(), HudStyle.Teal, false);
        HudStyle.Place(headDot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(7f, 7f));

        headerText = HudStyle.AddLabel(headGo.transform, "RadarTitle", isAirport ? "RADAR BANDARA" : "RADAR KOTA", 11,
            HudStyle.TextPrimary, TextAnchor.MiddleLeft, uiFont);
        HudStyle.Stretch(headerText.rectTransform, 18f, 0f, 40f, 0f);

        Image minBg = HudStyle.AddImage(headGo.transform, "Btn_Minimize", HudStyle.Rounded(7f), Color.white, true, true);
        HudStyle.Place(minBg.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(28f, 20f));
        minIconImg = HudStyle.AddImage(minBg.transform, "Icon", HudStyle.Icon("minus"), HudStyle.TextSecondary, false);
        HudStyle.Place(minIconImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f));

        Button minBtn = minBg.gameObject.AddComponent<Button>();
        minBtn.targetGraphic = minBg;
        ColorBlock minCb = minBtn.colors;
        minCb.normalColor = new Color(1f, 1f, 1f, 0.08f);
        minCb.highlightedColor = new Color(1f, 1f, 1f, 0.2f);
        minCb.pressedColor = new Color(1f, 1f, 1f, 0.04f);
        minCb.selectedColor = new Color(1f, 1f, 1f, 0.08f);
        minBtn.colors = minCb;
        minBtn.onClick.AddListener(ToggleMinimize);

        // Radar Disc Container (Klik untuk Buka Peta [M])
        discGo = new GameObject("RadarDisc", typeof(RectTransform));
        discGo.transform.SetParent(transform, false);
        RectTransform discRT = discGo.GetComponent<RectTransform>();
        discRT.anchorMin = new Vector2(0.5f, 1f);
        discRT.anchorMax = new Vector2(0.5f, 1f);
        discRT.pivot = new Vector2(0.5f, 1f);
        discRT.anchoredPosition = new Vector2(0f, -30f);
        discRT.sizeDelta = new Vector2(130f, 130f);
        discGlowGo = HudStyle.AddShadowBehind(discRT, 65f, 16f, new Color(0f, 0f, 0f, 0.5f), 4f).gameObject;

        // Background Disc (Dark Navy Glass)
        Image bgImg = discGo.AddComponent<Image>();
        bgImg.sprite = circleSprite;
        bgImg.color = new Color(0.04f, 0.055f, 0.085f, 0.90f);

        // Jadikan Radar Disc sebagai Button Klik
        Button discBtn = discGo.AddComponent<Button>();
        discBtn.targetGraphic = bgImg;
        ColorBlock cb = discBtn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.25f, 1.4f, 1f);
        cb.pressedColor = new Color(0.7f, 0.85f, 1f, 1f);
        discBtn.colors = cb;
        discBtn.onClick.AddListener(OnRadarDiscClicked);

        // Outer Ring Border
        GameObject ringGo = new GameObject("RadarOuterRing", typeof(RectTransform));
        ringGo.transform.SetParent(discGo.transform, false);
        RectTransform ringRT = ringGo.GetComponent<RectTransform>();
        ringRT.anchorMin = Vector2.zero;
        ringRT.anchorMax = Vector2.one;
        ringRT.offsetMin = Vector2.zero;
        ringRT.offsetMax = Vector2.zero;

        outerRingImg = ringGo.AddComponent<Image>();
        outerRingImg.sprite = ringSprite;
        outerRingImg.color = new Color(HudStyle.Teal.r, HudStyle.Teal.g, HudStyle.Teal.b, 0.75f);
        outerRingImg.raycastTarget = false;

        // Inner Range Ring (Skala Jarak)
        GameObject innerRingGo = new GameObject("RadarInnerRing", typeof(RectTransform));
        innerRingGo.transform.SetParent(discGo.transform, false);
        RectTransform innerRT = innerRingGo.GetComponent<RectTransform>();
        innerRT.anchorMin = new Vector2(0.25f, 0.25f);
        innerRT.anchorMax = new Vector2(0.75f, 0.75f);
        innerRT.offsetMin = Vector2.zero;
        innerRT.offsetMax = Vector2.zero;

        Image innerRingImg = innerRingGo.AddComponent<Image>();
        innerRingImg.sprite = ringSprite;
        innerRingImg.color = new Color(1f, 1f, 1f, 0.10f);
        innerRingImg.raycastTarget = false;

        // Crosshairs Axis
        CreateAxisLine(discGo.transform, new Vector2(0f, 0f), new Vector2(110f, 1f), new Color(1f, 1f, 1f, 0.07f));
        CreateAxisLine(discGo.transform, new Vector2(0f, 0f), new Vector2(1f, 110f), new Color(1f, 1f, 1f, 0.07f));

        // Cardinal Direction Labels
        CreateCardinalText(discGo.transform, font, "U", new Vector2(0f, 50f), HudStyle.Accent, 11);
        CreateCardinalText(discGo.transform, font, "S", new Vector2(0f, -50f), HudStyle.TextSecondary, 10);
        CreateCardinalText(discGo.transform, font, "T", new Vector2(50f, 0f), HudStyle.TextSecondary, 10);
        CreateCardinalText(discGo.transform, font, "B", new Vector2(-50f, 0f), HudStyle.TextSecondary, 10);

        // Petunjuk Klik Peta di piringan radar
        GameObject hintGo = new GameObject("ClickMapHint", typeof(RectTransform));
        hintGo.transform.SetParent(discGo.transform, false);
        RectTransform hintRT = hintGo.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0.5f, 0.5f);
        hintRT.anchorMax = new Vector2(0.5f, 0.5f);
        hintRT.pivot = new Vector2(0.5f, 0.5f);
        hintRT.anchoredPosition = new Vector2(0f, -32f);
        hintRT.sizeDelta = new Vector2(100f, 14f);

        clickHintText = hintGo.AddComponent<Text>();
        clickHintText.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        clickHintText.fontSize = 8;
        clickHintText.color = new Color(HudStyle.Teal.r, HudStyle.Teal.g, HudStyle.Teal.b, 0.7f);
        clickHintText.alignment = TextAnchor.MiddleCenter;
        clickHintText.text = "KLIK UNTUK PETA";
        clickHintText.raycastTarget = false;

        // Blip Container
        GameObject blipContainerGo = new GameObject("BlipContainer", typeof(RectTransform));
        blipContainerGo.transform.SetParent(discGo.transform, false);
        RectTransform bcRT = blipContainerGo.GetComponent<RectTransform>();
        bcRT.anchorMin = Vector2.zero;
        bcRT.anchorMax = Vector2.one;
        bcRT.offsetMin = Vector2.zero;
        bcRT.offsetMax = Vector2.zero;

        // Player Center Pointer
        GameObject ptrGo = new GameObject("PlayerPointer", typeof(RectTransform));
        ptrGo.transform.SetParent(discGo.transform, false);
        playerPointerRect = ptrGo.GetComponent<RectTransform>();
        playerPointerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerPointerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerPointerRect.pivot = new Vector2(0.5f, 0.5f);
        playerPointerRect.anchoredPosition = Vector2.zero;
        playerPointerRect.sizeDelta = new Vector2(16f, 16f);

        Image ptrImg = ptrGo.AddComponent<Image>();
        ptrImg.sprite = arrowSprite;
        ptrImg.color = HudStyle.Accent;
        ptrImg.raycastTarget = false;

        Outline ptrOutline = ptrGo.AddComponent<Outline>();
        ptrOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        ptrOutline.effectDistance = new Vector2(1f, -1f);

        // Target Distance & Zoom Badge (Klik untuk Ganti Zoom Radius)
        distGo = new GameObject("TargetDistanceBadge", typeof(RectTransform));
        distGo.transform.SetParent(transform, false);
        RectTransform distRT = distGo.GetComponent<RectTransform>();
        HudStyle.Place(distRT, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(200f, 38f));

        Image distBg = distGo.AddComponent<Image>();
        distBg.sprite = HudStyle.Rounded(13f);
        distBg.type = Image.Type.Sliced;
        distBg.color = HudStyle.Panel;
        Image distStroke = HudStyle.AddImage(distRT, "Stroke", HudStyle.RoundedStroke(13f, 1f), HudStyle.Stroke);
        HudStyle.Stretch(distStroke.rectTransform);

        Button distBtn = distGo.AddComponent<Button>();
        distBtn.targetGraphic = distBg;
        ColorBlock distCb = distBtn.colors;
        distCb.normalColor = Color.white;
        distCb.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        distCb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        distCb.selectedColor = Color.white;
        distBtn.colors = distCb;
        distBtn.onClick.AddListener(CycleRadarZoom);

        Image targetIcon = HudStyle.AddImage(distGo.transform, "TargetIcon", HudStyle.Icon("target"), HudStyle.Teal, false);
        HudStyle.Place(targetIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(16f, 16f));

        targetDistanceText = HudStyle.AddLabel(distGo.transform, "DistanceText", "Target: --m (60m)", 11, HudStyle.TextPrimary, TextAnchor.MiddleLeft, uiFont);
        HudStyle.Stretch(targetDistanceText.rectTransform, 36f, 2f, 10f, 2f);
        targetDistanceText.horizontalOverflow = HorizontalWrapMode.Wrap;
        targetDistanceText.verticalOverflow = VerticalWrapMode.Truncate;
        targetDistanceText.resizeTextForBestFit = true;
        targetDistanceText.resizeTextMinSize = 8;
        targetDistanceText.resizeTextMaxSize = 11;

        // Pre-instantiate Blips
        for (int i = 0; i < 12; i++)
        {
            GameObject blipGo = new GameObject($"Blip_{i}", typeof(RectTransform));
            blipGo.transform.SetParent(blipContainerGo.transform, false);

            RectTransform bRT = blipGo.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 0.5f);
            bRT.anchorMax = new Vector2(0.5f, 0.5f);
            bRT.pivot = new Vector2(0.5f, 0.5f);
            bRT.sizeDelta = new Vector2(9f, 9f);

            Image bImg = blipGo.AddComponent<Image>();
            bImg.sprite = circleSprite;
            bImg.color = Color.white;
            bImg.raycastTarget = false;

            Outline bOut = blipGo.AddComponent<Outline>();
            bOut.effectColor = new Color(0f, 0f, 0f, 0.85f);
            bOut.effectDistance = new Vector2(1f, -1f);

            blipGo.SetActive(false);

            blipPool.Add(new RadarBlip
            {
                gameObject = blipGo,
                rectTransform = bRT,
                image = bImg,
                outline = bOut
            });
        }
    }

    /// <summary>
    /// Klik piringan radar untuk membuka Peta Kota Lengkap [M].
    /// </summary>
    public void OnRadarDiscClicked()
    {
        GameAudioManager.Instance?.PlayUIClick();
        if (DeliveryHUD.Instance != null)
        {
            DeliveryHUD.Instance.ToggleMap();
        }
    }

    /// <summary>
    /// Klik badge bawah untuk mengubah skala zoom radar (35m -> 60m -> 100m).
    /// </summary>
    public void CycleRadarZoom()
    {
        GameAudioManager.Instance?.PlayUIClick();
        if (radarWorldRange <= 40f)
        {
            radarWorldRange = 60f;
        }
        else if (radarWorldRange <= 75f)
        {
            radarWorldRange = 100f;
        }
        else
        {
            radarWorldRange = 35f;
        }

        Debug.Log($"[DeliveryRadarHUD] 🔍 Skala zoom radar diubah ke: {radarWorldRange}m");
    }

    /// <summary>
    /// Klik tombol [-] atau [+] untuk minimize / restore radar.
    /// </summary>
    public void ToggleMinimize()
    {
        GameAudioManager.Instance?.PlayUIClick();
        isMinimized = !isMinimized;

        if (discGo != null) discGo.SetActive(!isMinimized);
        if (distGo != null) distGo.SetActive(!isMinimized);
        if (discGlowGo != null) discGlowGo.SetActive(!isMinimized);
        if (minIconImg != null) minIconImg.sprite = HudStyle.Icon(isMinimized ? "plus" : "minus");
        if (rootRect != null)
        {
            rootRect.sizeDelta = isMinimized ? CollapsedSize : ExpandedSize;
        }
    }

    private void CreateAxisLine(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject line = new GameObject("AxisLine", typeof(RectTransform));
        line.transform.SetParent(parent, false);
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = line.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    private void CreateCardinalText(Transform parent, Font font, string text, Vector2 pos, Color color, int fontSize)
    {
        GameObject go = new GameObject($"Cardinal_{text}", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(20f, 20f);

        Text t = go.AddComponent<Text>();
        t.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = text;
        t.raycastTarget = false;
    }

    private void Update()
    {
        // Toggle minimize/maximize dengan tombol [N]
        if (Input.GetKeyDown(KeyCode.N))
        {
            ToggleMinimize();
        }

        if (!isVisible || isMinimized || playerPointerRect == null) return;

        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        // 1. Orientasi Jarum Pemain (Static North: panah berputar sesuai hadap Rian)
        float playerYaw = playerTransform.eulerAngles.y;
        playerPointerRect.localRotation = Quaternion.Euler(0f, 0f, -playerYaw);

        // 2. Perbarui Posisi Blip Target
        UpdateRadarBlips();
    }

    private void UpdateRadarBlips()
    {
        Vector3 playerPos = playerTransform.position;
        int blipIndex = 0;

        DeliveryGameManager gm = DeliveryGameManager.Instance;
        DeliveryMissionState currentState = gm != null ? gm.CurrentState : DeliveryMissionState.Prolog;

        // Ambil seluruh DeliveryRecipient di scene
        DeliveryRecipient[] recipients = Object.FindObjectsByType<DeliveryRecipient>();
        Transform closestActiveTarget = null;
        float closestDistance = float.MaxValue;
        string activeTargetName = "Penerima";

        bool isAirportLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");

        foreach (var r in recipients)
        {
            if (r == null || !r.gameObject.activeInHierarchy) continue;
            if (blipIndex >= blipPool.Count) break;

            Vector3 npcPos = r.transform.position;
            if (BuildingInterior.TryGetRadarPosition(r, playerPos, out Vector3 doorPos)) npcPos = doorPos; // Pak Beni di dalam gedung: tunjuk pintu masuk
            Vector3 diff = npcPos - playerPos;
            float dist = new Vector2(diff.x, diff.z).magnitude;

            bool isCurrentTarget = false;

            if (currentState == DeliveryMissionState.Prolog || currentState == DeliveryMissionState.Final_ReturnToHub)
            {
                isCurrentTarget = (r.recipientType == DeliveryRecipient.RecipientType.PakBeni);
            }
            else if (!isAirportLevel2 && currentState == DeliveryMissionState.Package4_DroppedPackage)
            {
                isCurrentTarget = (r.recipientType == DeliveryRecipient.RecipientType.PetugasParkir);
            }
            else
            {
                isCurrentTarget = (r.targetForMission == currentState);
            }

            if (isCurrentTarget && dist < closestDistance)
            {
                closestDistance = dist;
                closestActiveTarget = r.transform;
                activeTargetName = r.characterName;
            }

            RadarBlip blip = blipPool[blipIndex];
            blip.gameObject.SetActive(true);

            // Koordinat 2D pada Radar (X = diff.x, Y = diff.z)
            float scale = radarRadius / Mathf.Max(radarWorldRange, 10f);
            Vector2 radarPos = new Vector2(diff.x, diff.z) * scale;

            if (radarPos.magnitude > radarRadius - 4f)
            {
                radarPos = radarPos.normalized * (radarRadius - 4f);
            }

            blip.rectTransform.anchoredPosition = radarPos;

            // Pewarnaan Blip
            if (isCurrentTarget)
            {
                // Target Aktif: Emas/Kuning berdenyut
                float pulse = 1f + 0.22f * Mathf.Sin(Time.time * 6f);
                blip.rectTransform.sizeDelta = new Vector2(12f * pulse, 12f * pulse);
                blip.image.color = new Color(1f, 0.78f, 0.12f, 1f);
                blip.outline.effectColor = new Color(0.8f, 0.3f, 0f, 0.95f);
            }
            else
            {
                // NPC Lainnya: Cyan Halus
                blip.rectTransform.sizeDelta = new Vector2(7f, 7f);
                blip.image.color = new Color(HudStyle.Teal.r, HudStyle.Teal.g, HudStyle.Teal.b, 0.85f);
                blip.outline.effectColor = new Color(0.05f, 0.15f, 0.3f, 0.8f);
            }

            blipIndex++;
        }

        // Cek paket jatuh (Mission 4 kota)
        if (!isAirportLevel2 && currentState == DeliveryMissionState.Package4_DroppedPackage)
        {
            GameObject dropped = GameObject.Find("DroppedPackagePickup") ?? GameObject.Find("DroppedPackage");
            if (dropped != null && dropped.activeInHierarchy && blipIndex < blipPool.Count)
            {
                Vector3 dPos = dropped.transform.position;
                Vector3 diff = dPos - playerPos;
                float dDist = new Vector2(diff.x, diff.z).magnitude;

                RadarBlip blip = blipPool[blipIndex];
                blip.gameObject.SetActive(true);

                float scale = radarRadius / Mathf.Max(radarWorldRange, 10f);
                Vector2 radarPos = new Vector2(diff.x, diff.z) * scale;
                if (radarPos.magnitude > radarRadius - 4f)
                {
                    radarPos = radarPos.normalized * (radarRadius - 4f);
                }

                blip.rectTransform.anchoredPosition = radarPos;
                blip.rectTransform.sizeDelta = new Vector2(11f, 11f);
                blip.image.color = new Color(1f, 0.35f, 0.2f, 1f);
                blip.outline.effectColor = Color.black;

                if (dDist < closestDistance)
                {
                    closestDistance = dDist;
                    closestActiveTarget = dropped.transform;
                    activeTargetName = "Paket Kardus Jatuh";
                }

                blipIndex++;
            }
        }

        // Matikan sisa blip di pool
        for (int i = blipIndex; i < blipPool.Count; i++)
        {
            blipPool[i].gameObject.SetActive(false);
        }

        // Perbarui Teks Jarak Target & Skala Zoom
        if (targetDistanceText != null)
        {
            int zoomMeters = Mathf.RoundToInt(radarWorldRange);
            if (closestActiveTarget != null)
            {
                targetDistanceText.text = $"{activeTargetName}: {Mathf.RoundToInt(closestDistance)}m [{zoomMeters}m]";
                targetDistanceText.color = (closestDistance <= 4f)
                    ? HudStyle.Success
                    : HudStyle.Warning;
            }
            else
            {
                targetDistanceText.text = $"Menunggu Misi [{zoomMeters}m]";
                targetDistanceText.color = HudStyle.TextSecondary;
            }
        }
    }
}
