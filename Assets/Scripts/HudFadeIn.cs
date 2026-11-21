using UnityEngine;

/// <summary>
/// Animasi masuk ringan untuk panel UI: fade + geser sedikit saat objek diaktifkan.
/// Memakai waktu unscaled agar tetap berjalan saat Time.timeScale = 0.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class HudFadeIn : MonoBehaviour
{
    public float duration = 0.45f;
    public Vector2 slide = new Vector2(0f, 0f);
    public RectTransform slideTarget;

    private CanvasGroup group;
    private Vector2 basePos;
    private float t;
    private bool hasBase;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (slideTarget != null)
        {
            if (!hasBase) { basePos = slideTarget.anchoredPosition; hasBase = true; }
            slideTarget.anchoredPosition = basePos + slide;
        }
        t = 0f;
        group.alpha = 0f;
    }

    private void Update()
    {
        if (t >= duration) return;
        t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(t / duration);
        float e = 1f - (1f - k) * (1f - k) * (1f - k);
        group.alpha = e;
        if (slideTarget != null) slideTarget.anchoredPosition = basePos + slide * (1f - e);
    }
}
