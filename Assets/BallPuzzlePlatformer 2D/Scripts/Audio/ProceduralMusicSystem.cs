using System.Collections;
using UnityEngine;

/// <summary>Conversión de nombres de nota a Hz. Ej: Notes.Hz("C#4"), Notes.Hz("Bb1"), Notes.Hz("C", 4)</summary>
public static class Notes
{
    static readonly int[] Semis = { 0, 2, 4, 5, 7, 9, 11 }; // C D E F G A B

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

/// <summary>
/// Música generativa chiptune por capas (bajo triangular, pad, lead cuadrada y percusión de ruido)
/// con un tema distinto para cada uno de los 10 niveles.
/// Usa sus propias voces: no compite con los SFX del ProceduralAudioManager.
/// </summary>
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

    // ---------- Temas por nivel ----------
    struct Theme
    {
        public string name;       // nombre de trabajo (cámbialo por el de tu nivel)
        public int tonic;         // MIDI de la tónica del bajo (36 = C2)
        public float bpm;
        public int[] chords;      // raíz de cada uno de los 4 compases, en semitonos sobre la tónica
        public string quality;    // 'm' menor, 'M' mayor, una letra por compás
        public bool minor;        // pentatónica menor (true) o mayor (false) para la lead
        public int pattern;       // índice en BassPatterns

        public Theme(string n, int t, float b, int[] c, string q, bool m, int p)
        { name = n; tonic = t; bpm = b; chords = c; quality = q; minor = m; pattern = p; }
    }

    static readonly Theme[] Themes =
    {
        new Theme("Nivel 1",  36, 108f, new[]{0,7,9,5},  "MMmM", false, 0), // Do mayor
        new Theme("Nivel 2",  43, 112f, new[]{0,5,7,5},  "MMMM", false, 0), // Sol mayor
        new Theme("Nivel 3",  38, 116f, new[]{0,8,10,7}, "mMMm", true,  1), // Re menor
        new Theme("Nivel 4",  33, 120f, new[]{0,5,8,7},  "mmMm", true,  1), // La menor
        new Theme("Nivel 5",  41, 124f, new[]{0,10,5,7}, "MMMM", false, 1), // Fa mayor
        new Theme("Nivel 6",  40, 128f, new[]{0,3,8,10}, "mMMM", true,  2), // Mi menor
        new Theme("Nivel 7",  45, 132f, new[]{0,7,5,7},  "MMMM", false, 2), // La mayor
        new Theme("Nivel 8",  36, 136f, new[]{0,8,3,10}, "mMMM", true,  2), // Do menor
        new Theme("Nivel 9",  42, 142f, new[]{0,8,3,10}, "mMMM", true,  2), // Fa# menor
        new Theme("Nivel 10", 38, 152f, new[]{0,1,7,0},  "mMMm", true,  2), // Re menor tenso
    };

    // 16 pasos de semicorchea por compás; 'x' = golpe de bajo
    static readonly string[] BassPatterns =
    {
        "x...x...x...x.x.",   // 0: pulso simple (niveles iniciales)
        "x..x..x.x..x..x.",   // 1: rebotón tipo Bomberman
        "x.x.x.x.x.x.x.x."    // 2: corcheas constantes (urgencia)
    };

    static readonly int[] MinorPent = { 0, 3, 5, 7, 10 };
    static readonly int[] MajorPent = { 0, 2, 4, 7, 9 };

    [Header("Capas Procedurales")]
    public Layer bass = new Layer { voices = 2 };
    public Layer pad = new Layer { voices = 3 };
    public Layer lead = new Layer { voices = 3 };
    public Layer perc = new Layer { voices = 2 };

    [Header("Percusión de ruido (Hz de velocidad del ruido)")]
    public float kickHz = 70f;
    public float snareHz = 2800f;
    public float hatHz = 8000f;

