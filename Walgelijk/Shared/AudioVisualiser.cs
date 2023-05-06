using System;
using Walgelijk.FFT;

namespace Walgelijk;

public class AudioVisualiser
{
    public readonly Sound Sound;

    public readonly int BufferSize;
    public readonly int BinCount;
    public readonly int BarCount;
    public readonly int FftSize;
    public float MinFreq = 50;
    public float MaxFreq = 16000;
    public int InputBlurIterations = 0;
    public int OutputBlurIterations = 0;
    public float InputBlurIntensity = 0.5f;
    public float OutputBlurIntensity = 0.5f;
    public float Smoothing = 0.5f;
    public bool OverlapWindow = false;

    public float MinDb = -30;
    public float MaxDb = 100;

    public float SampleAccumulatorOverwriteFactor = 0.7f;

    public float[] GetVisualiserData => bars;

    private float[] samples;
    private float[] fft;
    private float[] sampleAccumulator;
    private float[] bars;

    private int accumulationCursor = 0;

    public AudioVisualiser(Sound sound, int fftSize = 4096, int bufferSize = 1024, int barCount = 128)
    {
        BufferSize = bufferSize;
        BinCount = fftSize / 2;
        BarCount = barCount;
        Sound = sound;
        FftSize = fftSize;

        samples = new float[BufferSize];
        sampleAccumulator = new float[FftSize];
        fft = new float[FftSize];
        bars = new float[barCount];
    }

    private void UpdateFft(AudioRenderer audio)
    {
        int readSampleCount = audio.GetCurrentSamples(Sound, samples);

        if (Sound.Data.ChannelCount == 2)
        {
            //stereo! channels are interleaved! take left
            for (int i = 0; i < readSampleCount; i += 2)
            {
                var index = accumulationCursor % sampleAccumulator.Length;
                sampleAccumulator[index] = Utilities.Lerp(sampleAccumulator[index], (samples[i] + samples[i + 1]) / 2f, SampleAccumulatorOverwriteFactor);
                accumulationCursor++;
            }
        }
        else
        {
            //mono! take everything as-is
            for (int i = 0; i < readSampleCount; i++)
            {
                var index = accumulationCursor % sampleAccumulator.Length;
                sampleAccumulator[index] = Utilities.Lerp(sampleAccumulator[index], samples[i], SampleAccumulatorOverwriteFactor);
                accumulationCursor++;
            }
        }

        if (accumulationCursor >= FftSize) // if the amount of accumulated samples have not yet reached the end 
        {
            var window = new FFT.Windows.Blackman();
            window.ApplyInPlace(sampleAccumulator);

            fft = Transform.FFTmagnitude(sampleAccumulator);

            if (OverlapWindow)
            {
                //shift sampleAccumulator to the left halfway to let the windows overlap
                sampleAccumulator.AsSpan(sampleAccumulator.Length - BufferSize / 2, BufferSize / 2).CopyTo(sampleAccumulator);
                accumulationCursor = BufferSize / 2;
            }
            else
                accumulationCursor = 0;
        }
    }

    public float DecibelScale(float value)
    {
        float db = 20 * MathF.Log10(value);
        db = Utilities.Clamp(db, MinDb, MaxDb);
        float normalizedDb = Utilities.MapRange(MinDb, MaxDb, 0, 1, db);

        return normalizedDb;
    }

    public int FreqToBin(float freq)
    {
        int max = BinCount - 1; 
        int bin = (int)MathF.Round(freq * FftSize / Sound.Data.SampleRate);

        return bin < max ? bin : max;
    }

    public float BinToFreq(int bin)
    {
        var i = bin * Sound.Data.SampleRate / FftSize;
        return i != 0 ? i : 1;
    }

    private void UpdateBars(float dt)
    {
        var minIndex = FreqToBin(MinFreq);
        var maxIndex = FreqToBin(MaxFreq);

        float s = 10 / Smoothing;
        var LogScaleFactor = 2;
        for (int i = 0; i < BarCount; i++)
        {
            float ratio = (float)i / (BarCount - 1);
            float logRatio = (float)MathF.Pow(ratio, LogScaleFactor);
            int freqIndex = (int)Utilities.MapRange(0, 1, minIndex, maxIndex, logRatio);

            var frequency = DecibelScale(fft[freqIndex]);
            var smoothed = Utilities.Lerp(bars[i], frequency, s * dt);
            bars[i] = smoothed;
        }
    }

    public void Update(AudioRenderer audio, float dt)
    {
        UpdateFft(audio);
        UpdateBars(dt);
    }
}