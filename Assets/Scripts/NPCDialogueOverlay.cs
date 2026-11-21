using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistem Balon Dialog & Subtitle Overlay Beresolusi Tinggi (Screen-Space Overlay).
/// 1. Balon percakapan melayang tepat di atas kepala karakter di layar (mengikuti pergerakan kepala secara dinamis).
/// 2. Subtitle dialog elegan di bagian bawah layar agar selalu 100% terbaca dengan jelas dalam kondisi apapun.
/// Robust terhadap Unity domain reload & scene transitions (Enter Play Mode Options).
/// </summary>
public class NPCDialogueOverlay : MonoBehaviour
{
    private static NPCDialogueOverlay instance;
    private static bool isQuitting = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticsOnSubsystemRegistration()
    {
        instance = null;
        isQuitting = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStaticsBeforeSceneLoad()
    {
        instance = null;
        isQuitting = false;
    }

    public static NPCDialogueOverlay Instance
    {
        get
        {
            if (instance == null || instance.overheadText == null)
            {
                EnsureInstance();
            }
            return instance;
        }
    }

    private Canvas overlayCanvas;
    private CanvasScaler canvasScaler;

    // Overhead Speech Bubble Elements
    private GameObject overheadBubbleObj;
    private RectTransform overheadBubbleRect;
    private Text overheadText;
    private Image overheadBg;
    private RectTransform arrowRect;

    // Bottom Subtitle Elements
    private GameObject subtitleBarObj;
    private Text subtitleSpeakerText;
    private Text subtitleMessageText;
    private Text subtitleContinuePromptText;

    // Active Dialogue Tracking
    private NPCWalker activeSpeaker;
    private Transform activeSpeakerTransform;
    private float activeSpeakerHeightOffset = 0.20f;
    private string activeSpeakerName;
    private string activeMessage;

    // Base Target Speaker (NPC being talked to)
    private Transform baseNpcTransform;
    private string baseNpcName;
    private float baseHeightOffset = 0.20f;

    // Sequential Dialogue Tracking
    private string[] currentSequentialLines;
    private int currentSequentialIndex = 0;
    private bool isSequentialActive = false;
    private System.Action onSequentialComplete;
    private float sequentialAdvanceCooldown = 0f;

    public static bool IsDialogueActive => instance != null && (instance.isSequentialActive || (instance.subtitleBarObj != null && instance.subtitleBarObj.activeSelf));

    public static void EnsureInstance()
    {
        if (!Application.isPlaying || isQuitting) return;

        if (instance != null && instance.overheadText != null && instance.subtitleMessageText != null)
        {
            return;
        }

        GameObject go = GameObject.Find("NPCDialogueOverlaySystem");
        if (go == null)
        {
            if (isQuitting) return;
            go = new GameObject("NPCDialogueOverlaySystem");
            instance = go.AddComponent<NPCDialogueOverlay>();
        }
        else
        {
            instance = go.GetComponent<NPCDialogueOverlay>();
            if (instance == null) instance = go.AddComponent<NPCDialogueOverlay>();
            if (instance.overheadText == null || instance.subtitleMessageText == null)
            {
                instance.BuildOverlayUI();
            }
        }
    }

    private void Awake()
    {
        isQuitting = false;
        if (instance == null) instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BuildOverlayUI();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private static Font GetBestDialogueFont()
    {
        Font font = null;
#if UNITY_EDITOR
        font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
            "Assets/Develop 3D Mobile Games in Unity/Assets/MobileUI/fonts/Roboto-Bold.ttf");
        if (font == null)
        {
            font = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(
                "Assets/Develop 3D Mobile Games in Unity/Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
        }
#endif
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new string[] { "Segoe UI Bold", "Segoe UI", "Arial Bold", "Arial", "Roboto Bold", "Roboto" }, 32);
        }
        return font;
    }

