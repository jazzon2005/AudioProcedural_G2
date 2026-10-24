using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProceduralAudioSynth : MonoBehaviour
{
    private ProceduralAudioSO currentPreset;

    private float currentFrequency;
    private float currentAmplitude;
    private WaveformType currentWaveform;
    private float[] currentHarmonics = new float[10];

    // LFO (FM)
    private bool currentUseLFO;
    private float currentLfoRate;
    private float currentLfoIndex;

    // Pitch sweep
    private float sweepStartMul = 1f;
    private float sweepTime = 0f;
    private float sweepElapsed = 0f;

    // ADSR
    private enum AdsrStage { Idle, Attack, Decay, Sustain, Release }
    private AdsrStage stage = AdsrStage.Idle;
    private float envelopeValue = 0f;
    private float releaseStartValue = 0f;
    private float currentSustainTimer = 0f;
    private bool isPlaying = false;

    private float sampleRate;
    private float phase = 0f;     
    private float lfoTime = 0f;   

    void Awake()
    {
        sampleRate = AudioSettings.outputSampleRate;
    }

    public void PlayPreset(ProceduralAudioSO preset, float frequencyOverride = -1f,
                           float volumeScale = 1f, float sustainMsOverride = -1f)
    {
        if (preset == null) return;

        currentPreset = preset;
        currentFrequency = (frequencyOverride > 0) ? frequencyOverride : preset.baseFrequency;
        currentAmplitude = preset.amplitude * volumeScale;
        currentWaveform = preset.waveform;

        currentUseLFO = preset.useLFO;
        currentLfoRate = preset.lfoRate;
        currentLfoIndex = preset.LFOIndex;

        sweepStartMul = preset.pitchSweepMultiplier;
        sweepTime = preset.pitchSweepTime / 1000f;
        sweepElapsed = 0f;

        if (preset.harmonicLevels != null && preset.harmonicLevels.Length == 10)
            System.Array.Copy(preset.harmonicLevels, currentHarmonics, 10);

        phase = 0f;
        lfoTime = 0f;

        isPlaying = true;
        stage = AdsrStage.Attack;
        envelopeValue = 0f;
        float sustainMs = sustainMsOverride >= 0f ? sustainMsOverride : preset.sustainTime;
        currentSustainTimer = sustainMs / 1000f;
    }

    public void SetLiveFrequency(float freq)
    {
        currentFrequency = freq;
    }

    public void StopSound()
    {
        if (stage != AdsrStage.Idle)
        {
            stage = AdsrStage.Release;
            releaseStartValue = envelopeValue;
        }
        else
        {
            isPlaying = false;
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (currentPreset == null || (!isPlaying && stage == AdsrStage.Idle))
        {
            for (int i = 0; i < data.Length; i++) data[i] = 0f;
            return;
        }

        float dt = 1f / sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            float sweepMul = 1f;
            if (sweepTime > 0f && sweepElapsed < sweepTime)
            {
                sweepMul = Mathf.Lerp(sweepStartMul, 1f, sweepElapsed / sweepTime);
                sweepElapsed += dt;
            }

            phase += currentFrequency * sweepMul * dt;
            phase -= Mathf.Floor(phase);

            float sample = GetSample(currentWaveform, phase);
            float env = currentPreset.useADSR ? EvaluateAdsr(dt) : EvaluateNonAdsr(dt);
            sample = Mathf.Clamp(sample * env * currentAmplitude, -1f, 1f);

            for (int j = 0; j < channels; j++) data[i + j] = sample;

            lfoTime += dt;
        }
    }

    private float EvaluateAdsr(float dt)
    {
        switch (stage)
        {
            case AdsrStage.Attack:
                envelopeValue += dt / Mathf.Max(0.0001f, currentPreset.attack / 1000f);
                if (envelopeValue >= 1f) { envelopeValue = 1f; stage = AdsrStage.Decay; }
                break;

            case AdsrStage.Decay:
                envelopeValue -= dt / Mathf.Max(0.0001f, currentPreset.decay / 1000f);
                if (envelopeValue <= currentPreset.sustainLevel) { envelopeValue = currentPreset.sustainLevel; stage = AdsrStage.Sustain; }
                break;

            case AdsrStage.Sustain:
                envelopeValue = currentPreset.sustainLevel;
                currentSustainTimer -= dt;
                if (currentSustainTimer <= 0f)
                {
                    stage = AdsrStage.Release;
                    releaseStartValue = envelopeValue;
                }
                break;

            case AdsrStage.Release:
                envelopeValue -= dt * (releaseStartValue / Mathf.Max(0.0001f, currentPreset.release / 1000f));
                if (envelopeValue <= 0f) 
                { 
                    envelopeValue = 0f; 
                    stage = AdsrStage.Idle; 
                    isPlaying = false; // Finalizar reproducción limpiamente
                }
                break;
        }
        return envelopeValue;
    }

    private float EvaluateNonAdsr(float dt)
    {
        currentSustainTimer -= dt;
        if (currentSustainTimer <= 0f)
        {
            isPlaying = false;
            stage = AdsrStage.Idle;
            return 0f;
        }
        return 1f;
    }

    private float GetSample(WaveformType type, float ph)
    {
        float cycles = ph;
        if (currentUseLFO)
            cycles += currentLfoIndex * Mathf.Sin(2f * Mathf.PI * currentLfoRate * lfoTime) / (2f * Mathf.PI);

        switch (type)
        {
            case WaveformType.Sine: return Mathf.Sin(2f * Mathf.PI * cycles);
            case WaveformType.Square: return Mathf.Sign(Mathf.Sin(2f * Mathf.PI * cycles));
            case WaveformType.Triangle: return Mathf.PingPong(cycles * 2f, 1f) * 2f - 1f;
            case WaveformType.Sawtooth: return 2f * (cycles - Mathf.Floor(cycles + 0.5f));
            case WaveformType.Additive:
                float val = 0f;
                for (int i = 0; i < currentHarmonics.Length; i++)
                {
                    if (currentHarmonics[i] > 0f)
                        val += currentHarmonics[i] * Mathf.Sin(2f * Mathf.PI * (i + 1) * cycles);
                }
                return val / 2.5f;
            default: return 0f;
        }
    }
}