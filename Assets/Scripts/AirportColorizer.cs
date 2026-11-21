using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AirportColorizer: memberi warna (livery) pada pesawat bandara Level 2 dan memperbaiki objek tanpa tekstur.
/// Memakai material/tekstur bawaan project (SimpleAirport) yang diberi tint warna, disimpan sebagai aset di
/// Assets/Materials/Airport sehingga tersimpan di scene. Hanya berjalan di Editor (idempotent).
/// </summary>
public static class AirportColorizer
{
#if UNITY_EDITOR
    private const string BaseMaterialPath =
        "Assets/Develop 3D Mobile Games in Unity/Assets/SimpleWorldVol1/SimpleAirport/_Materials/SimpleAirport.mat";
    private const string OutputFolder = "Assets/Materials/Airport";

    // Warna livery (tint dikalikan dengan palet abu-abu pada tekstur SimpleAirport)
    private static readonly Color[] airlinerColors =
    {
        new Color(0.38f, 0.68f, 1.00f), // biru langit
        new Color(1.00f, 0.45f, 0.40f), // merah koral
        new Color(0.45f, 0.90f, 0.60f), // hijau mint
        new Color(0.80f, 0.62f, 1.00f), // ungu
    };
    private static readonly Color[] jetColors =
    {
        new Color(1.00f, 0.72f, 0.30f), // oranye
        new Color(0.35f, 0.90f, 0.90f), // teal
        new Color(1.00f, 0.90f, 0.40f), // kuning
        new Color(1.00f, 0.55f, 0.75f), // merah muda
        new Color(0.55f, 0.80f, 1.00f), // biru muda
    };
    private static readonly Color[] smallPlaneColors =
    {
        new Color(1.00f, 0.40f, 0.35f),
        new Color(1.00f, 0.88f, 0.35f),
        new Color(0.40f, 0.70f, 1.00f),
        new Color(0.50f, 0.90f, 0.55f),
    };
    private static readonly Color[] fighterColors =
    {
        new Color(0.62f, 0.78f, 0.50f), // hijau zaitun
        new Color(0.95f, 0.82f, 0.58f), // pasir
        new Color(0.58f, 0.68f, 0.90f), // biru baja
    };

    /// <summary>Mewarnai pesawat dan memperbaiki material kosong. Mengembalikan jumlah renderer yang diubah.</summary>
    public static int Apply()
    {
        Material baseMat = AssetDatabase.LoadAssetAtPath<Material>(BaseMaterialPath);
        if (baseMat == null)
        {
            Debug.LogWarning("[AirportColorizer] Material dasar SimpleAirport tidak ditemukan: " + BaseMaterialPath);
            return 0;
        }

        EnsureFolder();
        int changed = 0;
        int airlinerIdx = 2, jetIdx = 0, smallIdx = 0, fighterIdx = 0;

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        // Urutkan agar pemilihan warna stabil antar eksekusi
        System.Array.Sort(renderers, (a, b) => string.CompareOrdinal(Path(a.transform), Path(b.transform)));

        var rootColor = new System.Collections.Generic.Dictionary<Transform, Color>();
        Texture2D paletteTex = LoadPaletteTexture(baseMat);

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            // 1. Perbaiki material kosong / bawaan tanpa tekstur
            Material[] mats = r.sharedMaterials;
            bool fixedMissing = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                bool missing = m == null || m.name == "Default-Diffuse" || m.name == "Default-Material" ||
                               (m.shader != null && m.shader.name.Contains("Hidden/InternalErrorShader"));
                bool plainLit = m != null && m.name == "Lit" && AssetDatabase.GetAssetPath(m).StartsWith("Packages/");
                if (missing || plainLit)
                {
                    mats[i] = baseMat;
                    fixedMissing = true;
                }
            }
            if (fixedMissing)
            {
                r.sharedMaterials = mats;
                EditorUtility.SetDirty(r);
                changed++;
            }

            // 2. Livery pesawat
            Transform root = FindAircraftRoot(r.transform, out string kind);
            if (root == null) continue;
            if (kind == "flying1" || kind == "flying2")
            {
                // warna tetap: biru untuk pesawat 1, merah untuk pesawat 2
            }

            Color tint;
            if (!rootColor.TryGetValue(root, out tint))
            {
                switch (kind)
                {
                    case "flying1": tint = airlinerColors[0]; break;
                    case "flying2": tint = new Color(1.00f, 0.80f, 0.30f); break; // kuning keemasan
                    case "airliner": tint = airlinerColors[airlinerIdx++ % airlinerColors.Length]; break;
                    case "jet": tint = jetColors[jetIdx++ % jetColors.Length]; break;
                    case "small": tint = smallPlaneColors[smallIdx++ % smallPlaneColors.Length]; break;
                    default: tint = fighterColors[fighterIdx++ % fighterColors.Length]; break;
                }
                // Model yang UV-nya jatuh pada sel palet gelap (mis. Plane02) dikalikan lebih terang
                // agar warna livery tetap tampak cerah, bukan hitam.
                tint = BoostForMesh(root, tint, paletteTex);
                rootColor[root] = tint;
            }