    public void BuildOverlayUI()
    {
        // 1. Setup ScreenSpaceOverlay Canvas
        overlayCanvas = GetComponent<Canvas>();
        if (overlayCanvas == null) overlayCanvas = gameObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 999; // Prioritas paling atas di layar

        canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null) canvasScaler = gameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1280, 720); // Resolusi 720p HD seimbang
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasScaler.dynamicPixelsPerUnit = 2f; // Supersampling 2x untuk teks tajam tanpa pixelated

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Hapus UI anak lama jika ada
        while (transform.childCount > 0)
        {
            Transform ch = transform.GetChild(0);
            ch.SetParent(null);
            Destroy(ch.gameObject);
        }

        Font mainFont = GetBestDialogueFont();

        // 2. OVERHEAD SPEECH BUBBLE (kaca gelap membulat, mengikuti desain HUD baru)
        overheadBubbleObj = new GameObject("OverheadSpeechBubble", typeof(RectTransform));
        overheadBubbleObj.transform.SetParent(transform, false);

        overheadBubbleRect = (RectTransform)overheadBubbleObj.transform;
        overheadBubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
        overheadBubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        overheadBubbleRect.pivot = new Vector2(0.5f, 0f); // Pivot di bagian bawah tengah balon
        overheadBubbleRect.sizeDelta = new Vector2(440f, 64f);

        overheadBg = overheadBubbleObj.AddComponent<Image>();
        overheadBg.sprite = HudStyle.Rounded(16f);
        overheadBg.type = Image.Type.Sliced;
        overheadBg.color = new Color(0.05f, 0.066f, 0.098f, 0.96f);
        overheadBg.raycastTarget = false;

        Shadow bubbleShadow = overheadBubbleObj.AddComponent<Shadow>();
        bubbleShadow.effectColor = new Color(0f, 0f, 0f, 0.4f);
        bubbleShadow.effectDistance = new Vector2(0f, -4f);

        Image bubbleStroke = HudStyle.AddImage(overheadBubbleRect, "Stroke", HudStyle.RoundedStroke(16f, 1.2f),
            new Color(HudStyle.Accent.r, HudStyle.Accent.g, HudStyle.Accent.b, 0.55f));
        HudStyle.Stretch(bubbleStroke.rectTransform);

        // Penunjuk kecil di bawah balon mengarah ke kepala NPC
        GameObject arrowObj = new GameObject("BubblePointerArrow", typeof(RectTransform));
        arrowObj.transform.SetParent(overheadBubbleObj.transform, false);
        arrowRect = (RectTransform)arrowObj.transform;
        arrowRect.anchorMin = new Vector2(0.5f, 0f);
        arrowRect.anchorMax = new Vector2(0.5f, 0f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.sizeDelta = new Vector2(14f, 14f);
        arrowRect.anchoredPosition = new Vector2(0f, 0f);
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image arrowImg = arrowObj.AddComponent<Image>();
        arrowImg.color = new Color(0.05f, 0.066f, 0.098f, 0.96f);
        arrowImg.raycastTarget = false;

        GameObject textObj = new GameObject("BubbleText", typeof(RectTransform));
        textObj.transform.SetParent(overheadBubbleObj.transform, false);
        RectTransform textRT = (RectTransform)textObj.transform;
        HudStyle.Stretch(textRT, 18f, 12f, 18f, 12f);

        overheadText = textObj.AddComponent<Text>();
        overheadText.font = mainFont;
        overheadText.fontSize = 15;
        overheadText.fontStyle = FontStyle.Bold;
        overheadText.lineSpacing = 1.12f;
        overheadText.alignment = TextAnchor.MiddleCenter;
        overheadText.color = HudStyle.TextPrimary;
        overheadText.horizontalOverflow = HorizontalWrapMode.Wrap;
        overheadText.verticalOverflow = VerticalWrapMode.Overflow;
        overheadText.raycastTarget = false;

        // 3. SUBTITLE BAR (panel dialog bawah layar: nama pembicara, isi dialog, petunjuk lanjut)
        subtitleBarObj = new GameObject("SubtitleDialogueBar", typeof(RectTransform));
        subtitleBarObj.transform.SetParent(transform, false);

        RectTransform subRT = (RectTransform)subtitleBarObj.transform;
        HudStyle.Place(subRT, new Vector2(0.5f, 0.035f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(700f, 108f));

        Image subBg = subtitleBarObj.AddComponent<Image>();
        subBg.sprite = HudStyle.Rounded(18f);
        subBg.type = Image.Type.Sliced;
        subBg.color = new Color(0.05f, 0.066f, 0.098f, 0.95f);
        subBg.raycastTarget = false;

        Shadow subShadow = subtitleBarObj.AddComponent<Shadow>();
        subShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        subShadow.effectDistance = new Vector2(0f, -6f);

        Image subStroke = HudStyle.AddImage(subRT, "Stroke", HudStyle.RoundedStroke(18f, 1.2f), new Color(1f, 1f, 1f, 0.09f));
        HudStyle.Stretch(subStroke.rectTransform);

        // Titik aksen di depan nama pembicara
        Image speakerDot = HudStyle.AddImage(subRT, "SpeakerDot", HudStyle.Circle(), HudStyle.Accent, false);
        HudStyle.Place(speakerDot.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(24f, -23f), new Vector2(8f, 8f));

        // Nama Pembicara
        GameObject spkObj = new GameObject("SpeakerName", typeof(RectTransform));
        spkObj.transform.SetParent(subtitleBarObj.transform, false);
        RectTransform spkRT = (RectTransform)spkObj.transform;
        HudStyle.Place(spkRT, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -12f), new Vector2(360f, 22f));

        subtitleSpeakerText = spkObj.AddComponent<Text>();
        subtitleSpeakerText.font = mainFont;
        subtitleSpeakerText.fontSize = 15;
        subtitleSpeakerText.fontStyle = FontStyle.Bold;
        subtitleSpeakerText.alignment = TextAnchor.MiddleLeft;
        subtitleSpeakerText.color = HudStyle.Accent;
        subtitleSpeakerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        subtitleSpeakerText.raycastTarget = false;

        // Isi Dialog Subtitle
        GameObject msgObj = new GameObject("SpeakerMessage", typeof(RectTransform));
        msgObj.transform.SetParent(subtitleBarObj.transform, false);
        RectTransform msgRT = (RectTransform)msgObj.transform;
        HudStyle.Stretch(msgRT, 24f, 12f, 24f, 40f);

        subtitleMessageText = msgObj.AddComponent<Text>();
        subtitleMessageText.font = mainFont;
        subtitleMessageText.fontSize = 15;
        subtitleMessageText.fontStyle = FontStyle.Bold;
        subtitleMessageText.lineSpacing = 1.15f;
        subtitleMessageText.alignment = TextAnchor.UpperLeft;
        subtitleMessageText.color = HudStyle.TextPrimary;
        subtitleMessageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subtitleMessageText.verticalOverflow = VerticalWrapMode.Overflow;
        subtitleMessageText.raycastTarget = false;

        // Petunjuk Lanjut / Selesai Dialog (kanan atas panel, tidak menimpa isi dialog)
        GameObject contObj = new GameObject("ContinuePrompt", typeof(RectTransform));
        contObj.transform.SetParent(subtitleBarObj.transform, false);
        RectTransform contRT = (RectTransform)contObj.transform;
        HudStyle.Place(contRT, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -12f), new Vector2(250f, 22f));

        subtitleContinuePromptText = contObj.AddComponent<Text>();
        subtitleContinuePromptText.font = mainFont;
        subtitleContinuePromptText.fontSize = 12;
        subtitleContinuePromptText.fontStyle = FontStyle.Bold;
        subtitleContinuePromptText.alignment = TextAnchor.MiddleRight;
        subtitleContinuePromptText.color = HudStyle.TextSecondary;
        subtitleContinuePromptText.horizontalOverflow = HorizontalWrapMode.Overflow;
        subtitleContinuePromptText.raycastTarget = false;
        subtitleContinuePromptText.text = "[E] / [Spasi] / Klik  Lanjut";

        Image contIcon = HudStyle.AddImage(contObj.transform, "Chevron", HudStyle.Icon("chevron"), HudStyle.Accent, false);
        HudStyle.Place(contIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(14f, 14f));
        subtitleContinuePromptText.gameObject.SetActive(false);

        // Default sembunyikan saat mulai
        overheadBubbleObj.SetActive(false);
        subtitleBarObj.SetActive(false);
    }

    public static void ShowDialogue(NPCWalker speaker, string speakerName, string line, bool show)
    {
        float scaleMul = (speaker != null && speaker.transform.lossyScale.y > 0.5f) ? 10f : 1f;
        float offset = (speaker != null && speaker.isSittingPermanently) ? 0.11f * scaleMul : (speaker != null && speaker.speechBubbleHeight > 1.0f ? speaker.speechBubbleHeight : 0.20f * scaleMul);
        ShowDialogue(speaker != null ? speaker.transform : null, speakerName, line, show, offset);
        if (instance != null) instance.activeSpeaker = show ? speaker : null;
    }

    public static void ShowDialogue(Transform speakerTransform, string speakerName, string line, bool show, float heightOffset = 0.20f)
    {
        if (speakerTransform != null && speakerTransform.lossyScale.y > 0.5f && heightOffset < 1.0f)
        {
            heightOffset *= 10f;
        }

        // 1. Jika hanya ingin menyembunyikan dialog, jangan pernah panggil EnsureInstance() atau buat objek baru
        if (!show)
        {
            if (instance != null && (instance.activeSpeakerTransform == speakerTransform || speakerTransform == null))
            {
                instance.activeSpeaker = null;
                instance.activeSpeakerTransform = null;
                if (instance.overheadBubbleObj != null) instance.overheadBubbleObj.SetActive(false);
                if (instance.subtitleBarObj != null) instance.subtitleBarObj.SetActive(false);
            }
            return;
        }

        if (!Application.isPlaying || isQuitting) return;

        EnsureInstance();
        if (instance == null) return;

        instance.activeSpeakerTransform = speakerTransform;
        instance.activeSpeakerHeightOffset = heightOffset;
        instance.activeSpeakerName = speakerName;
        instance.activeMessage = line;

        if (instance.overheadText != null)
        {
            instance.overheadText.text = line;

            // Hitung tinggi balon percakapan secara dinamis agar seluruh teks muat rapi di dalam kotak putih
            float bubbleWidth = 440f;
            float padH = 36f;
            float padV = 34f;

            TextGenerationSettings settings = instance.overheadText.GetGenerationSettings(new Vector2(bubbleWidth - padH, 0f));
            float textH = instance.overheadText.cachedTextGeneratorForLayout.GetPreferredHeight(line, settings);
            if (textH <= 0f) textH = instance.overheadText.preferredHeight;

            float bubbleHeight = Mathf.Clamp(textH + padV, 56f, 200f);
            instance.overheadBubbleRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
        }
        if (instance.subtitleSpeakerText != null) instance.subtitleSpeakerText.text = speakerName;
        if (instance.subtitleMessageText != null) instance.subtitleMessageText.text = "\"" + line.Replace("\n", " ") + "\"";

        if (instance.overheadBubbleObj != null) instance.overheadBubbleObj.SetActive(true);
        if (instance.subtitleBarObj != null) instance.subtitleBarObj.SetActive(true);

        instance.UpdateOverheadPosition();
    }

    private void Update()
    {
        if (sequentialAdvanceCooldown > 0f)
        {
            sequentialAdvanceCooldown -= Time.unscaledDeltaTime;
        }

        if (isSequentialActive)
        {
            if (sequentialAdvanceCooldown <= 0f && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
            {
                AdvanceSequentialDialogue();
            }
        }
    }

    public static void ShowSequentialDialogue(Transform speakerTransform, string speakerName, string[] lines, System.Action onComplete = null, float heightOffset = 0.20f)
    {
        if (!Application.isPlaying || isQuitting) return;
        EnsureInstance();
        if (instance == null) return;

        if (lines == null || lines.Length == 0)
        {
            ShowDialogue(speakerTransform, speakerName, "", false);
            onComplete?.Invoke();
            return;
        }

        instance.baseNpcTransform = speakerTransform;
        instance.baseNpcName = speakerName;
        instance.baseHeightOffset = heightOffset;

        instance.currentSequentialLines = lines;
        instance.currentSequentialIndex = 0;
        instance.isSequentialActive = true;
        instance.onSequentialComplete = onComplete;
        instance.sequentialAdvanceCooldown = 0.25f;

        // Freeze kontrol player saat berdialog
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) player.SetMovementFrozen(true);

        instance.DisplayCurrentSequentialLine();
    }

    private void DisplayCurrentSequentialLine()
    {
        if (currentSequentialLines == null || currentSequentialIndex >= currentSequentialLines.Length)
        {
            EndSequentialDialogue();
            return;
        }

        string rawLine = currentSequentialLines[currentSequentialIndex];
        Transform targetSpeakerTransform = baseNpcTransform;
        string targetSpeakerName = baseNpcName;
        float targetHeightOffset = baseHeightOffset;
        string cleanLine = rawLine;

        // Dukungan percakapan 2 arah: "Rian: ..." atau "<NPC>: ..."
        int colonIdx = rawLine.IndexOf(": ");
        if (colonIdx > 0 && colonIdx <= 25)
        {
            string speakerTag = rawLine.Substring(0, colonIdx).Trim();
            cleanLine = rawLine.Substring(colonIdx + 2).Trim();

            if (speakerTag.Equals("Rian", System.StringComparison.OrdinalIgnoreCase) ||
                speakerTag.StartsWith("Rian", System.StringComparison.OrdinalIgnoreCase))
            {
                targetSpeakerName = "Rian (Kurir)";
                PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
                if (pc != null)
                {
                    targetSpeakerTransform = pc.transform;
                    // Rian menoleh ke arah NPC yang sedang diajak bicara
                    if (baseNpcTransform != null)
                    {
                        Vector3 dir = (baseNpcTransform.position - pc.transform.position);
                        dir.y = 0;
                        if (dir.sqrMagnitude > 0.01f) pc.transform.rotation = Quaternion.LookRotation(dir);
                    }
                }
                targetHeightOffset = 0.22f;
            }
            else
            {
                targetSpeakerName = speakerTag;
                targetSpeakerTransform = baseNpcTransform;
                // NPC menoleh ke arah Rian saat menjawab pertanyaan
                PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
                if (pc != null && baseNpcTransform != null)
                {
                    Vector3 dir = (pc.transform.position - baseNpcTransform.position);
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.01f) baseNpcTransform.rotation = Quaternion.LookRotation(dir);
                }
            }
        }

        GameAudioManager.Instance?.PlayDialogueAdvance();
        ShowDialogue(targetSpeakerTransform, targetSpeakerName, cleanLine, true, targetHeightOffset);

        // Bedakan warna teks pembicara: Cyan untuk Rian, Amber untuk NPC
        if (subtitleSpeakerText != null)
        {
            if (targetSpeakerName.StartsWith("Rian", System.StringComparison.OrdinalIgnoreCase))
            {
                subtitleSpeakerText.color = HudStyle.Teal;
                subtitleSpeakerText.text = targetSpeakerName + " [Pemain]";
            }
            else
            {
                subtitleSpeakerText.color = HudStyle.Accent;
                subtitleSpeakerText.text = targetSpeakerName;
            }
        }

        if (subtitleContinuePromptText != null)
        {
            subtitleContinuePromptText.gameObject.SetActive(true);
            bool isLast = currentSequentialIndex == currentSequentialLines.Length - 1;
            subtitleContinuePromptText.text = isLast ? "[E] / [Spasi] / Klik  Selesai" : "[E] / [Spasi] / Klik  Lanjut";
        }
    }

    private void AdvanceSequentialDialogue()
    {
        sequentialAdvanceCooldown = 0.2f;
        currentSequentialIndex++;
        if (currentSequentialIndex < currentSequentialLines.Length)
        {
            DisplayCurrentSequentialLine();
        }
        else
        {
            EndSequentialDialogue();
        }
    }

    public static void EndSequentialDialogue()
    {
        if (instance == null) return;
        instance.isSequentialActive = false;
        instance.currentSequentialLines = null;
        instance.currentSequentialIndex = 0;

        if (instance.subtitleContinuePromptText != null)
        {
            instance.subtitleContinuePromptText.gameObject.SetActive(false);
        }

        ShowDialogue((Transform)null, "", "", false);

        // Buka kembali freeze kontrol player
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null) player.SetMovementFrozen(false);

        var cb = instance.onSequentialComplete;
        instance.onSequentialComplete = null;
        cb?.Invoke();
    }

    private void LateUpdate()
    {
        if (activeSpeakerTransform != null && overheadBubbleObj != null && overheadBubbleObj.activeSelf)
        {
            UpdateOverheadPosition();
        }
    }

    private void UpdateOverheadPosition()
    {
        if (activeSpeakerTransform == null) return;

        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null) return;

        // Posisi kepala di dunia 3D:
        Vector3 headWorldPos = activeSpeakerTransform.position + Vector3.up * activeSpeakerHeightOffset;

        Vector3 screenPos = cam.WorldToScreenPoint(headWorldPos);

        // Hanya tampilkan jika berada di depan kamera (Z > 0)
        if (screenPos.z > 0.1f)
        {
            overheadBubbleObj.SetActive(true);

            // Letakkan balon tepat di atas kepala di layar
            // Jarak vertikal di layar: 35px di atas posisi kepala
            overheadBubbleRect.position = new Vector3(screenPos.x, screenPos.y + 35f, 0f);
        }
        else
        {
            overheadBubbleObj.SetActive(false);
        }
    }
}
