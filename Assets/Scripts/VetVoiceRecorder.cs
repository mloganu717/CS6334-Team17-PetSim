using System;
using System.IO;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class VetVoiceRecorder : MonoBehaviour
{
    [Header("Recording")]
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int maxRecordingSeconds = 10;

    private AudioClip recordingClip;
    private string microphoneDevice;
    private bool isRecording;

    public bool IsRecording => isRecording;

    public bool HasMicrophonePermission()
    {
#if UNITY_ANDROID
        return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
        return true;
#endif
    }

    public void RequestMicrophonePermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif
    }

    public bool StartRecording()
    {
        if (isRecording)
            return true;

        if (!HasMicrophonePermission())
        {
            RequestMicrophonePermission();
            Debug.LogWarning("Microphone permission requested. Press Start Recording again after accepting.");
            return false;
        }

        if (Microphone.devices == null || Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No microphone devices found.");
            return false;
        }

        microphoneDevice = Microphone.devices[0];

        recordingClip = Microphone.Start(
            microphoneDevice,
            false,
            maxRecordingSeconds,
            sampleRate
        );

        isRecording = recordingClip != null;

        return isRecording;
    }

    public byte[] StopRecordingAndGetWav()
    {
        if (!isRecording || recordingClip == null)
            return null;

        int recordedSamples = Microphone.GetPosition(microphoneDevice);

        Microphone.End(microphoneDevice);
        isRecording = false;

        if (recordedSamples <= 0)
        {
            Debug.LogWarning("Recording had zero samples.");
            return null;
        }

        return EncodeWav(recordingClip, recordedSamples);
    }

    private byte[] EncodeWav(AudioClip clip, int sampleCount)
    {
        int channels = clip.channels;
        float[] samples = new float[sampleCount * channels];
        clip.GetData(samples, 0);

        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];

        const float rescaleFactor = 32767f;

        for (int i = 0; i < samples.Length; i++)
        {
            float clamped = Mathf.Clamp(samples[i], -1f, 1f);
            intData[i] = (short)(clamped * rescaleFactor);

            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        using (MemoryStream stream = new MemoryStream())
        {
            int hz = clip.frequency;

            WriteString(stream, "RIFF");
            WriteInt(stream, 36 + bytesData.Length);
            WriteString(stream, "WAVE");

            WriteString(stream, "fmt ");
            WriteInt(stream, 16);
            WriteShort(stream, 1);
            WriteShort(stream, (short)channels);
            WriteInt(stream, hz);
            WriteInt(stream, hz * channels * 2);
            WriteShort(stream, (short)(channels * 2));
            WriteShort(stream, 16);

            WriteString(stream, "data");
            WriteInt(stream, bytesData.Length);
            stream.Write(bytesData, 0, bytesData.Length);

            return stream.ToArray();
        }
    }

    private void WriteString(Stream stream, string value)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private void WriteInt(Stream stream, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private void WriteShort(Stream stream, short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}