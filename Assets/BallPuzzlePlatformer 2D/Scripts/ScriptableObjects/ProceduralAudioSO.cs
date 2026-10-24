using UnityEngine;

[CreateAssetMenu(fileName = "NewProceduralAudioPreset", menuName = "Audio/Procedural Audio Preset")]
public class ProceduralAudioSO : ScriptableObject
{
    [Header("Forma de Onda y Volumen")]
    public WaveformType waveform = WaveformType.Sine;
    [Range(0f, 1f)] public float amplitude = 0.3f;
    public float baseFrequency = 440f;

    [Header("Síntesis Aditiva (10 Armónicos)")]
    public bool useAdditive = false;
    [Range(0f, 1f)] public float[] harmonicLevels = new float[10] { 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };

    [Header("Envolvente ADSR (en ms)")]
    public bool useADSR = true;
    public float attack = 10f;
    public float decay = 150f;
    [Range(0f, 1f)] public float sustainLevel = 0.7f;
    public float sustainTime = 200f;
    public float release = 100f;

    [Header("Pitch Sweep (boing / caída)")]
    public float pitchSweepMultiplier = 1f;
    public float pitchSweepTime = 0f;

    [Header("LFO / Vibrato (FM)")]
    public bool useLFO = false;
    [Range(0f, 10f)] public float lfoRate = 5f;   
    public float lfoDepthHz = 10f;                 
    public float LFOIndex => lfoRate > 0.0001f ? lfoDepthHz / lfoRate : 0f;
}

public enum WaveformType
{
    Sine,
    Square,
    Triangle,
    Sawtooth,
    Additive
}