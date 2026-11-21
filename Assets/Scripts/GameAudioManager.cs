using UnityEngine;

/// <summary>
/// GameAudioManager: sistem audio terpusat "Kurir Terakhir".
/// Seluruh efek suara disintesis saat game dimulai (tanpa berkas audio): klik UI, dialog, paket, pengiriman,
/// penalti, klakson, langkah jalan/lari, lompat, mendarat, tabrakan, whoosh sinematik, dan jingle ending.
/// Karakter suara: lembut dan hangat (nada bel, nada segitiga, thump rendah), selaras dengan desain UI baru.
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

    private const int SampleRate = 44100;

    private AudioSource sfxSource;
    private AudioSource footstepSource;

    private AudioClip uiClickClip;
    private AudioClip dialogueAdvanceClip;
    private AudioClip packagePickupClip;
    private AudioClip deliverySuccessClip;
    private AudioClip wrongDeliveryClip;
    private AudioClip trafficPenaltyClip;
    private AudioClip carHonkClip;
    private AudioClip jumpClip;
    private AudioClip landClip;
    private AudioClip whooshClip;
    private AudioClip endWinClip;
    private AudioClip endLoseClip;
    private AudioClip[] walkSteps;
    private AudioClip[] runSteps;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

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
        BuildClips();
    }

    private void SetupAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = 1f;
        }

        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 0f;
            footstepSource.volume = 0.8f;
        }
    }

    // ======================================================================= PUBLIC API

    public void PlayUIClick(float volume = 0.7f) { PlayClip(uiClickClip, volume); }
    public void PlayDialogueAdvance(float volume = 0.6f) { PlayClip(dialogueAdvanceClip, volume); }
    public void PlayPackagePickup(float volume = 0.85f) { PlayClip(packagePickupClip, volume); }
    public void PlayDeliverySuccess(float volume = 0.9f) { PlayClip(deliverySuccessClip, volume); }
    public void PlayWrongDelivery(float volume = 0.8f) { PlayClip(wrongDeliveryClip, volume); }
    public void PlayTrafficPenalty(float volume = 0.9f) { PlayClip(trafficPenaltyClip, volume); }
    public void PlayCarHonk(float volume = 0.75f) { PlayClip(carHonkClip, volume); }
    public void PlayJump(float volume = 0.6f) { PlayClip(jumpClip, volume); }
    public void PlayLand(float volume = 0.6f) { PlayClip(landClip, volume); }
    public void PlayBump(float volume = 0.7f) { PlayClip(trafficPenaltyClip, volume * 0.8f); }
    public void PlayWhoosh(float volume = 0.55f) { PlayClip(whooshClip, volume); }
    public void PlayEnding(bool success, float volume = 0.9f) { PlayClip(success ? endWinClip : endLoseClip, volume); }

    /// <summary>Langkah kaki. running = true memakai bunyi lari yang lebih cepat dan bertenaga.</summary>
    public void PlayFootstep(float volume = 0.4f, bool running = false)
    {
        AudioClip[] bank = running ? runSteps : walkSteps;
        if (bank == null || bank.Length == 0 || footstepSource == null) return;
        AudioClip clip = bank[Random.Range(0, bank.Length)];
        footstepSource.pitch = Random.Range(0.94f, 1.06f);
        footstepSource.PlayOneShot(clip, volume);
    }

    public void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    // ======================================================================= SINTESIS

    private void BuildClips()
    {
        uiClickClip = Synth("UI_Click", 0.12f, b =>
        {
            Tone(b, 0f, 0.09f, 1040f, 640f, 0.55f, 0.002f, 0.026f, Wave.Sine);
            Tone(b, 0f, 0.05f, 2080f, 1280f, 0.12f, 0.001f, 0.012f, Wave.Sine);
        });

        dialogueAdvanceClip = Synth("Dialogue_Advance", 0.22f, b =>
        {
            Tone(b, 0f, 0.09f, 740f, -1f, 0.40f, 0.004f, 0.045f, Wave.Triangle);
            Tone(b, 0.075f, 0.13f, 988f, -1f, 0.40f, 0.004f, 0.06f, Wave.Triangle);
        });

        packagePickupClip = Synth("Package_Pickup", 0.85f, b =>
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < notes.Length; i++) Bell(b, i * 0.075f, notes[i], 0.45f, 0.22f);
        });

        deliverySuccessClip = Synth("Delivery_Success", 1.5f, b =>
        {
            float[] arp = { 783.99f, 987.77f, 1174.66f, 1567.98f };
            for (int i = 0; i < arp.Length; i++) Bell(b, i * 0.085f, arp[i], 0.42f, 0.28f);
            // akord penutup
            Bell(b, 0.36f, 783.99f, 0.30f, 0.55f);
            Bell(b, 0.36f, 987.77f, 0.26f, 0.55f);
            Bell(b, 0.36f, 1174.66f, 0.24f, 0.55f);
            Bell(b, 0.36f, 1567.98f, 0.16f, 0.6f);
        });

        wrongDeliveryClip = Synth("Wrong_Delivery", 0.62f, b =>
        {
            Tone(b, 0f, 0.26f, 246.94f, 233.08f, 0.40f, 0.01f, 0.16f, Wave.SoftSquare);
            Tone(b, 0.22f, 0.36f, 185f, 164.81f, 0.42f, 0.01f, 0.22f, Wave.SoftSquare);
            LowpassInPlace(b, 0.28f);
        });

        trafficPenaltyClip = Synth("Traffic_Penalty", 0.5f, b =>
        {
            Tone(b, 0f, 0.4f, 150f, 42f, 0.85f, 0.002f, 0.11f, Wave.Sine);
            Noise(b, 0f, 0.18f, 0.55f, 0.04f, 0.2f, 11);
        });

        carHonkClip = Synth("Car_Honk", 0.5f, b =>
        {
            Tone(b, 0f, 0.42f, 392f, -1f, 0.45f, 0.012f, 1.2f, Wave.Saw, 0.06f);
            Tone(b, 0f, 0.42f, 494f, -1f, 0.40f, 0.012f, 1.2f, Wave.Saw, 0.06f);
            LowpassInPlace(b, 0.22f);
        });

        jumpClip = Synth("Jump", 0.3f, b =>
        {
            Tone(b, 0f, 0.2f, 300f, 700f, 0.40f, 0.005f, 0.11f, Wave.Sine);
            Noise(b, 0f, 0.14f, 0.18f, 0.06f, 0.35f, 21);
        });

        landClip = Synth("Land", 0.28f, b =>
        {
            Tone(b, 0f, 0.22f, 110f, 48f, 0.9f, 0.002f, 0.07f, Wave.Sine);
            Noise(b, 0f, 0.1f, 0.4f, 0.03f, 0.22f, 31);
        });

        whooshClip = Synth("Whoosh", 0.95f, b =>
        {
            var rng = new System.Random(41);
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)b.Length;
                float swell = Mathf.Sin(t * Mathf.PI);
                swell *= swell;
                float cutoff = Mathf.Lerp(0.015f, 0.22f, swell);
                float n = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += cutoff * (n - lp);
                b[i] += lp * swell * 3.2f;
            }
        });

        endWinClip = Synth("Ending_Win", 2.4f, b =>
        {
            float[] arp = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < arp.Length; i++) Bell(b, i * 0.14f, arp[i], 0.45f, 0.35f);
            float[] chord = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            for (int i = 0; i < chord.Length; i++) Bell(b, 0.62f, chord[i], 0.28f, 0.9f);
        });

        endLoseClip = Synth("Ending_Lose", 2.0f, b =>
        {
            Tone(b, 0f, 0.55f, 440f, -1f, 0.38f, 0.02f, 0.3f, Wave.Triangle);
            Tone(b, 0.34f, 0.55f, 349.23f, -1f, 0.38f, 0.02f, 0.3f, Wave.Triangle);
            Tone(b, 0.68f, 1.2f, 293.66f, 277f, 0.40f, 0.02f, 0.55f, Wave.Triangle);
            LowpassInPlace(b, 0.35f);
        });

        walkSteps = new AudioClip[3];
        runSteps = new AudioClip[3];
        for (int i = 0; i < 3; i++)
        {
            int seed = 100 + i * 7;
            float tone = 0.14f + i * 0.02f;
            walkSteps[i] = Synth("Step_Walk_" + i, 0.13f, b =>
            {
                Noise(b, 0f, 0.1f, 0.5f, 0.03f, tone, seed);
                Tone(b, 0f, 0.1f, 85f, 58f, 0.55f, 0.002f, 0.04f, Wave.Sine);
            });
            runSteps[i] = Synth("Step_Run_" + i, 0.1f, b =>
            {
                Noise(b, 0f, 0.08f, 0.6f, 0.022f, tone + 0.12f, seed + 3);
                Tone(b, 0f, 0.08f, 105f, 64f, 0.65f, 0.002f, 0.032f, Wave.Sine);
            });
        }
    }

    private enum Wave { Sine, Triangle, SoftSquare, Saw }

    private static AudioClip Synth(string name, float seconds, System.Action<float[]> fill)
    {
        float[] data = new float[Mathf.CeilToInt(seconds * SampleRate)];
        fill(data);

        // normalisasi lembut + batas puncak agar tidak pecah
        float peak = 0.0001f;
        for (int i = 0; i < data.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
        float gain = 0.88f / peak;
        for (int i = 0; i < data.Length; i++)
        {
            float v = data[i] * Mathf.Min(gain, 1.6f);
            data[i] = (float)System.Math.Tanh(v * 1.1f) * 0.92f;
        }
        // fade-out singkat di ujung klip agar tidak berdetak
        int fade = Mathf.Min(data.Length, SampleRate / 200);
        for (int i = 0; i < fade; i++) data[data.Length - 1 - i] *= i / (float)fade;

        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static float Osc(Wave wave, float phase)
    {
        float p = phase - Mathf.Floor(phase);
        switch (wave)
        {
            case Wave.Triangle: return 4f * Mathf.Abs(p - 0.5f) - 1f;
            case Wave.SoftSquare: return (float)System.Math.Tanh(Mathf.Sin(p * Mathf.PI * 2f) * 3f);
            case Wave.Saw: return 2f * p - 1f;
            default: return Mathf.Sin(p * Mathf.PI * 2f);
        }
    }

    /// <summary>Nada tunggal dengan envelope attack + peluruhan eksponensial. freqEnd &lt; 0 = frekuensi tetap.</summary>
    private static void Tone(float[] b, float start, float dur, float freq, float freqEnd, float amp,
        float attack, float tau, Wave wave, float vibrato = 0f)
    {
        int s0 = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int n = Mathf.RoundToInt(dur * SampleRate);
        double phase = 0.0;
        for (int i = 0; i < n && s0 + i < b.Length; i++)
        {
            float t = i / (float)SampleRate;
            float k = n > 1 ? i / (float)(n - 1) : 0f;
            float f = freqEnd > 0f ? Mathf.Lerp(freq, freqEnd, 1f - Mathf.Pow(1f - k, 2f)) : freq;
            if (vibrato > 0f) f *= 1f + vibrato * 0.02f * Mathf.Sin(t * 38f);
            phase += f / SampleRate;
            float env = (t < attack ? t / Mathf.Max(0.0001f, attack) : 1f) * Mathf.Exp(-t / Mathf.Max(0.001f, tau));
            b[s0 + i] += Osc(wave, (float)phase) * env * amp;
        }
    }

    /// <summary>Nada bel: dasar + overtone, peluruhan panjang.</summary>
    private static void Bell(float[] b, float start, float freq, float amp, float tau)
    {
        Tone(b, start, tau * 5f, freq, -1f, amp, 0.002f, tau, Wave.Sine);
        Tone(b, start, tau * 3f, freq * 2.01f, -1f, amp * 0.35f, 0.001f, tau * 0.45f, Wave.Sine);
        Tone(b, start, tau * 2f, freq * 3.02f, -1f, amp * 0.14f, 0.001f, tau * 0.25f, Wave.Sine);
    }

    /// <summary>Derau yang dilunakkan (low-pass satu kutub) dengan envelope peluruhan.</summary>
    private static void Noise(float[] b, float start, float dur, float amp, float tau, float cutoff, int seed)
    {
        var rng = new System.Random(seed);
        int s0 = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
        int n = Mathf.RoundToInt(dur * SampleRate);
        float lp = 0f;
        for (int i = 0; i < n && s0 + i < b.Length; i++)
        {
            float t = i / (float)SampleRate;
            float x = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += cutoff * (x - lp);
            float env = Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t / Mathf.Max(0.001f, tau));
            b[s0 + i] += lp * env * amp * 2.2f;
        }
    }

    private static void LowpassInPlace(float[] b, float cutoff)
    {
        float lp = 0f;
        for (int i = 0; i < b.Length; i++)
        {
            lp += cutoff * (b[i] - lp);
            b[i] = lp;
        }
    }
}
