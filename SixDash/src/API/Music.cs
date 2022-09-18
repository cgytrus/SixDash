using System;

using JetBrains.Annotations;

using UnityEngine;

namespace SixDash.API;

[PublicAPI]
public static class Music {
    private const float BasePulseFps = 60f;

    public static AudioSource? music { get; private set; }
    public static float offset { get; private set; }
    public static float pulse { get; private set; }

    private static float[] _samples = Array.Empty<float>();
    private static int _prevSamples;

    private static float _peak;
    private static float _pulse2;
    private static float _pulse3;
    private static float _pulseCounter;

    internal static void Patch() {
        World.levelLoading += () => {
            if(music)
                return;
            GameObject musicObj = GameObject.FindGameObjectWithTag("Music");
            music = musicObj ? musicObj.GetComponent<AudioSource>() : null;
            offset = !music || music!.time == 0f ? LevelEditor.songStartTime / 1000f : music.time;
            _prevSamples = music ? music!.timeSamples : 0;
            AudioSettings.GetDSPBufferSize(out int bufferLength, out _);
            _samples = new float[bufferLength];
            pulse = 0f;
            _pulse2 = 0f;
            _pulse3 = 0f;
            _pulseCounter = 0f;
        };

        World.levelUpdate += (_, _) => {
            UpdateMeteringInfo();
            UpdatePulse();
        };
    }

    private static void UpdateMeteringInfo() {
        if(!music)
            return;

        int samples = music!.timeSamples;
        if(samples == _prevSamples)
            return;
        _prevSamples = samples;

        float volume = music.volume * AudioListener.volume;

        AudioClip clip = music.clip;
        int channels = clip.channels;
        float avgPeak = 0f;
        for(int i = 0; i < channels; i++) {
            music.GetOutputData(_samples, i);
            float peak = 0f;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach(float sample in _samples) {
                float absSample = Mathf.Abs(sample / volume);
                if(absSample > peak)
                    peak = absSample;
            }
            avgPeak += peak;
        }
        _peak = avgPeak / channels;
    }

    private static void UpdatePulse() {
        float peak = _peak + 0.1f;
        pulse = peak;
        if(_pulseCounter < 3f || peak < _pulse2 * 1.1f || peak < _pulse3 * 0.95f && _pulse3 * 0.2f < _pulse2)
            pulse = _pulse2 * 0.93f;
        else {
            _pulse3 = peak;
            _pulseCounter = 0f;
            pulse = peak * 1.1f;
        }
        if(pulse < 0.1f)
            _pulse3 = 0f;
        _pulseCounter += Time.deltaTime * BasePulseFps;
        _pulse2 = pulse;
    }
}
