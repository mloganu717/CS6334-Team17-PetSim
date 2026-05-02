using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using var stream = new MemoryStream();
        int sampleCount = clip.samples * clip.channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        using var writer = new BinaryWriter(stream);

        // WAV header
        writer.Write(new char[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + sampleCount * 2);
        writer.Write(new char[] { 'W', 'A', 'V', 'E' });
        writer.Write(new char[] { 'f', 'm', 't', ' ' });
        writer.Write(16);
        writer.Write((ushort)1);                          // PCM
        writer.Write((ushort)clip.channels);
        writer.Write(clip.frequency);
        writer.Write(clip.frequency * clip.channels * 2);
        writer.Write((ushort)(clip.channels * 2));
        writer.Write((ushort)16);                         // bits per sample
        writer.Write(new char[] { 'd', 'a', 't', 'a' });
        writer.Write(sampleCount * 2);

        // Sample data
        foreach (var sample in samples)
        {
            short s = (short)Mathf.Clamp(sample * 32767f, short.MinValue, short.MaxValue);
            writer.Write(s);
        }

        return stream.ToArray();
    }
}