using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BuildingInterior: ruangan dalam yang bisa dimasuki Rian (Mal di Scene 1, Gedung Terminal 3 di Scene 2).
///
/// Cara kerja:
///  - Pintu masuk bercahaya (gerbang + lingkaran) dipasang di depan bangunan. Dekati lalu tekan [E] untuk masuk.
///  - Ruangan dibangun dari kubus berwarna di area kosong di luar peta, jadi tidak bentrok dengan collider gedung.
///    Perpindahan memakai layar fade singkat. Di dalam ada kantor Pak Beni: meja, bangku, tanaman, karpet, lampu.
///  - Saat misi mencapai tahap lapor akhir (Final_ReturnToHub), Pak Beni pindah ke dalam ruangan;
///    radar menunjuk ke pintu masuk selama Rian masih di luar.
/// </summary>
public class BuildingInterior : MonoBehaviour
{
    public static BuildingInterior Hub { get; private set; }

    [Header("Bangunan")]
    public string buildingName = "Gedung";
    public bool isHub = true;
    public float worldScale = 1f;

    [Header("Pintu masuk (di luar)")]
    public Vector3 doorOutside;
    public Vector3 doorFacing = Vector3.forward; // arah menjauhi gedung

    [Header("Ruangan dalam")]
    public Vector3 roomCenter;
    public Vector2 halfSize = new Vector2(6f, 5f);
    public float roomHeight = 5f;

    private PlayerController player;
    private DeliveryRecipient pakBeni;
    private Vector3 pakBeniHomePos;
    private Quaternion pakBeniHomeRot;
    private bool pakBeniHomeSaved;
    private bool pakBeniInside;
    private bool bound;
    private bool transitioning;
    private float savedCamDistance = -1f;
    private float savedCamHeight = -1f;

    private Vector3 insideSpawn;
    private Vector3 exitPoint;
    private Vector3 pakBeniInsidePos;
    private Transform outsideRing;
    private Transform insideRing;
    private TextMesh outsideLabel;
    private TextMesh insideLabel;
    private readonly List<Material> materials = new List<Material>();

