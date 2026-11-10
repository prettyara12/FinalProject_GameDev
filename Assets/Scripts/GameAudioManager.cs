using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// GameAudioManager: Sistem audio terpusat untuk efek suara gameplay, UI, dialog,
/// langkah kaki karakter, serta penalti tabrakan dan salah alamat.
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    private static GameAudioManager instance;
    public static GameAudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                EnsureInstance();
            }
            return instance;
        }
    }

    [Header("=== Audio Sources ===")]
    private AudioSource sfxSource;
    private AudioSource footstepSource;

    [Header("=== Sound Clips ===")]
    public AudioClip uiClickClip;
    public AudioClip dialogueAdvanceClip;
    public AudioClip packagePickupClip;
    public AudioClip deliverySuccessClip;
    public AudioClip wrongDeliveryClip;
    public AudioClip trafficPenaltyClip;
    public AudioClip carHonkClip;
    public AudioClip footstepClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureInstance()
    {
        if (instance != null) return;

        instance = Object.FindAnyObjectByType<GameAudioManager>();
        if (instance == null)
        {
            GameObject go = new GameObject("GameAudioManager");
            instance = go.AddComponent<GameAudioManager>();
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
            }
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SetupAudioSources();
        LoadAudioClips();
    }

    private void SetupAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D Sound
            sfxSource.volume = 1f;
        }

        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 0f;
            footstepSource.volume = 0.5f;
        }
    }

    private void LoadAudioClips()
    {
#if UNITY_EDITOR
        if (uiClickClip == null)
            uiClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/UI_Click.wav");
        if (dialogueAdvanceClip == null)
            dialogueAdvanceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Dialogue_Advance.wav");
        if (packagePickupClip == null)
            packagePickupClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Package_Pickup.wav");
        if (deliverySuccessClip == null)
            deliverySuccessClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Delivery_Success.wav");
        if (wrongDeliveryClip == null)
            wrongDeliveryClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Wrong_Delivery.wav");
        if (trafficPenaltyClip == null)
            trafficPenaltyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Traffic_Penalty.wav");
        if (carHonkClip == null)
            carHonkClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Car_Honk.wav");
        if (footstepClip == null)
            footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Footstep.wav");
#endif
    }

    public void PlayUIClick(float volume = 0.75f)
    {
        PlayClip(uiClickClip, volume);
    }

    public void PlayDialogueAdvance(float volume = 0.65f)
    {
        PlayClip(dialogueAdvanceClip, volume);
    }

    public void PlayPackagePickup(float volume = 0.90f)
    {
        PlayClip(packagePickupClip, volume);
    }

    public void PlayDeliverySuccess(float volume = 0.95f)
    {
        PlayClip(deliverySuccessClip, volume);
    }

    public void PlayWrongDelivery(float volume = 0.85f)
    {
        PlayClip(wrongDeliveryClip, volume);
    }

    public void PlayTrafficPenalty(float volume = 0.90f)
    {
        PlayClip(trafficPenaltyClip, volume);
    }

    public void PlayCarHonk(float volume = 0.80f)
    {
        PlayClip(carHonkClip, volume);
    }

    public void PlayFootstep(float volume = 0.40f)
    {
        if (footstepClip == null || footstepSource == null) return;
        footstepSource.pitch = Random.Range(0.92f, 1.08f);
        footstepSource.PlayOneShot(footstepClip, volume);
    }

    public void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}

