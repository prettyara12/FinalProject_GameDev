using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// VendingMachine: setiap vending machine di jalan bisa dipakai Rian untuk membeli minuman dan memulihkan rasa haus.
/// Berdirilah dekat mesin yang diinginkan lalu tekan [E] untuk membuka menu pilihan minuman.
/// Tiap varian (A / B / C) menawarkan tiga minuman yang berbeda; membeli memakan sedikit waktu pengiriman.
/// Komponen dipasang otomatis ke semua objek bermesh "VendingMachine" saat scene dimuat.
/// </summary>
public class VendingMachine : MonoBehaviour
{
    public struct Drink
    {
        public string name;
        public float hydration;
        public float timeCost;
        public Drink(string name, float hydration, float timeCost) { this.name = name; this.hydration = hydration; this.timeCost = timeCost; }
    }

    private static readonly Drink[] menuA =
    {
        new Drink("Air Mineral", 45f, 2f),
        new Drink("Es Teh", 55f, 3f),
        new Drink("Air Dingin Besar", 70f, 4f),
    };
    private static readonly Drink[] menuB =
    {
        new Drink("Air Mineral", 45f, 2f),
        new Drink("Jus Jeruk", 60f, 3f),
        new Drink("Air Kelapa", 80f, 4f),
    };
    private static readonly Drink[] menuC =
    {
        new Drink("Air Mineral", 45f, 2f),
        new Drink("Es Teh", 55f, 3f),
        new Drink("Minuman Isotonik", 100f, 5f),
    };

    public char variant = 'A';
    private PlayerController player;
    private float radius;

    private static bool hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        hooked = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        if (!hooked)
        {
            hooked = true;
            SceneManager.sceneLoaded += (s, m) => EnsureAll();
        }
        EnsureAll();
    }

    /// <summary>Memasang VendingMachine + collider pada semua mesh vending di scene aktif.</summary>
    public static int EnsureAll()
    {
        int count = 0;
        foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude))
        {
            if (mf == null || mf.sharedMesh == null) continue;
            if (!mf.sharedMesh.name.ToLowerInvariant().Contains("vendingmachine") &&
                !mf.gameObject.name.ToLowerInvariant().Contains("vendingmachine")) continue;

            GameObject go = mf.gameObject;
            if (go.GetComponent<VendingMachine>() != null) continue;

            VendingMachine vm = go.AddComponent<VendingMachine>();
            string n = go.name.ToUpperInvariant();
            if (n.Contains("_B")) vm.variant = 'B';
            else if (n.Contains("_C")) vm.variant = 'C';
            else vm.variant = 'A';

            // Mesin harus padat: Rian tidak boleh menembusnya
            if (go.GetComponent<Collider>() == null)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.center = mf.sharedMesh.bounds.center;
                box.size = mf.sharedMesh.bounds.size;
            }
            count++;
        }
        return count;
    }

    public Drink[] Menu
    {
        get { return variant == 'B' ? menuB : (variant == 'C' ? menuC : menuA); }
    }

    private void Update()
    {
        if (VendingMenu.IsOpen) return;

        if (player == null)
        {
            player = Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            float scale = Mathf.Max(0.1f, Mathf.Abs(player.transform.lossyScale.y));
            radius = Mathf.Max(0.3f, 2.4f * scale);
        }

        DeliveryHUD hud = DeliveryHUD.Instance;
        if (hud == null || hud.IsMainMenuOpen || KurirIntroCutscene.IsPlaying || NPCDialogueOverlay.IsDialogueActive) return;

        Vector3 a = player.transform.position; a.y = 0f;
        Vector3 b = transform.position; b.y = 0f;
        float dist = Vector3.Distance(a, b);
        if (dist > radius) return;

        // Beberapa mesin berdekatan: yang paling dekat dengan Rian mendapat prioritas tertinggi
        int priority = 20 + Mathf.RoundToInt((1f - dist / radius) * 60f);
        hud.RegisterNearbyInteractable("Beli minuman di Vending " + variant, () => VendingMenu.Open(this, player), priority);
    }
}

/// <summary>Menu pilihan minuman vending machine (dibuat saat dibutuhkan, gaya HUD baru).</summary>
public class VendingMenu : MonoBehaviour
{
    private static VendingMenu instance;
    public static bool IsOpen { get; private set; }

