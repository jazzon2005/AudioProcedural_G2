using System.Collections;
using UnityEngine;

public static class Notes
{
    static readonly int[] Semis = { 0, 2, 4, 5, 7, 9, 11 };

    public static int Midi(string n)
    {
        int s = Semis["CDEFGAB".IndexOf(n[0])];
        int i = 1;
        if (n[i] == '#') { s++; i++; }
        else if (n[i] == 'b') { s--; i++; }
        int oct = int.Parse(n.Substring(i));
        return 12 * (oct + 1) + s;
    }

    public static float Hz(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);
    public static float Hz(string note) => Hz(Midi(note));
    public static float Hz(string note, int octave) => Hz(note + octave);
}

public class ProceduralMusicSystem : MonoBehaviour
{
    public static ProceduralMusicSystem Instance { get; private set; }

    [System.Serializable]
    public class Layer
    {
        public ProceduralAudioSO preset;
        [Range(1, 6)] public int voices = 2;
        [Range(0f, 1f)] public float volume = 1f;

        ProceduralAudioSynth[] pool;
        int idx;

        public void Init(Transform parent, string label)
        {
            pool = new ProceduralAudioSynth[voices];
            for (int i = 0; i < voices; i++)
            {
                var go = new GameObject($"{label}_{i}");
                go.transform.SetParent(parent);
                pool[i] = go.AddComponent<ProceduralAudioSynth>();
            }
        }

        public void Play(float hz, float lenSec, float master, float velocity = 1f)
        {
            if (preset == null || pool == null) return;
            float sustainMs = Mathf.Max(0f, lenSec * 1000f - preset.attack - preset.decay);
            pool[idx].PlayPreset(preset, hz, volume * master * velocity, sustainMs);
            idx = (idx + 1) % pool.Length;
        }

        public void StopAll()
        {
            if (pool == null) return;
            foreach (var s in pool) s.StopSound();
        }
    }

    [Header("Capas Procedurales")]
    public Layer bass = new Layer { voices = 2 };
    public Layer pad = new Layer { voices = 3 };
    public Layer lead = new Layer { voices = 3 };
    public Layer perc = new Layer { voices = 2 };

    [Header("Control Generativo")]
    public bool playOnStart = true;
    [Range(40f, 160f)] public float bpm = 96f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float progress = 0f;
    public int seed = 7;

    static readonly string[] RootNames = { "D2", "Bb1", "C2", "A1" };
    static readonly string[][] PadChords =
    {
        new[] { "D3", "F3", "A3" },
        new[] { "Bb2", "D3", "F3" },
        new[] { "C3", "E3", "G3" },
        new[] { "A2", "C3", "E3" }
    };

    static readonly string[] LeadScale = { "D4", "F4", "G4", "A4", "C5", "D5", "F5", "G5", "A5", "C6" };
    static readonly bool[] BassHit =
        { true,false,false,true, false,false,true,false, true,false,false,true, false,false,true,false };

    float[] rootHz;
    float[][] padHz;
    float[] leadHz;

    System.Random rng;
    double nextStepTime;
    int stepCount;
    int leadIdx = 3;
    bool running;
    bool inCadence;

    bool musicEnabled = true;
    public bool MusicEnabled
    {
        get => musicEnabled;
        set
        {
            musicEnabled = value;
            if (!value) StopMusic();
            else StartMusic();
        }
    }

    public float MusicVolume { get => musicVolume; set => musicVolume = Mathf.Clamp01(value); }
    public void SetProgress(float p) { progress = Mathf.Clamp01(p); }

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Mantener la música viva entre escenas
        } 
        else 
        { 
            Destroy(gameObject); 
            return; 
        }

        rootHz = new float[RootNames.Length];
        for (int i = 0; i < RootNames.Length; i++) rootHz[i] = Notes.Hz(RootNames[i]);

        padHz = new float[PadChords.Length][];
        for (int c = 0; c < PadChords.Length; c++)
        {
            padHz[c] = new float[PadChords[c].Length];
            for (int n = 0; n < PadChords[c].Length; n++) padHz[c][n] = Notes.Hz(PadChords[c][n]);
        }

        leadHz = new float[LeadScale.Length];
        for (int i = 0; i < LeadScale.Length; i++) leadHz[i] = Notes.Hz(LeadScale[i]);

        bass.Init(transform, "Bass");
        pad.Init(transform, "Pad");
        lead.Init(transform, "Lead");
        perc.Init(transform, "Perc");
    }

    void Start()
    {
        if (playOnStart && musicEnabled) StartMusic();
    }

    public void StartMusic()
    {
        if (!musicEnabled) return;
        rng = new System.Random(seed);
        stepCount = 0;
        nextStepTime = AudioSettings.dspTime + 0.1;
        running = true;
    }

    public void StopMusic()
    {
        running = false;
        bass.StopAll(); pad.StopAll(); lead.StopAll(); perc.StopAll();
    }

    void Update()
    {
        if (!running) return;

        double stepDur = 60.0 / bpm / 4.0;
        double now = AudioSettings.dspTime;
        int guard = 0;
        while (now >= nextStepTime && guard++ < 4)
        {
            Step(stepCount, (float)stepDur);
            stepCount++;
            nextStepTime += stepDur;
        }
        if (guard >= 4) nextStepTime = now + stepDur;
    }

    void Step(int count, float stepDur)
    {
        int bar = (count / 16) % rootHz.Length;
        int s = count % 16;
        float m = musicVolume;

        if (s == 0)
        {
            float barSec = stepDur * 16f;
            foreach (float hz in padHz[bar]) pad.Play(hz, barSec * 0.9f, m);
        }

        if (BassHit[s])
        {
            float mul = (s == 6 || s == 14) ? 2f : (s == 3 || s == 11) ? 1.5f : 1f;
            bass.Play(rootHz[bar] * mul, stepDur * 1.5f, m);
        }

        if (!inCadence && s % 2 == 0 && rng.NextDouble() < Mathf.Lerp(0.30f, 0.70f, progress))
        {
            leadIdx = Mathf.Clamp(leadIdx + rng.Next(-2, 3), 0, leadHz.Length - 1);
            float vel = (s % 4 == 0) ? 1f : 0.75f;
            lead.Play(leadHz[leadIdx], stepDur * 2f, m, vel);
        }

        if (s == 4 || s == 12) perc.Play(180f, stepDur, m);
        if (progress >= 0.25f && (s == 7 || s == 10 || s == 15) && rng.NextDouble() < 0.7) perc.Play(330f, stepDur, m, 0.8f);
        if (progress >= 0.6f && s % 2 == 1) perc.Play(900f, stepDur * 0.5f, m, 0.5f);
    }

    public void PlayCadence()
    {
        if (!running) return;
        StartCoroutine(CadenceRoutine());
    }

    IEnumerator CadenceRoutine()
    {
        inCadence = true;
        string[] arp = { "D4", "F4", "A4", "D5", "F5", "A5" };
        foreach (string n in arp)
        {
            lead.Play(Notes.Hz(n), 0.5f, musicVolume);
            yield return new WaitForSeconds(0.11f);
        }
        yield return new WaitForSeconds(0.6f);
        inCadence = false;
    }
}