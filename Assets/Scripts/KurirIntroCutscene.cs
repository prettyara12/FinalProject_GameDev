using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cutscene pembuka "Kurir Terakhir" (±15 detik) yang diputar setelah pemain menekan "Mulai Permainan".
///
/// Cerita singkat (berdasarkan dokumen History Game):
///   1. Kota menjelang malam, toko-toko bersiap tutup.            (establishing shot)
///   2. Pak Beni menyerahkan 5 paket terakhir kepada Rian.          (two-shot Pak Beni & Rian)
///   3. Label paket basah: Rian harus bertanya ke pegawai toko.     (Clara - coffee shop, Pak Joko - drug store)
///   4. Rintangan: lalu lintas & paket terjatuh di area parkir.     (fire station, petugas parkir)
///   5. Rian siap berangkat -> kamera turun tepat ke belakang Rian, gameplay dimulai.
///
/// Dibuat sepenuhnya saat runtime (kamera, UI letterbox, subtitle, tombol Lewati),
/// tidak bergantung pada Timeline / Cinemachine / objek scene lama.
/// Skip: tombol "Lewati" di layar, atau [Spasi] / [Esc] / [Enter].
/// </summary>
public class KurirIntroCutscene : MonoBehaviour
{
    public static KurirIntroCutscene Instance { get; private set; }
    public static bool IsPlaying => Instance != null && Instance.playing;
    public static bool HasPlayedThisSession { get; private set; }

    private const float TotalDuration = 15f;

    private struct Shot
    {
        public float start, end;
        public Vector3 camFrom, camTo, lookFrom, lookTo;
        public float fovFrom, fovTo;
        public bool orbit;      // jika true: kamera berputar mengelilingi pivot (tidak menembus karakter)
        public Vector3 pivot;
    }

    private struct Beat
    {
        public float start, end;
        public string speaker, line;
        public Beat(float s, float e, string sp, string l) { start = s; end = e; speaker = sp; line = l; }
    }

    private static readonly Beat[] beats =
    {
        new Beat(0.6f, 3.0f,  "Narator", "Menjelang malam, toko-toko di kawasan kota mulai bersiap tutup."),
        new Beat(3.0f, 6.4f,  "Pak Beni", "\"Rian, ini lima paket terakhir. Semuanya harus sampai sebelum waktu operasional habis!\""),
        new Beat(6.4f, 9.6f,  "Narator", "Label paket basah terkena hujan. Rian harus bertanya kepada pegawai toko dan warga untuk menemukan penerima yang tepat."),
        new Beat(9.6f, 12.4f, "Narator", "Hati-hati dengan kendaraan di jalan, dan temukan paket yang terjatuh di area parkir."),
        new Beat(12.4f, 15.0f, "Rian", "\"Siap, Pak! Semua paket akan sampai ke orang yang tepat, tepat waktu!\""),
    };

    private Shot[] shots;
    private Camera cam;
    private ThirdPersonCamera tpc;
    private Behaviour cinemachineBrain;
    private PlayerController rian;
    private Action onFinished;
    private float time;
    private bool playing;
    private float originalFov;

    // UI
    private GameObject canvasGo;
    private RectTransform barTop, barBottom;
    private Image fadeImage;
    private Text titleText, subtitleSpeaker, subtitleLine;
    private CanvasGroup titleGroup, subtitleGroup;
    private Font font;

    // ===================================================================== API

    /// <summary>Memutar cutscene. onFinished dipanggil sekali saat cutscene selesai atau dilewati.</summary>
    public static void Play(Action onFinished)
    {
        if (Instance != null) return;
        var go = new GameObject("KurirIntroCutscene_Runtime");
        var cs = go.AddComponent<KurirIntroCutscene>();
        cs.onFinished = onFinished;
        cs.Begin();
    }

    public void Skip()
    {
        if (!playing) return;
        Debug.Log("[KurirIntroCutscene] ⏩ Cutscene dilewati oleh pemain.");
        Finish();
    }