    private GameObject root;
    private Font font;
    private VendingMachine machine;
    private PlayerController player;
    private Text titleText;
    private readonly List<Text> nameTexts = new List<Text>();
    private readonly List<Text> effectTexts = new List<Text>();
    private readonly List<Text> costTexts = new List<Text>();
    private Image hydrationFill;
    private Text hydrationText;
    private bool prevCursorVisible;
    private CursorLockMode prevLock;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        IsOpen = false;
    }

    public static void Open(VendingMachine m, PlayerController p)
    {
        if (IsOpen || m == null || p == null) return;
        if (instance == null)
        {
            var go = new GameObject("VendingMenuSystem");
            instance = go.AddComponent<VendingMenu>();
            instance.Build();
        }
        instance.Show(m, p);
    }

    public static void CloseMenu()
    {
        if (instance != null) instance.Hide();
    }

    private void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#if UNITY_EDITOR
        Font roboto = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/Develop 3D Mobile Games in Unity/Assets/MobileUI/fonts/Roboto-Bold.ttf");
        if (roboto != null) font = roboto;
#endif
        DeliveryHUD.EnsureEventSystem();

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1050;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        root = new GameObject("VendingMenuRoot", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        RectTransform rrt = (RectTransform)root.transform;
        HudStyle.Stretch(rrt);

        Color ink = HudStyle.Ink;
        Image dim = HudStyle.AddImage(rrt, "Dim", null, new Color(ink.r, ink.g, ink.b, 0.55f), false, true);
        HudStyle.Stretch(dim.rectTransform);

        Vector2 C = new Vector2(0.5f, 0.5f);
        Vector2 TC = new Vector2(0.5f, 1f);
        const float W = 560f, H = 400f;
        RectTransform card = HudStyle.NewRect("Card", rrt);
        HudStyle.Place(card, C, C, Vector2.zero, new Vector2(W, H));
        Image shadow = HudStyle.AddImage(card, "Shadow", HudStyle.Glow(20f, 26f), new Color(0f, 0f, 0f, 0.55f));
        HudStyle.Stretch(shadow.rectTransform, -26f, -34f, -26f, -18f);
        Image bg = HudStyle.AddPanel(card, "Panel", 20f, HudStyle.PanelSolid, HudStyle.Stroke);
        HudStyle.Stretch(bg.rectTransform);
        Transform cp = bg.transform;

        Image dropIcon = HudStyle.AddImage(cp, "DropIcon", HudStyle.Icon("drop"), HudStyle.Teal, false);
        HudStyle.Place(dropIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(26f, 26f));
        titleText = HudStyle.AddLabel(cp, "Title", "Vending Machine", 26, HudStyle.TextPrimary, TextAnchor.MiddleLeft, font);
        HudStyle.Place(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -26f), new Vector2(400f, 34f));
        Text sub = HudStyle.AddLabel(cp, "Sub", "Pilih minuman untuk menghilangkan rasa haus", 13, HudStyle.TextSecondary, TextAnchor.UpperLeft, font, FontStyle.Normal);
        HudStyle.Place(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(64f, -62f), new Vector2(440f, 20f));

        // Tiga baris pilihan
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            float y = -104f - i * 66f;
            Image row = HudStyle.AddPanel(cp, "Row_" + i, 14f, new Color(1f, 1f, 1f, 0.04f), HudStyle.Stroke, 1f, true);
            HudStyle.Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(W - 56f, 56f));
            Button btn = row.gameObject.AddComponent<Button>();
            btn.targetGraphic = row;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.82f, 0.4f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;
            btn.onClick.AddListener(() => Buy(index));

            RectTransform key = HudStyle.AddKeycap(row.transform, "Key", (i + 1).ToString(), new Vector2(30f, 32f), font, 15);
            HudStyle.Place(key, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(30f, 32f));

            Text n = HudStyle.AddLabel(row.transform, "Name", "Minuman", 17, HudStyle.TextPrimary, TextAnchor.MiddleLeft, font);
            HudStyle.Place(n.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(200f, 30f));
            Text e = HudStyle.AddLabel(row.transform, "Effect", "Hidrasi +0", 14, HudStyle.Teal, TextAnchor.MiddleRight, font);
            HudStyle.Place(e.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-110f, 0f), new Vector2(120f, 30f));
            Text c = HudStyle.AddLabel(row.transform, "Cost", "-0 dtk", 13, HudStyle.TextSecondary, TextAnchor.MiddleRight, font);
            HudStyle.Place(c.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(80f, 30f));
            nameTexts.Add(n); effectTexts.Add(e); costTexts.Add(c);
        }

        // Bar hidrasi saat ini
        Text hLabel = HudStyle.AddLabel(cp, "HydrationLabel", "HIDRASI", 11, HudStyle.TextMuted, TextAnchor.MiddleLeft, font);
        HudStyle.Place(hLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 76f), new Vector2(80f, 16f));
        Image track = HudStyle.AddImage(cp, "HydrationTrack", HudStyle.Rounded(4f), new Color(1f, 1f, 1f, 0.08f));
        HudStyle.Place(track.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 58f), new Vector2(W - 150f, 8f));
        hydrationFill = HudStyle.AddImage(track.transform, "Fill", HudStyle.Rounded(4f), HudStyle.Teal);
        hydrationFill.rectTransform.anchorMin = new Vector2(0f, 0f);
        hydrationFill.rectTransform.anchorMax = new Vector2(0f, 1f);
        hydrationFill.rectTransform.pivot = new Vector2(0f, 0.5f);
        hydrationFill.rectTransform.offsetMin = Vector2.zero;
        hydrationFill.rectTransform.offsetMax = new Vector2(100f, 0f);
        hydrationText = HudStyle.AddLabel(cp, "HydrationValue", "100%", 13, HudStyle.TextPrimary, TextAnchor.MiddleRight, font);
        HudStyle.Place(hydrationText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 52f), new Vector2(80f, 20f));

        Button close = HudStyle.AddButton(cp, "Btn_Close", "Tutup  [Esc]", HudStyle.ButtonKind.Secondary, new Vector2(150f, 36f), font, 13, () =>
        {
            GameAudioManager.Instance?.PlayUIClick();
            Hide();
        });
        HudStyle.Place((RectTransform)close.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 78f), new Vector2(150f, 36f));
        // dipindah ke kanan bawah di atas bar agar tidak menimpa pilihan terakhir
        ((RectTransform)close.transform).anchoredPosition = new Vector2(-28f, 12f);
        ((RectTransform)close.transform).sizeDelta = new Vector2(120f, 30f);

        root.SetActive(false);
    }

    private void Show(VendingMachine m, PlayerController p)
    {
        machine = m;
        player = p;
        IsOpen = true;

        titleText.text = "Vending Machine " + m.variant;
        VendingMachine.Drink[] menu = m.Menu;
        for (int i = 0; i < 3; i++)
        {
            nameTexts[i].text = menu[i].name;
            effectTexts[i].text = "Hidrasi +" + menu[i].hydration.ToString("0");
            costTexts[i].text = "-" + menu[i].timeCost.ToString("0") + " dtk";
        }
        RefreshHydration();

        prevCursorVisible = Cursor.visible;
        prevLock = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        player.SetMovementFrozen(true);
        root.SetActive(true);
        GameAudioManager.Instance?.PlayUIClick();
    }

    private void Hide()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (root != null) root.SetActive(false);
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevLock;
        if (player != null) player.SetMovementFrozen(false);
    }

    private void RefreshHydration()
    {
        if (player == null || hydrationFill == null) return;
        float h = player.Hydration01;
        hydrationFill.rectTransform.offsetMax = new Vector2((h * (560f - 150f)), 0f);
        hydrationFill.color = h > 0.4f ? HudStyle.Teal : (h > 0.15f ? HudStyle.Warning : HudStyle.Danger);
        hydrationText.text = Mathf.RoundToInt(h * 100f) + "%";
    }

    private void Buy(int index)
    {
        if (!IsOpen || machine == null || player == null) return;
        VendingMachine.Drink d = machine.Menu[Mathf.Clamp(index, 0, 2)];

        DeliveryGameManager gm = DeliveryGameManager.Instance;
        if (gm != null) gm.SpendTime(d.timeCost);

        player.Drink(d.hydration);
        GameAudioManager.Instance?.PlayDrink();
        DeliveryHUD.Instance?.ShowBumpNotice("Minum " + d.name + "  •  Hidrasi +" + d.hydration.ToString("0") + "  (-" + d.timeCost.ToString("0") + " dtk)");
        Hide();
    }

    private void Update()
    {
        if (!IsOpen) return;
        RefreshHydration();

        if (Input.GetKeyDown(KeyCode.Escape)) { Hide(); return; }
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) Buy(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) Buy(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) Buy(2);
    }
}
