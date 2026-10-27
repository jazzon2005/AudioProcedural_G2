using System.Collections;
using UnityEngine;

namespace View.Control
{
    /// <summary>
    /// Controlador principal de audio del juego refactorizado para síntesis procedural.
    /// Contiene un enum GameClip extendido con identificadores explícitos para cada acción del videojuego.
    /// </summary>
    public class GameAudio : MonoBehaviour
    {
        private const string MusicStatusKey = "music.status";
        private const string SfxStatusKey = "sfx.status";
        private const float DefaultMusicVolume = 0.2f;

        public static GameAudio Instance { get; private set; }

        [Header("Presets Procedurales (Mapeados 1 a 1 con GameClip)")]
        [Tooltip("Asigna en el Inspector un ProceduralAudioSO para cada posición correspondiente del enum GameClip.")]
        public ProceduralAudioSO[] SfxPresets;

        [Header("Música de Fondo Tradicional (Opcional)")]
        public AudioClip[] MusicClips;

        private AudioSource _musicSource;
        private Coroutine _musicFadeCoroutine;

        private bool _musicEnabled = true;
        public bool MusicEnabled
        {
            get => _musicEnabled;
            set {
                _musicEnabled = value;
                if (_musicSource != null)
                {
                    _musicSource.volume = value ? DefaultMusicVolume : 0f;
                    if (!value) _musicSource.Stop();
                }

                if (ProceduralMusicSystem.Instance != null)
                {
                    ProceduralMusicSystem.Instance.MusicEnabled = value;
                }

                PlayerPrefs.SetInt(MusicStatusKey, value ? 0 : 1);
            }
        }

        private bool _sfxEnabled = true;
        public bool SfxEnabled
        {
            get => _sfxEnabled;
            set {
                _sfxEnabled = value;
                PlayerPrefs.SetInt(SfxStatusKey, value ? 0 : 1);
            }
        }

        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                EnsureAudioListener();
                ValidateAndFixPresets();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            if (!PlayerPrefs.HasKey(MusicStatusKey)) PlayerPrefs.SetInt(MusicStatusKey, 0);
            if (!PlayerPrefs.HasKey(SfxStatusKey)) PlayerPrefs.SetInt(SfxStatusKey, 0);

            MusicEnabled = PlayerPrefs.GetInt(MusicStatusKey) == 0;
            SfxEnabled = PlayerPrefs.GetInt(SfxStatusKey) == 0;
            SafeInitializePlayerPrefs();
        }

        public ProceduralAudioSynth Play(GameClip clip, float delay = 0f, float volume = 1f, float startTime = 0f)
        {
            if (!enabled || !SfxEnabled) {
                return null;
            }

            int index = (int)clip;

            if (SfxPresets == null || index < 0 || index >= SfxPresets.Length || SfxPresets[index] == null)
            {
                Debug.LogWarning($"[GameAudio] No hay un ProceduralAudioSO asignado para el clip: {clip} (Índice: {index})");
                return null;
            }

            ProceduralAudioSO preset = SfxPresets[index];

            if (delay > 0f)
            {
                StartCoroutine(PlayDelayedRoutine(preset, delay, volume));
                return null;
            }

            return ProceduralAudioManager.Instance.PlayPreset(preset, volumeScale: volume);
        }

        private IEnumerator PlayDelayedRoutine(ProceduralAudioSO preset, float delay, float volume)
        {
            yield return new WaitForSeconds(delay);
            if (enabled && SfxEnabled)
            {
                ProceduralAudioManager.Instance.PlayPreset(preset, volumeScale: volume);
            }
        }

        private void EnsureAudioListener()
        {
            if (FindObjectOfType<AudioListener>() == null)
            {
                Debug.LogWarning("[GameAudio] No se encontró AudioListener. Añadiendo uno a la cámara principal automáticamente.");
                if (Camera.main != null)
                {
                    Camera.main.gameObject.AddComponent<AudioListener>();
                }
                else
                {
                    gameObject.AddComponent<AudioListener>();
                }
            }
        }