    // ===================================================================== Lifecycle

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Begin()
    {
        cam = Camera.main;
        if (cam == null) cam = FindAnyObjectByType<Camera>();
        rian = FindRian();

        if (cam == null || rian == null)
        {
            Debug.LogWarning("[KurirIntroCutscene] Kamera atau Rian tidak ditemukan, cutscene dilewati.");
            playing = true;
            Finish();
            return;
        }

        tpc = cam.GetComponent<ThirdPersonCamera>();
        if (tpc != null) tpc.enabled = false;

        // Pastikan CinemachineBrain (jika ada) tidak ikut menggerakkan kamera
        foreach (var b in cam.GetComponents<Behaviour>())
        {
            if (b != null && b.GetType().Name == "CinemachineBrain")
            {
                cinemachineBrain = b;
                b.enabled = false;
            }
        }

        originalFov = cam.fieldOfView;
        rian.SetMovementFrozen(true);

        BuildShots();
        BuildUI();

        time = 0f;
        playing = true;
        Apply(0f);
        Debug.Log("[KurirIntroCutscene] 🎬 Cutscene pembuka Kurir Terakhir dimulai (15 detik).");
    }

    private void Update()
    {
        if (!playing) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Skip();
            return;
        }

        time += Time.unscaledDeltaTime;
        if (time >= TotalDuration)
        {
            Finish();
        }
    }

    private void LateUpdate()
    {
        if (!playing) return;
        Apply(time);
    }

    // ===================================================================== Shots

    private static PlayerController FindRian()
    {
        var all = FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
        foreach (var p in all) if (p.gameObject.activeInHierarchy) return p;
        return all.Length > 0 ? all[0] : null;
    }

    private static Vector3 FindPos(string name, Vector3 fallback)
    {
        var go = GameObject.Find(name);
        return go != null ? go.transform.position : fallback;
    }

    private void BuildShots()
    {
        Transform r = rian.transform;
        float s = Mathf.Max(0.01f, r.lossyScale.y);          // Module 2 berskala 0.1
        Vector3 up = Vector3.up;
        Vector3 rianPos = r.position;
        Vector3 rianHead = rianPos + up * (1.6f * s);

        Vector3 beni   = FindPos("Pak Beni",       rianPos + new Vector3(-1.2f, 0f, 1.1f));
        Vector3 clara  = FindPos("Clara",          rianPos + new Vector3(4.1f, 0f, 4.1f));
        Vector3 joko   = FindPos("Pak Joko",       rianPos + new Vector3(4.4f, 0f, -1.1f));
        Vector3 dimas  = FindPos("Dimas",          rianPos + new Vector3(-9f, 0f, 2.4f));
        Vector3 parkir = FindPos("Petugas Parkir", rianPos + new Vector3(-11.8f, 0f, -0.9f));

        // Pose akhir = pose ThirdPersonCamera di belakang Rian (agar transisi ke gameplay mulus)
        float tH = tpc != null ? tpc.targetHeight : 1.8f * s;
        float dist = tpc != null ? tpc.distance : 13.5f * s;
        float h = tpc != null ? tpc.height : 4.5f * s;
        Vector3 focus = rianPos + up * tH;
        Vector3 endCam = focus + Quaternion.Euler(15f, r.eulerAngles.y, 0f) * new Vector3(0f, h, -dist);

        Vector3 dirRB = rianPos - beni; dirRB.y = 0f;
        dirRB = dirRB.sqrMagnitude > 1e-6f ? dirRB.normalized : -r.forward;
        Vector3 sideRB = Vector3.Cross(Vector3.up, dirRB);

        Vector3 shopsMid = (clara + joko) * 0.5f;
        Vector3 hazardMid = (dimas + parkir) * 0.5f;
        Vector3 hazardFocus = hazardMid + new Vector3(0f, 0f, -4.8f) * s;
        Vector3 cityCenter = (rianPos + shopsMid + hazardMid) / 3f;

        shots = new[]
        {
            // 1. Establishing: kamera tinggi melayang pelan di atas kawasan pertokoan
            new Shot { start = 0f, end = 3f,
                camFrom = cityCenter + new Vector3(-6f, 6f, -10f) * (s * 10f),
                camTo   = cityCenter + new Vector3(-3f, 4.5f, -8f) * (s * 10f),
                lookFrom = cityCenter, lookTo = cityCenter + up * (2f * s),
                fovFrom = 55f, fovTo = 50f },

            // 2. Pak Beni memberi tugas kepada Rian (over-the-shoulder dari belakang Rian, push-in pelan)
            new Shot { start = 3f, end = 6.4f,
                camFrom = rianPos + dirRB * (6f * s) + sideRB * (2.5f * s) + up * (2.6f * s),
                camTo   = rianPos + dirRB * (3.5f * s) + sideRB * (2.0f * s) + up * (2.2f * s),
                lookFrom = Vector3.Lerp(rianPos, beni, 0.55f) + up * (1.2f * s),
                lookTo   = Vector3.Lerp(rianPos, beni, 0.7f) + up * (1.4f * s),
                fovFrom = 40f, fovTo = 34f },

            // 3a. Coffee shop: Clara di depan papan "COFFEE SHOP" (push-in pelan)
            new Shot { start = 6.4f, end = 8.0f,
                camFrom = clara + new Vector3(-16f, 4.0f, 1.5f) * s,
                camTo   = clara + new Vector3(-12f, 3.2f, 0f) * s,
                lookFrom = clara + up * (1.0f * s), lookTo = clara + up * (1.0f * s),
                fovFrom = 48f, fovTo = 44f },

            // 3b. Drug store: Pak Joko di depan toko obat
            new Shot { start = 8.0f, end = 9.6f,
                camFrom = joko + new Vector3(1.5f, 4.2f, 16f) * s,
                camTo   = joko + new Vector3(0f, 3.4f, 12f) * s,
                lookFrom = joko + up * (1.2f * s), lookTo = joko + up * (1.0f * s),
                fovFrom = 48f, fovTo = 44f },

            // 4. Rintangan: jalan raya di depan Fire Station & petugas parkir (pan pelan)
            new Shot { start = 9.6f, end = 12.4f,
                camFrom = hazardFocus + new Vector3(-26f, 9.7f, 4f) * s,
                camTo   = hazardFocus + new Vector3(-23f, 8.0f, -3f) * s,
                lookFrom = hazardFocus + new Vector3(0f, 0.5f, 6f) * s, lookTo = hazardFocus + up * (0.5f * s),
                fovFrom = 55f, fovTo = 52f },

            // 5. Kembali ke Rian: dari depan wajah Rian, berputar turun ke belakang punggung (pose gameplay)
            new Shot { start = 12.4f, end = 15f,
                camFrom = rianHead + (Quaternion.Euler(0f, 70f, 0f) * r.forward) * (6f * s) + up * (0.6f * s),
                camTo   = endCam,
                lookFrom = rianHead, lookTo = focus,
                fovFrom = 40f, fovTo = originalFov,
                orbit = true, pivot = focus },
        };
    }

    private void Apply(float t)
    {
        // Pilih shot aktif
        Shot shot = shots[shots.Length - 1];
        foreach (var sh in shots) { if (t >= sh.start && t < sh.end) { shot = sh; break; } }

        float k = Mathf.Clamp01((t - shot.start) / Mathf.Max(0.01f, shot.end - shot.start));
        float e = k * k * (3f - 2f * k); // smoothstep

        Vector3 pos = shot.orbit
            ? shot.pivot + Vector3.Slerp(shot.camFrom - shot.pivot, shot.camTo - shot.pivot, e)
            : Vector3.Lerp(shot.camFrom, shot.camTo, e);
        Vector3 look = Vector3.Lerp(shot.lookFrom, shot.lookTo, e);
        cam.transform.position = pos;
        if ((look - pos).sqrMagnitude > 1e-8f) cam.transform.rotation = Quaternion.LookRotation(look - pos);
        cam.fieldOfView = Mathf.Lerp(shot.fovFrom, shot.fovTo, e);

        UpdateUI(t);
    }

    // ===================================================================== Finish

    private void Finish()
    {
        if (!playing) return;
        playing = false;
        HasPlayedThisSession = true;

        if (cam != null) cam.fieldOfView = originalFov;
        if (cinemachineBrain != null) cinemachineBrain.enabled = false;
        if (canvasGo != null) Destroy(canvasGo);

        // Gameplay dimulai (timer, posisi awal Rian, HUD) lewat callback dari DeliveryHUD
        try { onFinished?.Invoke(); }
        catch (Exception ex) { Debug.LogException(ex); }

        // Kamera langsung terarah ke Rian
        if (tpc != null)
        {
            if (tpc.target == null && rian != null) tpc.target = rian.transform;
            tpc.viewMode = ThirdPersonCamera.CameraViewMode.ThirdPersonFollow;
            tpc.enabled = true;
            tpc.SnapToTarget();
        }
        if (rian != null) rian.SetMovementFrozen(false);

        Debug.Log("[KurirIntroCutscene] ✅ Cutscene selesai — kamera mengikuti Rian, gameplay dimulai.");
        Destroy(gameObject);
    }

    // ===================================================================== UI

    private void BuildUI()
    {
        font = LoadFont();

        canvasGo = new GameObject("KurirIntroCutscene_Canvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        DeliveryHUD.EnsureEventSystem();

        // Letterbox sinematik
        barTop = MakeBar("Letterbox_Top", new Vector2(0f, 1f));
        barBottom = MakeBar("Letterbox_Bottom", new Vector2(0f, 0f));

        // Judul
        var titleRoot = new GameObject("Title", typeof(RectTransform), typeof(CanvasGroup));
        titleRoot.transform.SetParent(canvasGo.transform, false);
        var trt = (RectTransform)titleRoot.transform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.62f);
        trt.sizeDelta = new Vector2(1400f, 220f);
        titleGroup = titleRoot.GetComponent<CanvasGroup>();
        titleText = MakeText(titleRoot.transform, "KURIR TERAKHIR", 96, FontStyle.Bold, new Color(1f, 0.78f, 0.25f),
            TextAnchor.MiddleCenter, new Vector2(0f, 30f), new Vector2(1400f, 130f));
        MakeText(titleRoot.transform, "Lima paket terakhir. Satu sore yang menentukan.", 34, FontStyle.Normal, new Color(1f, 1f, 1f, 0.9f),
            TextAnchor.MiddleCenter, new Vector2(0f, -60f), new Vector2(1400f, 60f));

        // Subtitle (di dalam bar bawah)
        var subRoot = new GameObject("Subtitle", typeof(RectTransform), typeof(CanvasGroup));
        subRoot.transform.SetParent(canvasGo.transform, false);
        var srt = (RectTransform)subRoot.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 18f);
        srt.sizeDelta = new Vector2(1500f, 120f);
        subtitleGroup = subRoot.GetComponent<CanvasGroup>();
        subtitleSpeaker = MakeText(subRoot.transform, "", 30, FontStyle.Bold, new Color(1f, 0.8f, 0.25f),
            TextAnchor.UpperCenter, new Vector2(0f, 38f), new Vector2(1500f, 40f));
        subtitleLine = MakeText(subRoot.transform, "", 32, FontStyle.Normal, Color.white,
            TextAnchor.UpperCenter, new Vector2(0f, -14f), new Vector2(1500f, 80f));

        // Tombol Lewati
        var btnGo = new GameObject("Btn_SkipCutscene", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(canvasGo.transform, false);
        var brt = (RectTransform)btnGo.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 1f);
        brt.anchoredPosition = new Vector2(-36f, -28f);
        brt.sizeDelta = new Vector2(260f, 64f);
        var img = btnGo.GetComponent<Image>();
        img.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);
        var btn = btnGo.GetComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(0.96f, 0.58f, 0.12f, 1f);
        cb.pressedColor = new Color(0.68f, 0.32f, 0.04f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(Skip);
        MakeText(btnGo.transform, "Lewati  ▶▶", 30, FontStyle.Bold, Color.white,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(260f, 64f));
        MakeText(canvasGo.transform, "[Spasi] / [Esc]", 22, FontStyle.Normal, new Color(1f, 1f, 1f, 0.7f),
            TextAnchor.UpperRight, new Vector2(-36f, -98f), new Vector2(260f, 30f), new Vector2(1f, 1f));

        // Fade layar penuh (paling atas, tidak memblokir klik)
        var fadeGo = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fadeGo.transform.SetParent(canvasGo.transform, false);
        var frt = (RectTransform)fadeGo.transform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        fadeImage = fadeGo.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;
        fadeGo.transform.SetSiblingIndex(0);
        btnGo.transform.SetAsLastSibling();
    }

    private void UpdateUI(float t)
    {
        // Letterbox: masuk di awal, keluar di 1 detik terakhir
        float barIn = Mathf.Clamp01(t / 0.8f);
        float barOut = Mathf.Clamp01((TotalDuration - t) / 1.0f);
        float barH = 130f * Mathf.Min(barIn, barOut);
        barTop.sizeDelta = new Vector2(0f, barH);
        barBottom.sizeDelta = new Vector2(0f, barH);

        // Fade dari hitam di awal + "dip" singkat di setiap pergantian shot
        float fade = 1f - Mathf.Clamp01(t / 0.8f);
        foreach (var sh in shots)
        {
            if (sh.start <= 0.01f) continue;
            float d = Mathf.Abs(t - sh.start);
            if (d < 0.25f) fade = Mathf.Max(fade, (1f - d / 0.25f) * 0.85f);
        }
        fadeImage.color = new Color(0f, 0f, 0f, fade);

        // Judul tampil di shot pertama
        float titleA = Mathf.Clamp01((t - 0.5f) / 0.6f) * Mathf.Clamp01((3.0f - t) / 0.5f);
        titleGroup.alpha = titleA;

        // Subtitle
        string sp = "", ln = "";
        float subA = 0f;
        foreach (var b in beats)
        {
            if (t >= b.start && t < b.end)
            {
                sp = b.speaker; ln = b.line;
                subA = Mathf.Clamp01((t - b.start) / 0.25f) * Mathf.Clamp01((b.end - t) / 0.25f);
                break;
            }
        }
        // Di shot judul, subtitle muncul setelah judul memudar sedikit
        if (t < 3f) subA *= 0.95f;
        subtitleSpeaker.text = sp;
        subtitleLine.text = ln;
        subtitleGroup.alpha = subA;
    }

    private RectTransform MakeBar(string name, Vector2 anchorY)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, anchorY.y);
        rt.anchorMax = new Vector2(1f, anchorY.y);
        rt.pivot = new Vector2(0.5f, anchorY.y);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 0f);
        var img = go.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return rt;
    }

    private Text MakeText(Transform parent, string content, int size, FontStyle style, Color color,
                          TextAnchor align, Vector2 pos, Vector2 sizeDelta, Vector2? anchor = null)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        rt.anchorMin = rt.anchorMax = a;
        rt.pivot = a;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var txt = go.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = align;
        txt.text = content;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        return txt;
    }

    private static Font LoadFont()
    {
        Font f = null;
#if UNITY_EDITOR
        f = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/Develop 3D Mobile Games in Unity/Assets/MobileUI/fonts/Roboto-Bold.ttf");
#endif
        if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return f;
    }
}
