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
    private Text minBtnText;
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
        float innerRadius = radius - 4.5f;

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
        rootRect.anchoredPosition = new Vector2(-20f, -20f);
        rootRect.sizeDelta = new Vector2(150f, 185f);

        // Header Title Container
        GameObject headGo = new GameObject("RadarHeader", typeof(RectTransform));
        headGo.transform.SetParent(transform, false);
        RectTransform headRT = headGo.GetComponent<RectTransform>();
        headRT.anchorMin = new Vector2(0.5f, 1f);
        headRT.anchorMax = new Vector2(0.5f, 1f);
        headRT.pivot = new Vector2(0.5f, 1f);
        headRT.anchoredPosition = new Vector2(0f, 0f);
        headRT.sizeDelta = new Vector2(150f, 20f);

        headerText = headGo.AddComponent<Text>();
        headerText.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headerText.fontSize = 11;
        headerText.fontStyle = FontStyle.Bold;
        headerText.color = new Color(0.4f, 0.82f, 1f, 0.95f);
        headerText.alignment = TextAnchor.MiddleLeft;
        bool isAirport = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Level 2");
        headerText.text = isAirport ? " RADAR BANDARA" : " RADAR KOTA";

        // Minimize / Maximize Button [-]
        GameObject minBtnGo = new GameObject("Btn_Minimize", typeof(RectTransform));
        minBtnGo.transform.SetParent(headGo.transform, false);
        RectTransform minRT = minBtnGo.GetComponent<RectTransform>();
        minRT.anchorMin = new Vector2(1f, 0.5f);
        minRT.anchorMax = new Vector2(1f, 0.5f);
        minRT.pivot = new Vector2(1f, 0.5f);
        minRT.anchoredPosition = new Vector2(0f, 0f);
        minRT.sizeDelta = new Vector2(24f, 20f);

        Button minBtn = minBtnGo.AddComponent<Button>();
        minBtnText = minBtnGo.AddComponent<Text>();
        minBtnText.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        minBtnText.fontSize = 12;
        minBtnText.fontStyle = FontStyle.Bold;
        minBtnText.color = new Color(0.7f, 0.85f, 1f, 0.9f);
        minBtnText.alignment = TextAnchor.MiddleCenter;
        minBtnText.text = "[-]";

        minBtn.targetGraphic = minBtnText;
        ColorBlock minCb = minBtn.colors;
        minCb.normalColor = Color.white;
        minCb.highlightedColor = Color.yellow;
        minCb.pressedColor = new Color(0.5f, 0.8f, 1f, 1f);
        minBtn.colors = minCb;
        minBtn.onClick.AddListener(ToggleMinimize);

        // Radar Disc Container (Klik untuk Buka Peta [M])
        discGo = new GameObject("RadarDisc", typeof(RectTransform));
        discGo.transform.SetParent(transform, false);
        RectTransform discRT = discGo.GetComponent<RectTransform>();
        discRT.anchorMin = new Vector2(0.5f, 1f);
        discRT.anchorMax = new Vector2(0.5f, 1f);
        discRT.pivot = new Vector2(0.5f, 1f);
        discRT.anchoredPosition = new Vector2(0f, -22f);
        discRT.sizeDelta = new Vector2(130f, 130f);

        // Background Disc (Dark Navy Glass)
        Image bgImg = discGo.AddComponent<Image>();
        bgImg.sprite = circleSprite;
        bgImg.color = new Color(0.03f, 0.07f, 0.14f, 0.90f);

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
        outerRingImg.color = new Color(0.24f, 0.65f, 1f, 0.85f);
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
        innerRingImg.color = new Color(0.16f, 0.35f, 0.60f, 0.50f);
        innerRingImg.raycastTarget = false;

        // Crosshairs Axis
        CreateAxisLine(discGo.transform, new Vector2(0f, 0f), new Vector2(110f, 1f), new Color(0.18f, 0.35f, 0.55f, 0.4f));
        CreateAxisLine(discGo.transform, new Vector2(0f, 0f), new Vector2(1f, 110f), new Color(0.18f, 0.35f, 0.55f, 0.4f));

        // Cardinal Direction Labels
        CreateCardinalText(discGo.transform, font, "U", new Vector2(0f, 50f), new Color(1f, 0.82f, 0.25f, 1f), 11);
        CreateCardinalText(discGo.transform, font, "S", new Vector2(0f, -50f), new Color(0.6f, 0.75f, 0.9f, 0.8f), 10);
        CreateCardinalText(discGo.transform, font, "T", new Vector2(50f, 0f), new Color(0.6f, 0.75f, 0.9f, 0.8f), 10);
        CreateCardinalText(discGo.transform, font, "B", new Vector2(-50f, 0f), new Color(0.6f, 0.75f, 0.9f, 0.8f), 10);

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
        clickHintText.color = new Color(0.35f, 0.75f, 1f, 0.65f);
        clickHintText.alignment = TextAnchor.MiddleCenter;
        clickHintText.text = "[KLIK: PETA]";
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
        ptrImg.color = new Color(1f, 0.95f, 0.3f, 1f);
        ptrImg.raycastTarget = false;

        Outline ptrOutline = ptrGo.AddComponent<Outline>();
        ptrOutline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        ptrOutline.effectDistance = new Vector2(1f, -1f);

        // Target Distance & Zoom Badge (Klik untuk Ganti Zoom Radius)
        distGo = new GameObject("TargetDistanceBadge", typeof(RectTransform));
        distGo.transform.SetParent(transform, false);
        RectTransform distRT = distGo.GetComponent<RectTransform>();
        distRT.anchorMin = new Vector2(0.5f, 0f);
        distRT.anchorMax = new Vector2(0.5f, 0f);
        distRT.pivot = new Vector2(0.5f, 0f);
        distRT.anchoredPosition = new Vector2(0f, 4f);
        distRT.sizeDelta = new Vector2(146f, 24f);

        Image distBg = distGo.AddComponent<Image>();
        distBg.color = new Color(0.04f, 0.08f, 0.16f, 0.94f);

        Outline distOutline = distGo.AddComponent<Outline>();
        distOutline.effectColor = new Color(0.2f, 0.5f, 0.85f, 0.6f);
        distOutline.effectDistance = new Vector2(1f, -1f);

        Button distBtn = distGo.AddComponent<Button>();
        distBtn.targetGraphic = distBg;
        ColorBlock distCb = distBtn.colors;
        distCb.normalColor = Color.white;
        distCb.highlightedColor = new Color(1.2f, 1.3f, 1.5f, 1f);
        distCb.pressedColor = new Color(0.7f, 0.85f, 1f, 1f);
        distBtn.colors = distCb;
        distBtn.onClick.AddListener(CycleRadarZoom);

        GameObject textSubGo = new GameObject("DistanceText", typeof(RectTransform));
        textSubGo.transform.SetParent(distGo.transform, false);
        RectTransform tsRT = textSubGo.GetComponent<RectTransform>();
        tsRT.anchorMin = Vector2.zero;
        tsRT.anchorMax = Vector2.one;
        tsRT.offsetMin = new Vector2(4f, 0f);
        tsRT.offsetMax = new Vector2(-4f, 0f);

        targetDistanceText = textSubGo.AddComponent<Text>();
        targetDistanceText.font = font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        targetDistanceText.fontSize = 10;
        targetDistanceText.fontStyle = FontStyle.Bold;
        targetDistanceText.color = new Color(0.95f, 0.97f, 1f, 1f);
        targetDistanceText.alignment = TextAnchor.MiddleCenter;
        targetDistanceText.text = "🎯 Target: --m (60m)";
        targetDistanceText.raycastTarget = false;

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
        if (minBtnText != null) minBtnText.text = isMinimized ? "[+]" : "[-]";
        if (rootRect != null)
        {
            rootRect.sizeDelta = isMinimized ? new Vector2(150f, 22f) : new Vector2(150f, 185f);
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
                blip.image.color = new Color(0.25f, 0.85f, 1f, 0.85f);
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
                targetDistanceText.text = $"🎯 {activeTargetName}: {Mathf.RoundToInt(closestDistance)}m [{zoomMeters}m]";
                targetDistanceText.color = (closestDistance <= 4f)
                    ? new Color(0.3f, 1f, 0.5f, 1f)
                    : new Color(1f, 0.88f, 0.35f, 1f);
            }
            else
            {
                targetDistanceText.text = $"🎯 Menunggu Misi [{zoomMeters}m]";
                targetDistanceText.color = new Color(0.7f, 0.75f, 0.85f, 1f);
            }
        }
    }
}
