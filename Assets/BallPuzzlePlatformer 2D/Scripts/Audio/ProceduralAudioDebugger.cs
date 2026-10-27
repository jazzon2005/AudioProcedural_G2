using UnityEngine;

namespace View.Control
{
    /// <summary>
    /// Componente de diagnóstico rápido para detectar la causa exacta por la que
    /// no se escucha el audio procedural en Unity.
    /// </summary>
    public class ProceduralAudioDebugger : MonoBehaviour
    {
        [Header("Prueba Directa de Tono Senoidal (Bypass de Managers)")]
        [Tooltip("Tecla para emitir un pitido de prueba puro de 440 Hz directo al hardware")]
        public KeyCode testToneKey = KeyCode.T;

        [Header("Prueba a Través de GameAudio")]
        public KeyCode testGameAudioKey = KeyCode.G;
        public GameClip clipToTest = GameClip.GameStart;

        private AudioSource _directAudioSource;
        private bool _isPlayingDirectTone = false;
        private float _sampleRate;
        private float _phase = 0f;

        private void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;

            // Configurar AudioSource dedicado para pruebas directas de hardware
            _directAudioSource = gameObject.AddComponent<AudioSource>();
            _directAudioSource.playOnAwake = false;
            _directAudioSource.spatialBlend = 0f; // Importante: 2D Sound
            _directAudioSource.volume = 0.5f;
        }

        private void Start()
        {
            RunFullDiagnostics();
        }

        private void Update()
        {
            // Tono Directo (Bypass completo)
            if (Input.GetKeyDown(testToneKey))
            {
                _isPlayingDirectTone = true;
                _directAudioSource.Play();
                Debug.Log("<color=cyan>[AudioDebugger] Disparando Tono Senoidal Directo (Bypass)...</color>");
            }
            if (Input.GetKeyUp(testToneKey))
            {
                _isPlayingDirectTone = false;
                _directAudioSource.Stop();
            }

            // Prueba mediante GameAudio
            if (Input.GetKeyDown(testGameAudioKey))
            {
                Debug.Log($"<color=yellow>[AudioDebugger] Disparando {clipToTest} vía GameAudio.Instance...</color>");
                if (GameAudio.Instance != null)
                {
                    var synth = GameAudio.Instance.Play(clipToTest);
                    if (synth == null)
                    {
                        Debug.LogError("[AudioDebugger] ERROR: GameAudio.Play() retornó NULL. Revisa el array SfxPresets o sfx.status en PlayerPrefs.");
                    }
                    else
                    {
                        Debug.Log("<color=green>[AudioDebugger] ÉXITO: Sintetizador asignado y ejecutándose.</color>");
                    }
                }
                else
                {
                    Debug.LogError("[AudioDebugger] ERROR: GameAudio.Instance es NULL en la escena.");
                }
            }
        }

        /// <summary>
        /// Realiza un escaneo automático de las causas más comunes de silencio.
        /// </summary>
        public void RunFullDiagnostics()
        {
            Debug.Log("==========================================");
            Debug.Log("   DIAGNÓSTICO DE AUDIO PROCEDURAL UNIT   ");
            Debug.Log("==========================================");

            // 1. AudioListener
            var listeners = FindObjectsOfType<AudioListener>();
            if (listeners.Length == 0)
            {
                Debug.LogError("❌ CRÍTICO: No se encontró ningún 'AudioListener' en la escena. Agrega uno a la Main Camera.");
            }
            else if (listeners.Length > 1)
            {
                Debug.LogWarning($"⚠️ ADVERTENCIA: Hay {listeners.Length} AudioListeners activos. Unity recomienda tener solo uno.");
            }
            else
            {
                Debug.Log("✅ AudioListener: OK (Encontrado 1 en la escena).");
            }

            // 2. Estado de PlayerPrefs (Mute status)
            int sfxStatus = PlayerPrefs.GetInt("sfx.status", 0);
            if (sfxStatus != 0)
            {
                Debug.LogWarning("⚠️ PlayerPrefs Mute: 'sfx.status' está en 1 (SILENCIADO). Limpiando clave para restaurar sonido...");
                PlayerPrefs.SetInt("sfx.status", 0);
                PlayerPrefs.Save();
                if (GameAudio.Instance != null) GameAudio.Instance.SfxEnabled = true;
            }
            else
            {
                Debug.Log("✅ PlayerPrefs: SFX Habilitado (sfx.status = 0).");
            }

            // 3. GameAudio Instance
            if (GameAudio.Instance == null)
            {
                Debug.LogError("❌ CRÍTICO: GameAudio.Instance no está instanciado en la escena.");
            }
            else
            {
                Debug.Log("✅ GameAudio: Singleton instanciado correctamente.");

                // 4. SfxPresets Array Check
                var presets = GameAudio.Instance.SfxPresets;
                if (presets == null || presets.Length == 0)
                {
                    Debug.LogError("❌ CRÍTICO: El array 'SfxPresets' en GameAudio está VACÍO o sin inicializar.");
                }
                else
                {
                    Debug.Log($"✅ SfxPresets: Array configurado con {presets.Length} elementos.");
                    int index = (int)clipToTest;
                    if (index >= presets.Length || presets[index] == null)
                    {
                        Debug.LogError($"❌ CRÍTICO: No hay un ProceduralAudioSO asignado en el índice {index} ({clipToTest}).");
                    }
                    else
                    {
                        var so = presets[index];
                        Debug.Log($"✅ Preset '{clipToTest}': Asignado ({so.name}). Amplitude: {so.amplitude}, Freq: {so.baseFrequency}Hz, Waveform: {so.waveform}");
                        if (so.amplitude <= 0.001f)
                        {
                            Debug.LogWarning($"⚠️ ADVERTENCIA: La amplitud del preset '{so.name}' es {so.amplitude}. ¡Incrementa su valor en el Inspector!");
                        }
                    }
                }
            }

            Debug.Log("------------------------------------------");
            Debug.Log("INSTRUCCIONES:");
            Debug.Log($" - Mantén presionada la tecla [{testToneKey}] para probar tono puro directo.");
            Debug.Log($" - Presiona la tecla [{testGameAudioKey}] para probar reproducción mediante GameAudio.");
            Debug.Log("==========================================");
        }

        // Generador de tono senoidal puro directo para la prueba de bypass hardware
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_isPlayingDirectTone) return;

            float freq = 440f; // Nota A4
            float increment = freq * 2f * Mathf.PI / _sampleRate;

            for (int i = 0; i < data.Length; i += channels)
            {
                _phase += increment;
                if (_phase > 2f * Mathf.PI) _phase -= 2f * Mathf.PI;

                float sample = Mathf.Sin(_phase) * 0.3f;

                for (int j = 0; j < channels; j++)
                {
                    data[i + j] = sample;
                }
            }
        }
    }
}