    [Header("Control Generativo")]
    public bool playOnStart = true;
    [Range(1, 10)] public int level = 1;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Tooltip("0 = inicio / calma, 1 = máxima tensión (cerca de la salida o del límite de tiempo)")]
    [Range(0f, 1f)] public float progress = 0f;
    public int seed = 7;

    // Estado del tema activo
    Theme theme;
    float[] rootHz;
    float[][] padHz;
    float[] leadHz;
    int thirdLead;

    System.Random rng;
    double nextStepTime;
    int stepCount;
    int leadIdx = 3;
    bool running;

    // Ducking: baja la música unos instantes cuando suena un SFX grande
    float duck = 1f;
    float duckRecover = 0.5f;

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

    // Compatibilidad: lee el tempo actual del tema o lo reemplaza a mano.
    // SetLevel() borra el reemplazo y vuelve al tempo del tema.
    float bpmOverride = -1f;
    public float bpm
    {
        get => bpmOverride > 0f ? bpmOverride : theme.bpm;
        set => bpmOverride = value;
    }
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

        bass.Init(transform, "Bass");
        pad.Init(transform, "Pad");
        lead.Init(transform, "Lead");
        perc.Init(transform, "Perc");

        BuildTheme(level);
    }

    void Start()
    {
        if (playOnStart && musicEnabled) StartMusic();
    }

    /// <summary>Cambia al tema del nivel (1 a 10) y reinicia la música con una breve pausa de transición.</summary>
    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, 1, Themes.Length);
        progress = 0f;
        bpmOverride = -1f;
        BuildTheme(level);
        StopMusic();
        if (musicEnabled) StartMusic();
    }

    void BuildTheme(int lvl)
    {
        theme = Themes[Mathf.Clamp(lvl, 1, Themes.Length) - 1];

        rootHz = new float[4];
        padHz = new float[4][];
        for (int c = 0; c < 4; c++)
        {
            int o = theme.chords[c] > 6 ? theme.chords[c] - 12 : theme.chords[c]; // mantiene el bajo en registro grave
            int b = theme.tonic + o;
            rootHz[c] = Notes.Hz(b);
            int third = theme.quality[c] == 'm' ? 3 : 4;
            int r = b + 12;
            padHz[c] = new[] { Notes.Hz(r), Notes.Hz(r + third), Notes.Hz(r + 7) };
        }

        int[] scale = theme.minor ? MinorPent : MajorPent;
        leadHz = new float[10];
        for (int i = 0; i < 10; i++)
            leadHz[i] = Notes.Hz(theme.tonic + 24 + 12 * (i / 5) + scale[i % 5]);

        thirdLead = theme.minor ? 3 : 4;
    }

    public void StartMusic()
    {
        if (!musicEnabled) return;
        rng = new System.Random(seed + level);   // cada nivel tiene su propia melodía, repetible
        stepCount = 0;
        leadIdx = 3;
        nextStepTime = AudioSettings.dspTime + 0.25; // pausa breve: transición entre niveles
        running = true;
    }

    public void StopMusic()
    {
        running = false;
        bass.StopAll(); pad.StopAll(); lead.StopAll(); perc.StopAll();
    }

    /// <summary>Baja la música (amount 0 a 1) y la recupera en 'seconds'. Úsalo con explosiones, muerte, etc.</summary>
    public void Duck(float amount, float seconds)
    {
        duck = Mathf.Min(duck, 1f - Mathf.Clamp01(amount));
        duckRecover = Mathf.Max(0.05f, seconds);
    }

    void Update()
    {
        duck = Mathf.MoveTowards(duck, 1f, Time.unscaledDeltaTime / duckRecover);

        if (!running) return;

        // La tensión acelera un poco el tempo (hasta +12 %)
        float bpmNow = bpm * Mathf.Lerp(1f, 1.12f, progress);
        double stepDur = 60.0 / bpmNow / 4.0;
        double now = AudioSettings.dspTime;
        int guard = 0;
        while (now >= nextStepTime && guard++ < 4)
        {
            Step(stepCount, (float)stepDur);
            stepCount++;
            nextStepTime += stepDur;
        }
        if (guard >= 4) nextStepTime = now + stepDur; // recuperación tras un hitch
    }

    void Step(int count, float stepDur)
    {
        int bar = (count / 16) % 4;
        int s = count % 16;
        float m = musicVolume * duck;

        // PAD: acorde largo al inicio de cada compás
        if (s == 0)
        {
            float barSec = stepDur * 16f;
            foreach (float hz in padHz[bar]) pad.Play(hz, barSec * 0.9f, m);
        }

        // BAJO: patrón del tema, con octava y quinta como en los juegos de plataformas
        if (BassPatterns[theme.pattern][s] == 'x')
        {
            float mul = (s % 8 == 6) ? 2f : (s % 8 == 3) ? 1.5f : 1f;
            bass.Play(rootHz[bar] * mul, stepDur * 1.5f, m);
        }

        // LEAD: caminata aleatoria por la pentatónica; más densa con el nivel y con la tensión
        float baseProb = Mathf.Lerp(0.25f, 0.45f, (level - 1) / 9f);
        if (s % 2 == 0 && rng.NextDouble() < Mathf.Lerp(baseProb, 0.75f, progress))
        {
            leadIdx = Mathf.Clamp(leadIdx + rng.Next(-2, 3), 0, leadHz.Length - 1);
            float vel = (s % 4 == 0) ? 1f : 0.75f;
            lead.Play(leadHz[leadIdx], stepDur * 2f, m, vel);
        }

        // PERCUSIÓN de ruido: bombo siempre, caja desde el nivel 3, hi-hat según tensión
        if (s == 0 || s == 8) perc.Play(kickHz, stepDur, m);
        if (level >= 3 && (s == 4 || s == 12)) perc.Play(snareHz, stepDur, m, 0.8f);
        if (progress > 0.1f && s % 2 == 1 && rng.NextDouble() < Mathf.InverseLerp(0.1f, 0.6f, progress))
            perc.Play(hatHz, stepDur * 0.5f, m, 0.4f);
    }

    /// <summary>Compatibilidad con la versión anterior: equivale a PlayWinStinger.</summary>
    public void PlayCadence() { PlayWinStinger(); }

    /// <summary>Cierre al ganar: para la música y toca un arpegio ascendente en la tonalidad del nivel.</summary>
    public void PlayWinStinger()
    {
        StopMusic();
        int r = theme.tonic + 24;
        StartCoroutine(Stinger(new[] { 0, thirdLead, 7, 12, 12 + thirdLead, 19 }, r, 0.09f));
    }

    /// <summary>Cierre al perder: para la música y toca una línea descendente menor.</summary>
    public void PlayLoseStinger()
    {
        StopMusic();
        int r = theme.tonic + 24;
        StartCoroutine(Stinger(new[] { 12, 7, 3, 0, -5 }, r, 0.16f));
    }

    IEnumerator Stinger(int[] semis, int rootMidi, float gap)
    {
        foreach (int semi in semis)
        {
            lead.Play(Notes.Hz(rootMidi + semi), gap * 3f, musicVolume);
            yield return new WaitForSeconds(gap);
        }
    }
}