            Material[] cur = r.sharedMaterials;
            bool any = false;
            for (int i = 0; i < cur.Length; i++)
            {
                Material m = cur[i];
                if (m == null) continue;
                bool isBase = m == baseMat || m.name == "SimpleAirport";
                bool isLivery = m.name.StartsWith("Livery_");
                if (!isBase && !isLivery) continue;

                Material livery = GetOrCreateLivery(baseMat, tint);
                if (cur[i] != livery)
                {
                    cur[i] = livery;
                    any = true;
                }
            }
            if (any)
            {
                r.sharedMaterials = cur;
                EditorUtility.SetDirty(r);
                changed++;
            }
        }

        AssetDatabase.SaveAssets();
        return changed;
    }

    /// <summary>Mencari akar pesawat. kind: flying1, flying2, airliner, jet, small, fighter. Mengembalikan null jika bukan pesawat.</summary>
    private static Transform FindAircraftRoot(Transform t, out string kind)
    {
        kind = null;
        Transform cur = t;
        for (int depth = 0; cur != null && depth < 4; depth++, cur = cur.parent)
        {
            string n = cur.name.ToLowerInvariant();
            if (n.StartsWith("plane01_flying")) { kind = "flying1"; return cur; }
            if (n.StartsWith("plane02_flying")) { kind = "flying2"; return cur; }
            if (n.StartsWith("small_plane")) { kind = "small"; return cur; }
            if (n.StartsWith("fighter_jet")) { kind = "fighter"; return cur; }
            if (n.StartsWith("jet")) { kind = "jet"; return cur; }
            if (n.StartsWith("plane0") && !n.Contains("cart")) { kind = "airliner"; return cur; }
        }
        return null;
    }

    private static Texture2D LoadPaletteTexture(Material baseMat)
    {
        Texture src = baseMat.HasProperty("_BaseMap") ? baseMat.GetTexture("_BaseMap") : baseMat.mainTexture;
        string texPath = src != null ? AssetDatabase.GetAssetPath(src) : null;
        if (string.IsNullOrEmpty(texPath) || !System.IO.File.Exists(texPath)) return null;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(System.IO.File.ReadAllBytes(texPath));
        return tex;
    }

    /// <summary>
    /// Menghitung tint material agar warna akhir badan pesawat mendekati warna livery yang diinginkan.
    /// Tint = warna yang diinginkan / rata-rata warna sel palet yang dipakai mesh (selain sel sangat gelap
    /// seperti jendela & roda), sehingga model yang UV-nya jatuh pada sel gelap atau biru (mis. Plane02)
    /// tetap tampil berwarna cerah, bukan hitam.
    /// </summary>
    private static Color BoostForMesh(Transform root, Color desired, Texture2D palette)
    {
        if (palette == null) return desired;

        Vector3 sum = Vector3.zero; int count = 0;
        Vector3 sumAll = Vector3.zero; int countAll = 0;
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;
            if (mf.name.ToLowerInvariant().Contains("wheel")) continue;
            Vector2[] uvs = mesh.uv;
            for (int i = 0; i < uvs.Length; i++)
            {
                Color c = palette.GetPixelBilinear(uvs[i].x, uvs[i].y);
                float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                sumAll += new Vector3(c.r, c.g, c.b); countAll++;
                if (lum >= 0.12f) { sum += new Vector3(c.r, c.g, c.b); count++; }
            }
        }
        if (countAll == 0) return desired;

        Vector3 avg = count > 0 ? sum / count : sumAll / countAll;
        const float target = 1.0f; // kecerahan akhir livery yang diinginkan
        Color wanted = new Color(desired.r * target, desired.g * target, desired.b * target, 1f);
        return new Color(
            Mathf.Min(5f, wanted.r / Mathf.Max(0.10f, avg.x)),
            Mathf.Min(5f, wanted.g / Mathf.Max(0.10f, avg.y)),
            Mathf.Min(5f, wanted.b / Mathf.Max(0.10f, avg.z)), 1f);
    }

    private static Material GetOrCreateLivery(Material baseMat, Color tint)
    {
        string hex = Mathf.RoundToInt(tint.r * 100f) + "_" + Mathf.RoundToInt(tint.g * 100f) + "_" + Mathf.RoundToInt(tint.b * 100f);
        string path = OutputFolder + "/Livery_" + hex + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(baseMat);
        mat.name = "Livery_" + hex;
        Color c = new Color(tint.r, tint.g, tint.b, 1f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets/Materials", "Airport");
    }

    private static string Path(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
#else
    public static int Apply() { return 0; }
#endif
}
