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

/// <summary>Bar pemuatan tanpa persentase: balok kecil bolak-balik di dalam track (waktu unscaled).</summary>
public class HudLoadingBar : MonoBehaviour
{
    public RectTransform track;
    private RectTransform self;

    private void Awake()
    {
        self = (RectTransform)transform;
    }

    private void Update()
    {
        if (track == null) return;
        if (self == null) self = (RectTransform)transform;
        float range = Mathf.Max(0f, track.rect.width - self.rect.width);
        float t = Mathf.PingPong(Time.unscaledTime * 0.9f, 1f);
        float e = t * t * (3f - 2f * t);
        self.anchoredPosition = new Vector2(e * range, 0f);
    }
}

/// <summary>Pembawa coroutine sederhana yang tidak ikut hancur saat scene berganti.</summary>
public class HudRunner : MonoBehaviour
{
    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Tugasnya selesai setelah scene tujuan termuat
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Destroy(gameObject, 0.5f);
    }
}
