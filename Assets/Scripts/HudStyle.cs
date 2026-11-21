using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HudStyle: sistem desain UI "Kurir Terakhir" (modern dark-glass + aksen amber).
/// Menyediakan palet warna, sprite prosedural (sudut membulat, bayangan lembut, ikon),
/// serta helper untuk membangun panel, teks, keycap, dan tombol secara konsisten.
/// Semua sprite dibuat saat runtime sehingga tidak butuh aset gambar tambahan.
/// </summary>
public static class HudStyle
{
    // ======================================================================= PALET
    public static readonly Color Ink          = new Color32(0x0A, 0x0E, 0x16, 255);
    public static readonly Color Panel        = new Color(0.050f, 0.066f, 0.098f, 0.88f);
    public static readonly Color PanelSolid   = new Color(0.062f, 0.080f, 0.116f, 0.98f);
    public static readonly Color Stroke       = new Color(1f, 1f, 1f, 0.075f);
    public static readonly Color Chip         = new Color(1f, 1f, 1f, 0.045f);
    public static readonly Color TextPrimary  = new Color32(0xF4, 0xF6, 0xFA, 255);
    public static readonly Color TextSecondary = new Color32(0xA9, 0xB4, 0xC6, 255);
    public static readonly Color TextMuted    = new Color32(0x72, 0x7E, 0x92, 255);
    public static readonly Color Accent       = new Color32(0xFF, 0xB0, 0x20, 255);
    public static readonly Color AccentHover  = new Color32(0xFF, 0xC4, 0x55, 255);
    public static readonly Color AccentPressed = new Color32(0xE0, 0x93, 0x00, 255);
    public static readonly Color OnAccent     = new Color32(0x1B, 0x12, 0x04, 255);
    public static readonly Color Teal         = new Color32(0x3D, 0xD6, 0xC6, 255);
    public static readonly Color Danger       = new Color32(0xFF, 0x5A, 0x5F, 255);
    public static readonly Color Success      = new Color32(0x4A, 0xDE, 0x80, 255);
    public static readonly Color Warning      = new Color32(0xFF, 0xD1, 0x4D, 255);

    // Skala sprite: 2 piksel tekstur per 1 unit canvas (tajam di layar 1080p).
    private const int S = 2;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() { cache.Clear(); }

    // ======================================================================= SPRITE
    private static Sprite Get(string key, Func<Sprite> make)
    {
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
        s = make();
        cache[key] = s;
        return s;
    }

    private static Texture2D NewTex(int w, int h)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    private static float SdRoundBox(float px, float py, float hx, float hy, float r)
    {
        float qx = Mathf.Abs(px) - (hx - r);
        float qy = Mathf.Abs(py) - (hy - r);
        float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
    }

    private static float SdSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
        float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
        float dx = pax - bax * h, dy = pay - bay * h;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static Sprite Make9Slice(Texture2D tex, int n)
    {
        tex.Apply(false, false);
        int b = n / 2 - 1;
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f * S, 0,
            SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    /// <summary>Persegi membulat (9-slice). radius dalam unit canvas.</summary>
    public static Sprite Rounded(float radius)
    {
        return Get("r" + radius, () =>
        {
            float R = radius * S;
            int n = Mathf.CeilToInt(2f * R + 4f); if ((n & 1) == 1) n++;
            var tex = NewTex(n, n);
            var px = new Color32[n * n];
            float h = n * 0.5f - 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = SdRoundBox(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f, h, h, R);
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
                }
            tex.SetPixels32(px);
            return Make9Slice(tex, n);
        });
    }

    /// <summary>Garis tepi membulat (9-slice), tebal stroke dalam unit canvas.</summary>
    public static Sprite RoundedStroke(float radius, float stroke)
    {
        return Get("rs" + radius + "_" + stroke, () =>
        {
            float R = radius * S, W = stroke * S;
            int n = Mathf.CeilToInt(2f * R + 4f); if ((n & 1) == 1) n++;
            var tex = NewTex(n, n);
            var px = new Color32[n * n];
            float h = n * 0.5f - 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = SdRoundBox(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f, h, h, R);
                    float a = Mathf.Clamp01(0.5f - d) - Mathf.Clamp01(0.5f - (d + W));
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
            tex.SetPixels32(px);
            return Make9Slice(tex, n);
        });
    }

    /// <summary>Bayangan lembut (9-slice). Ukuran gambar = ukuran panel + 2*blur.</summary>
    public static Sprite Glow(float radius, float blur)
    {
        return Get("g" + radius + "_" + blur, () =>
        {
            float R = radius * S, B = blur * S;
            int n = Mathf.CeilToInt(2f * (R + B) + 4f); if ((n & 1) == 1) n++;
            var tex = NewTex(n, n);
            var px = new Color32[n * n];
            float h = n * 0.5f - B;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = SdRoundBox(x + 0.5f - n * 0.5f, y + 0.5f - n * 0.5f, h, h, R);
                    float a = Mathf.Clamp01(1f - d / B);
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels32(px);
            return Make9Slice(tex, n);
        });
    }