    private static Image fadeImage;
    private static GameObject fadeCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Hub = null;
        fadeImage = null;
        fadeCanvas = null;
    }

    // ===================================================================== FACTORY

    /// <summary>Scene 1 (Module 2): Mal kota, skala dunia 0.1.</summary>
    public static BuildingInterior CreateForCity()
    {
        if (!Application.isPlaying) return null;
        if (GameObject.Find("BuildingInterior_Mal") != null) return null;

        var go = new GameObject("BuildingInterior_Mal");
        var bi = go.AddComponent<BuildingInterior>();
        bi.buildingName = "Mal";
        bi.worldScale = 0.1f;
        // Fasad Mega Mall menghadap timur (ke arah Pos Pengiriman); sisi barat/utara lot tertutup collider mal
        bi.doorOutside = new Vector3(311.05f, 0f, 4.3f);
        bi.doorFacing = new Vector3(1f, 0f, 0f);
        bi.roomCenter = new Vector3(520f, 0f, 0f);
        bi.halfSize = new Vector2(2.8f, 2.3f);
        bi.roomHeight = 1.05f;
        bi.Build();
        return bi;
    }

    /// <summary>Scene 2 (Level 2 saya): Airport Building 03 (gedung segi delapan), skala dunia 1.</summary>
    public static BuildingInterior CreateForAirport()
    {
        if (!Application.isPlaying) return null;
        if (GameObject.Find("BuildingInterior_Terminal3") != null) return null;

        Vector3 center = new Vector3(90.1f, 0f, 112.6f);
        float radius = 25.8f;
        GameObject b3 = GameObject.Find("Airport_Building03");
        if (b3 != null)
        {
            Renderer r = b3.GetComponent<Renderer>();
            if (r != null)
            {
                center = new Vector3(r.bounds.center.x, 0f, r.bounds.center.z);
                radius = Mathf.Max(r.bounds.extents.x, r.bounds.extents.z);
            }
        }

        // Pilih sisi bangunan yang lapang (tanpa gangway/pagar): timur, selatan, barat, utara, lalu diagonal.
        Vector3[] dirs =
        {
            new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(-1, 0, 0), new Vector3(0, 0, 1),
            new Vector3(1, 0, 1).normalized, new Vector3(1, 0, -1).normalized,
            new Vector3(-1, 0, -1).normalized, new Vector3(-1, 0, 1).normalized,
        };
        Vector3 chosen = dirs[0];
        for (int i = 0; i < dirs.Length; i++)
        {
            Vector3 p = center + dirs[i] * (radius + 4.5f) + Vector3.up * 1.2f;
            if (!Physics.CheckSphere(p, 2.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                chosen = dirs[i];
                break;
            }
        }

        var go = new GameObject("BuildingInterior_Terminal3");
        var bi = go.AddComponent<BuildingInterior>();
        bi.buildingName = "Gedung Terminal 3";
        bi.worldScale = 1f;
        bi.doorFacing = chosen;
        bi.doorOutside = center + chosen * (radius + 3.2f);
        bi.roomCenter = new Vector3(700f, 0f, 130f);
        bi.halfSize = new Vector2(13f, 10f);
        bi.roomHeight = 7f;
        bi.Build();
        return bi;
    }

    // ===================================================================== BUILD

    private static Shader FindLitShader()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        return sh;
    }

    private Material MakeMat(Color color, float emission = 0f)
    {
        var m = new Material(FindLitShader());
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (emission > 0f && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * emission);
        }
        materials.Add(m);
        return m;
    }

    private GameObject Cube(string name, Vector3 centerPos, Vector3 size, Material mat, bool collider = true, Transform parent = null)
    {
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.name = name;
        c.transform.SetParent(parent != null ? parent : transform, false);
        c.transform.position = centerPos;
        c.transform.localScale = size;
        c.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider)
        {
            Collider col = c.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
        return c;
    }

    private TextMesh Label(string text, Vector3 pos, Color color, float size)
    {
        var go = new GameObject("Label_" + text);
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = size;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null)
        {
            tm.font = f;
            go.GetComponent<MeshRenderer>().sharedMaterial = f.material;
        }
        return tm;
    }

    private void AddLight(Vector3 pos, float range, float intensity, Color color)
    {
        var go = new GameObject("InteriorLight");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = range;
        l.intensity = intensity;
        l.color = color;
        l.shadows = LightShadows.None;
    }

    private void Build()
    {
        float s = worldScale;
        float t = 0.4f * s;
        float hx = halfSize.x, hz = halfSize.y, h = roomHeight;
        Vector3 c = roomCenter;

        Color wallCream = new Color(0.97f, 0.89f, 0.74f);
        Color accent = new Color(0.20f, 0.62f, 0.66f);
        Color wood = new Color(0.58f, 0.38f, 0.22f);
        Color amber = new Color(1.0f, 0.69f, 0.12f);

        Material wallMat = MakeMat(wallCream);
        Material accentMat = MakeMat(accent);
        Material floorMat = MakeMat(new Color(0.52f, 0.36f, 0.24f));
        Material ceilMat = MakeMat(new Color(0.96f, 0.96f, 0.94f));
        Material woodMat = MakeMat(wood);
        Material woodTopMat = MakeMat(new Color(0.78f, 0.56f, 0.34f));
        Material benchMat = MakeMat(new Color(0.24f, 0.66f, 0.70f));
        Material potMat = MakeMat(new Color(0.80f, 0.40f, 0.26f));
        Material leafMat = MakeMat(new Color(0.30f, 0.72f, 0.38f));
        Material rugMat = MakeMat(new Color(0.62f, 0.30f, 0.28f));
        Material posterMat = MakeMat(new Color(1.0f, 0.80f, 0.30f));
        Material poster2Mat = MakeMat(new Color(0.40f, 0.66f, 0.95f));
        Material glowMat = MakeMat(amber, 2.2f);

        // Lantai (permukaan atas di y = roomCenter.y) & atap
        Cube("Interior_Floor", c + new Vector3(0f, -t * 0.5f, 0f), new Vector3(2f * hx + 2f * t, t, 2f * hz + 2f * t), floorMat);
        Cube("Interior_Ceiling", c + new Vector3(0f, h + t * 0.5f, 0f), new Vector3(2f * hx + 2f * t, t, 2f * hz + 2f * t), ceilMat);

        // Dinding (dinding belakang berwarna aksen). Pintu ada di dinding selatan (z negatif).
        Cube("Interior_Wall_North", c + new Vector3(0f, h * 0.5f, hz + t * 0.5f), new Vector3(2f * hx + 2f * t, h, t), accentMat);
        Cube("Interior_Wall_South", c + new Vector3(0f, h * 0.5f, -hz - t * 0.5f), new Vector3(2f * hx + 2f * t, h, t), wallMat);
        Cube("Interior_Wall_East", c + new Vector3(hx + t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, 2f * hz), wallMat);
        Cube("Interior_Wall_West", c + new Vector3(-hx - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, 2f * hz), wallMat);

        // Karpet, meja kerja, bangku, tanaman
        Cube("Interior_Floor_Rug", c + new Vector3(0f, 0.012f * s, -0.1f * hz), new Vector3(hx * 0.8f, 0.024f * s, hz * 0.7f), rugMat, false);

        float deskW = Mathf.Min(hx * 1.2f, 3.6f * s * 2.2f);
        Vector3 deskC = c + new Vector3(0f, 0.45f * s, hz - 1.0f * s * 1.4f);
        Cube("Interior_Table_Desk", deskC, new Vector3(deskW, 0.9f * s, 1.1f * s), woodMat);
        Cube("Interior_Table_DeskTop", deskC + new Vector3(0f, 0.47f * s, 0f), new Vector3(deskW + 0.1f * s, 0.07f * s, 1.25f * s), woodTopMat, false);

        float benchX = hx - 0.75f * s * 1.1f;
        Cube("Interior_Bench_West", c + new Vector3(-benchX, 0.25f * s, -0.1f * hz), new Vector3(0.7f * s, 0.5f * s, Mathf.Min(hz * 0.9f, 2.6f * s * 1.6f)), benchMat);
        Cube("Interior_Bench_East", c + new Vector3(benchX, 0.25f * s, -0.1f * hz), new Vector3(0.7f * s, 0.5f * s, Mathf.Min(hz * 0.9f, 2.6f * s * 1.6f)), benchMat);

        foreach (float sx in new[] { -1f, 1f })
        {
            Vector3 pc = c + new Vector3(sx * (hx - 0.6f * s), 0f, hz - 0.65f * s);
            Cube("Interior_Planter_Pot", pc + new Vector3(0f, 0.3f * s, 0f), new Vector3(0.7f * s, 0.6f * s, 0.7f * s), potMat);
            var leaf = Cube("Interior_Planter_Leaf", pc + new Vector3(0f, 0.85f * s, 0f), new Vector3(0.6f * s, 0.6f * s, 0.6f * s), leafMat, false);
            leaf.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
        }

        // Poster di dinding samping & lampu
        Cube("Interior_Poster_West", c + new Vector3(-hx + 0.03f * s, h * 0.55f, 0.2f * hz), new Vector3(0.06f * s, 1.1f * s, 1.6f * s), posterMat, false);
        Cube("Interior_Poster_East", c + new Vector3(hx - 0.03f * s, h * 0.55f, 0.2f * hz), new Vector3(0.06f * s, 1.1f * s, 1.6f * s), poster2Mat, false);
        AddLight(c + new Vector3(0f, h * 0.82f, 0f), Mathf.Max(hx, hz) * 3.2f, 2.4f, new Color(1f, 0.94f, 0.82f));
        AddLight(c + new Vector3(0f, h * 0.7f, hz * 0.6f), Mathf.Max(hx, hz) * 2.2f, 1.6f, new Color(1f, 0.9f, 0.75f));

        // Papan nama di dinding belakang
        Label("PUSAT PENGIRIMAN", c + new Vector3(0f, h * 0.72f, hz - 0.03f * s), new Color(1f, 0.93f, 0.7f), LabelSize()).transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        // Pintu keluar (dinding selatan) + lingkaran bercahaya
        insideSpawn = c + new Vector3(0f, 0f, -hz + Mathf.Max(1.5f * s, 0.5f * hz));
        exitPoint = c + new Vector3(0f, 0f, -hz + 0.9f * s);
        Cube("Interior_ExitFrame_L", c + new Vector3(-0.95f * s, 1.1f * s, -hz + 0.04f * s), new Vector3(0.14f * s, 2.2f * s, 0.08f * s), glowMat, false);
        Cube("Interior_ExitFrame_R", c + new Vector3(0.95f * s, 1.1f * s, -hz + 0.04f * s), new Vector3(0.14f * s, 2.2f * s, 0.08f * s), glowMat, false);
        Cube("Interior_ExitFrame_T", c + new Vector3(0f, 2.2f * s, -hz + 0.04f * s), new Vector3(2.04f * s, 0.14f * s, 0.08f * s), glowMat, false);
        insideRing = MakeRing("Interior_ExitRing", exitPoint + new Vector3(0f, 0.02f * s, 0f), 0.75f * s, glowMat);
        insideLabel = Label("KELUAR", c + new Vector3(0f, 2.65f * s, -hz + 0.1f * s), amber, LabelSize());
        insideLabel.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        pakBeniInsidePos = c + new Vector3(0f, 0f, hz - 2.9f * s);

        // Gerbang masuk di luar bangunan
        Vector3 f = doorFacing.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, f).normalized;
        Quaternion face = Quaternion.LookRotation(f, Vector3.up);
        float aw = 1.35f * s * 1.4f;
        float ah = 2.6f * s * 1.4f;
        var gate = new GameObject("Entrance_Gate");
        gate.transform.SetParent(transform, false);
        gate.transform.position = doorOutside;
        gate.transform.rotation = face;
        Cube("Gate_Post_L", doorOutside - right * aw + Vector3.up * ah * 0.5f, new Vector3(0.18f * s * 1.4f, ah, 0.18f * s * 1.4f), glowMat, false, gate.transform);
        Cube("Gate_Post_R", doorOutside + right * aw + Vector3.up * ah * 0.5f, new Vector3(0.18f * s * 1.4f, ah, 0.18f * s * 1.4f), glowMat, false, gate.transform);
        var lintel = Cube("Gate_Lintel", doorOutside + Vector3.up * ah, new Vector3(aw * 2f + 0.2f * s * 1.4f, 0.2f * s * 1.4f, 0.2f * s * 1.4f), glowMat, false, gate.transform);
        lintel.transform.rotation = face;
        outsideRing = MakeRing("Entrance_Ring", doorOutside + Vector3.up * (0.02f * s), 0.95f * s * 1.2f, glowMat);
        outsideLabel = Label("MASUK " + buildingName.ToUpperInvariant(), doorOutside + Vector3.up * (ah + 0.5f * s * 1.4f), amber, LabelSize());
        AddLight(doorOutside + Vector3.up * (ah * 0.8f) + f * (0.6f * s), 6f * s * 1.4f, Mathf.Lerp(0.6f, 2.0f, Mathf.Clamp01(worldScale)), new Color(1f, 0.78f, 0.35f));
    }

    private float LabelSize()
    {
        return 0.012f + 0.09f * worldScale;
    }

    private Transform MakeRing(string name, Vector3 pos, float radius, Material mat)
    {
        GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d.name = name;
        d.transform.SetParent(transform, false);
        d.transform.position = pos;
        d.transform.localScale = new Vector3(radius * 2f, 0.004f * Mathf.Max(0.1f, worldScale) * 5f, radius * 2f);
        d.GetComponent<Renderer>().sharedMaterial = mat;
        Collider col = d.GetComponent<Collider>();
        if (col != null) Destroy(col);
        return d.transform;
    }

    // ===================================================================== RUNTIME

    private void OnEnable()
    {
        if (isHub) Hub = this;
    }

    private void OnDestroy()
    {
        if (Hub == this) Hub = null;
        if (bound && DeliveryGameManager.Instance != null)
        {
            DeliveryGameManager.Instance.OnMissionChanged -= OnMissionChanged;
        }
    }

    private void TryBind()
    {
        if (bound || DeliveryGameManager.Instance == null) return;
        DeliveryGameManager.Instance.OnMissionChanged += OnMissionChanged;
        bound = true;
        OnMissionChanged(DeliveryGameManager.Instance.CurrentState);
    }

    private void FindPakBeni()
    {
        if (pakBeni != null) return;
        foreach (var r in Object.FindObjectsByType<DeliveryRecipient>(FindObjectsInactive.Include))
        {
            if (r != null && r.recipientType == DeliveryRecipient.RecipientType.PakBeni)
            {
                pakBeni = r;
                break;
            }
        }
        if (pakBeni != null && !pakBeniHomeSaved)
        {
            pakBeniHomePos = pakBeni.transform.position;
            pakBeniHomeRot = pakBeni.transform.rotation;
            pakBeniHomeSaved = true;
        }
    }

    private void OnMissionChanged(DeliveryMissionState state)
    {
        if (!isHub) return;
        SetPakBeniInside(state == DeliveryMissionState.Final_ReturnToHub);
    }

    /// <summary>Memindahkan Pak Beni ke dalam ruangan (lapor akhir) atau mengembalikannya ke posisi awal.</summary>
    public void SetPakBeniInside(bool inside)
    {
        FindPakBeni();
        if (pakBeni == null || pakBeniInside == inside) return;
        pakBeniInside = inside;

        if (inside)
        {
            pakBeni.transform.position = pakBeniInsidePos;
            pakBeni.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // menghadap pintu masuk (selatan)
        }
        else if (pakBeniHomeSaved)
        {
            pakBeni.transform.position = pakBeniHomePos;
            pakBeni.transform.rotation = pakBeniHomeRot;
        }
    }

    /// <summary>Radar: selama Pak Beni di dalam gedung dan Rian di luar, arahkan ke pintu masuk.</summary>
    public static bool TryGetRadarPosition(DeliveryRecipient recipient, Vector3 playerPos, out Vector3 position)
    {
        position = Vector3.zero;
        BuildingInterior hub = Hub;
        if (hub == null || !hub.pakBeniInside || recipient == null || recipient != hub.pakBeni) return false;
        if (hub.IsInside(playerPos)) return false;
        position = hub.doorOutside;
        return true;
    }

    public bool IsInside(Vector3 pos)
    {
        float s = Mathf.Max(0.1f, worldScale);
        return Mathf.Abs(pos.x - roomCenter.x) <= halfSize.x + 0.5f * s &&
               Mathf.Abs(pos.z - roomCenter.z) <= halfSize.y + 0.5f * s &&
               pos.y > roomCenter.y - 3f * s && pos.y < roomCenter.y + roomHeight + 3f * s;
    }

    private void Update()
    {
        TryBind();

        // Label menghadap kamera
        Camera cam = Camera.main;
        if (cam != null)
        {
            Billboard(outsideLabel, cam);
            Billboard(insideLabel, cam);
        }

        if (player == null)
        {
            player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;
        }

        DeliveryHUD hud = DeliveryHUD.Instance;
        if (hud == null || hud.IsMainMenuOpen || transitioning || KurirIntroCutscene.IsPlaying || NPCDialogueOverlay.IsDialogueActive) return;

        float s = Mathf.Max(0.1f, worldScale);
        float radius = Mathf.Max(0.32f, 2.6f * s);
        Vector3 p = player.transform.position;

        if (!IsInside(p))
        {
            if (FlatDistance(p, doorOutside) <= radius)
            {
                hud.RegisterNearbyInteractable("Masuk " + buildingName, () => StartCoroutine(Travel(true)), 6);
            }
        }
        else if (FlatDistance(p, exitPoint) <= radius)
        {
            hud.RegisterNearbyInteractable("Keluar dari " + buildingName, () => StartCoroutine(Travel(false)), 6);
        }
    }

    private static void Billboard(TextMesh tm, Camera cam)
    {
        if (tm == null) return;
        Vector3 dir = tm.transform.position - cam.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) tm.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // ===================================================================== TRANSISI

    private IEnumerator Travel(bool enter)
    {
        if (transitioning || player == null) yield break;
        transitioning = true;
        GameAudioManager.Instance?.PlayWhoosh(0.35f);

        yield return Fade(1f, 0.22f);

        Vector3 dest;
        Quaternion rot;
        if (enter)
        {
            dest = insideSpawn;
            rot = Quaternion.LookRotation(Vector3.forward, Vector3.up); // menghadap ke meja Pak Beni
        }
        else
        {
            dest = doorOutside + doorFacing.normalized * (1.1f * worldScale);
            rot = Quaternion.LookRotation(doorFacing.normalized, Vector3.up);
        }
        player.transform.SetPositionAndRotation(dest, rot);

        Camera cam = Camera.main;
        ThirdPersonCamera tpc = cam != null ? cam.GetComponent<ThirdPersonCamera>() : null;
        if (tpc != null)
        {
            // Di dalam ruangan kamera dirapatkan agar tidak tertahan dinding; dipulihkan saat keluar
            if (enter)
            {
                if (savedCamDistance < 0f) { savedCamDistance = tpc.distance; savedCamHeight = tpc.height; }
                // jarak kamera dibatasi agar tetap berada di dalam ruangan di belakang Rian
                float roomBack = (insideSpawn.z - (roomCenter.z - halfSize.y)) - 0.8f * worldScale;
                tpc.distance = Mathf.Clamp(savedCamDistance * 0.55f, savedCamDistance * 0.2f, Mathf.Max(savedCamDistance * 0.2f, roomBack));
                tpc.height = savedCamHeight * 0.7f;
            }
            else if (savedCamDistance >= 0f)
            {
                tpc.distance = savedCamDistance;
                tpc.height = savedCamHeight;
                savedCamDistance = -1f;
            }
            tpc.SnapToTarget();
        }

        yield return null;
        yield return Fade(0f, 0.22f);
        transitioning = false;
    }

    private static void EnsureFader()
    {
        if (fadeImage != null) return;
        fadeCanvas = new GameObject("BuildingFadeCanvas");
        var canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4500;
        var go = new GameObject("Fade", typeof(RectTransform));
        go.transform.SetParent(fadeCanvas.transform, false);
        fadeImage = go.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static IEnumerator Fade(float target, float duration)
    {
        EnsureFader();
        float start = fadeImage.color.a;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
            if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, target);
    }
}
