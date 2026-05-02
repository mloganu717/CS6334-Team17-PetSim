using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MicrophoneRecorder : MonoBehaviour
{
    private AudioClip _clip;
    private bool _isRecording = false;
    private Action<string> _onTranscribed;
    private const int RecordSeconds = 6;

    public void StartListening(Action<string> callback)
    {
        if (_isRecording) return;
        _onTranscribed = callback;
        _isRecording = true;
        _clip = Microphone.Start(null, false, RecordSeconds, 16000);
        StartCoroutine(StopAfterSilence());
    }

    public void StopListening()
    {
        _isRecording = false;
        Microphone.End(null);
        StopAllCoroutines();
    }

    private IEnumerator StopAfterSilence()
    {
        yield return new WaitForSeconds(RecordSeconds);
        if (!_isRecording) yield break;
        Microphone.End(null);
        _isRecording = false;
        byte[] wavData = WavUtility.FromAudioClip(_clip);
        StartCoroutine(SendToWhisper(wavData));
    }

    private IEnumerator SendToWhisper(byte[] wavData)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        using var req = UnityWebRequest.Post(
            "https://api.openai.com/v1/audio/transcriptions", form);
        req.SetRequestHeader("Authorization", "Bearer YOUR_OPENAI_KEY");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var json = JsonUtility.FromJson<WhisperResponse>(req.downloadHandler.text);
            _onTranscribed?.Invoke(json.text);
        }
    }

    [Serializable] private class WhisperResponse { public string text; }
}