    /// <summary>Lingkaran penuh (tanpa slicing), diameter ≈ 128 px.</summary>
    public static Sprite Circle()
    {
        return Get("circle", () =>
        {
            int n = 128;
            var tex = NewTex(n, n);
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f, r = c - 1f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        });
    }

    /// <summary>Gradasi horizontal: opak di kiri, transparan di kanan.</summary>
    public static Sprite FadeLeftToRight()
    {
        return Get("fadeH", () =>
        {
            int w = 256;
            var tex = NewTex(w, 1);
            var px = new Color32[w];
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                float a = Mathf.Pow(1f - t, 1.5f);
                px[x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 100f);
        });
    }

    /// <summary>Gradasi vertikal: opak di bawah, transparan di atas.</summary>
    public static Sprite FadeBottomToTop()
    {
        return Get("fadeV", () =>
        {
            int h = 256;
            var tex = NewTex(1, h);
            var px = new Color32[h];
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                px[y] = new Color(1f, 1f, 1f, Mathf.Pow(1f - t, 1.6f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 1, h), new Vector2(0.5f, 0.5f), 100f);
        });
    }

    /// <summary>Ikon prosedural putih 64x64: clock, package, target, chevron, close.</summary>
    public static Sprite Icon(string name)
    {
        return Get("icon_" + name, () =>
        {
            int n = 64;
            var tex = NewTex(n, n);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = IconDistance(name, fx, fy);
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
                }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        });
    }

    // Jarak bertanda (px) untuk tiap ikon; d < 0 berarti di dalam bentuk.
    private static float IconDistance(string name, float x, float y)
    {
        const float line = 3.2f; // setengah tebal garis
        switch (name)
        {
            case "clock":
            {
                float ring = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f)) - 24f) - line;
                float h1 = SdSegment(x, y, 32f, 32f, 32f, 47f) - line;
                float h2 = SdSegment(x, y, 32f, 32f, 43f, 26f) - line;
                return Mathf.Min(ring, Mathf.Min(h1, h2));
            }
            case "package":
            {
                float box = Mathf.Abs(SdRoundBox(x - 32f, y - 31f, 24f, 22f, 5f)) - line;
                float band = SdSegment(x, y, 32f, 53f, 32f, 38f) - line;
                float lid = SdSegment(x, y, 8f, 38f, 56f, 38f) - (line * 0.8f);
                return Mathf.Min(box, Mathf.Min(band, lid));
            }
            case "target":
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f));
                float ring = Mathf.Abs(dist - 22f) - line;
                float dot = dist - 6.5f;
                float t1 = SdSegment(x, y, 32f, 60f, 32f, 46f) - line;
                float t2 = SdSegment(x, y, 32f, 4f, 32f, 18f) - line;
                float t3 = SdSegment(x, y, 4f, 32f, 18f, 32f) - line;
                float t4 = SdSegment(x, y, 60f, 32f, 46f, 32f) - line;
                return Mathf.Min(Mathf.Min(ring, dot), Mathf.Min(Mathf.Min(t1, t2), Mathf.Min(t3, t4)));
            }
            case "chevron":
            {
                float a = SdSegment(x, y, 22f, 12f, 44f, 32f) - 4.2f;
                float b = SdSegment(x, y, 44f, 32f, 22f, 52f) - 4.2f;
                return Mathf.Min(a, b);
            }
            case "close":
            {
                float a = SdSegment(x, y, 16f, 16f, 48f, 48f) - 4f;
                float b = SdSegment(x, y, 16f, 48f, 48f, 16f) - 4f;
                return Mathf.Min(a, b);
            }
            case "minus":
                return SdSegment(x, y, 14f, 32f, 50f, 32f) - 4.4f;
            case "plus":
                return Mathf.Min(SdSegment(x, y, 14f, 32f, 50f, 32f) - 4.4f, SdSegment(x, y, 32f, 14f, 32f, 50f) - 4.4f);
            default:
                return 1f;
        }
    }

    // ======================================================================= RECT HELPERS
    public static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    /// <summary>Anchor tunggal (min=max) dengan pivot, posisi, dan ukuran.</summary>
    public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    /// <summary>Mengisi seluruh parent dengan inset (kiri, bawah, kanan, atas).</summary>
    public static RectTransform Stretch(RectTransform rt, float l = 0f, float b = 0f, float r = 0f, float t = 0f)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        return rt;
    }

    public static Image AddImage(Transform parent, string name, Sprite sprite, Color color, bool sliced = true, bool raycast = false)
    {
        var rt = NewRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = (sprite != null && sliced && sprite.border.x > 0f) ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = raycast;
        return img;
    }

    /// <summary>Panel membulat: fill + garis tepi. Mengembalikan Image fill (ikut menjadi root).</summary>
    public static Image AddPanel(Transform parent, string name, float radius, Color fill, Color stroke, float strokeWidth = 1f, bool raycast = false)
    {
        var img = AddImage(parent, name, Rounded(radius), fill, true, raycast);
        if (stroke.a > 0.001f)
        {
            var ring = AddImage(img.transform, "Stroke", RoundedStroke(radius, strokeWidth), stroke, true, false);
            Stretch(ring.rectTransform);
        }
        return img;
    }

    /// <summary>Bayangan lembut sebagai sibling tepat di belakang target (target harus sudah diposisikan).</summary>
    public static Image AddShadowBehind(RectTransform target, float radius, float blur, Color color, float offsetY = 6f)
    {
        var img = AddImage(target.parent, target.name + "_Shadow", Glow(radius, blur), color, true, false);
        var rt = img.rectTransform;
        rt.anchorMin = target.anchorMin; rt.anchorMax = target.anchorMax; rt.pivot = target.pivot;
        rt.anchoredPosition = target.anchoredPosition + new Vector2(0f, -offsetY);
        rt.sizeDelta = target.sizeDelta + new Vector2(blur * 2f, blur * 2f);
        rt.SetSiblingIndex(target.GetSiblingIndex());
        return img;
    }

    public static Text AddLabel(Transform parent, string name, string text, int size, Color color,
        TextAnchor anchor, Font font, FontStyle style = FontStyle.Bold, bool wrap = false)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    /// <summary>Keycap tombol keyboard bergaya 3D tipis. Mengembalikan RectTransform root.</summary>
    public static RectTransform AddKeycap(Transform parent, string name, string label, Vector2 size, Font font, int fontSize = 14)
    {
        var root = NewRect(name, parent);
        root.sizeDelta = size;
        var edge = AddImage(root, "Edge", Rounded(6f), new Color32(0x8C, 0x93, 0xA3, 255));
        Stretch(edge.rectTransform);
        var face = AddImage(root, "Face", Rounded(6f), new Color32(0xF3, 0xF4, 0xF8, 255));
        Stretch(face.rectTransform, 0f, 3f, 0f, 0f);
        var txt = AddLabel(face.transform, "Label", label, fontSize, new Color32(0x14, 0x19, 0x24, 255), TextAnchor.MiddleCenter, font);
        Stretch(txt.rectTransform);
        return root;
    }

    // ======================================================================= BUTTON
    public enum ButtonKind { Primary, Secondary, Ghost, Success }

    public static Button AddButton(Transform parent, string name, string text, ButtonKind kind, Vector2 size,
        Font font, int fontSize, Action onClick, bool arrow = false)
    {
        float radius = 12f;
        Color normal, hover, pressed, textColor, stroke;
        switch (kind)
        {
            case ButtonKind.Primary:
                normal = Accent; hover = AccentHover; pressed = AccentPressed; textColor = OnAccent; stroke = Color.clear; break;
            case ButtonKind.Success:
                normal = Success; hover = new Color32(0x7B, 0xE8, 0xA0, 255); pressed = new Color32(0x36, 0xB8, 0x66, 255);
                textColor = OnAccent; stroke = Color.clear; break;
            case ButtonKind.Secondary:
                normal = new Color(1f, 1f, 1f, 0.035f); hover = new Color(1f, 1f, 1f, 0.10f); pressed = new Color(1f, 1f, 1f, 0.02f);
                textColor = TextPrimary; stroke = new Color(1f, 1f, 1f, 0.11f); break;
            default:
                normal = new Color(1f, 1f, 1f, 0f); hover = new Color(1f, 1f, 1f, 0.05f); pressed = new Color(1f, 1f, 1f, 0.02f);
                textColor = TextSecondary; stroke = Color.clear; break;
        }

        var img = AddImage(parent, name, Rounded(radius), Color.white, true, true);
        img.rectTransform.sizeDelta = size;
        if (stroke.a > 0.001f)
        {
            var ring = AddImage(img.transform, "Stroke", RoundedStroke(radius, 1f), stroke, true, false);
            Stretch(ring.rectTransform);
        }

        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.1f;
        cb.normalColor = normal; cb.highlightedColor = hover; cb.pressedColor = pressed; cb.selectedColor = normal;
        cb.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.4f);
        btn.colors = cb;
        if (onClick != null) btn.onClick.AddListener(() => onClick());

        var lbl = AddLabel(img.transform, "Text", text, fontSize, textColor, TextAnchor.MiddleCenter, font);
        Stretch(lbl.rectTransform, arrow ? 0f : 0f, 0f, arrow ? 18f : 0f, 0f);

        if (arrow)
        {
            var ic = AddImage(img.transform, "Arrow", Icon("chevron"), textColor, false);
            Place(ic.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(18f, 18f));
        }
        return btn;
    }
}