        private ProceduralAudioSO _fallbackPreset;

        private void ValidateAndFixPresets()
        {
            int enumSize = System.Enum.GetNames(typeof(GameClip)).Length;
            
            // Redimensionar si es más corto
            if (SfxPresets == null || SfxPresets.Length < enumSize)
            {
                System.Array.Resize(ref SfxPresets, enumSize);
            }

            // Crear un sonido por defecto para referencias perdidas
            if (_fallbackPreset == null)
            {
                _fallbackPreset = ScriptableObject.CreateInstance<ProceduralAudioSO>();
                _fallbackPreset.baseFrequency = 440f;
                _fallbackPreset.amplitude = 0.5f;
                _fallbackPreset.useADSR = false;
                _fallbackPreset.sustainTime = 100f;
            }

            for (int i = 0; i < SfxPresets.Length; i++)
            {
                if (SfxPresets[i] == null)
                {
                    Debug.LogWarning($"[GameAudio] Falta preset para {(GameClip)i}. Asignando fallback de emergencia.");
                    SfxPresets[i] = _fallbackPreset;
                }
            }
        }

        private void SafeInitializePlayerPrefs()
        {
        #if UNITY_EDITOR
            // En el editor, forzamos siempre el sonido para evitar falsos positivos de bugs
            PlayerPrefs.SetInt(SfxStatusKey, 0);
            PlayerPrefs.SetInt(MusicStatusKey, 0);
            PlayerPrefs.Save();
        #else
            // En la build final, si las claves no existen, inicializarlas en 0 (encendido)
            if (!PlayerPrefs.HasKey(MusicStatusKey)) PlayerPrefs.SetInt(MusicStatusKey, 0);
            if (!PlayerPrefs.HasKey(SfxStatusKey)) PlayerPrefs.SetInt(SfxStatusKey, 0);
        #endif

            MusicEnabled = PlayerPrefs.GetInt(MusicStatusKey) == 0;
            SfxEnabled = PlayerPrefs.GetInt(SfxStatusKey) == 0;
        }

        public void Play(MusicClip clip, float fadeTime = 0f, float delay = 0f, float volume = 1f, float startTime = 0f)
        {
            if (!enabled || !MusicEnabled || MusicClips == null || (uint)clip >= MusicClips.Length || MusicClips[(uint)clip] == null) {
                return;
            }

            var audioClip = MusicClips[(uint)clip];

            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.loop = true;
            }

            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(audioClip, fadeTime, delay, volume, startTime));
        }

        private IEnumerator FadeMusicRoutine(AudioClip clip, float fadeTime, float delay, float targetVolume, float startTime)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            _musicSource.clip = clip;
            _musicSource.time = Mathf.Clamp(startTime, 0f, clip.length);
            _musicSource.volume = 0f;
            _musicSource.Play();

            if (fadeTime > 0f)
            {
                float elapsed = 0f;
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    _musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeTime);
                    yield return null;
                }
            }

            _musicSource.volume = targetVolume;
        }
    }

    /// <summary>
    /// Mapeo completo de clips de efectos sonoros procedurales.
    /// </summary>
    public enum GameClip
    {
        // Eventos Principales del Juego
        GameStart,
        GameEnd,
        LevelWin,
        LevelLose,

        // Jugador
        Jump,
        Landing,
        LadderClimb,
        PlayerDeath,

        // Mecánicas e Interacciones del Escenario
        JumpPadBounce,
        ButtonPress,
        ButtonRelease,
        PlatformMove,
        PlatformReturn,
        DoorOpen,
        DoorClose,
        DoorEnter,

        // Potenciadores y Objetos
        PowerUpCollect,
        BladeCollision,
        ObjectDestroy,
        SphereExplosion,

        // Interfaz de Usuario y Recompensas
        MenuSelect,
        RewardObtained
    }

    /// <summary>
    /// Mapeo de pistas de música de fondo.
    /// </summary>
    public enum MusicClip
    {
        Ambient01,
        Ambient02
    }
}