using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class VetDialogueClient : MonoBehaviour
{
    [SerializeField] private string backendBaseUrl = "http://localhost:3000";
    [SerializeField] private string demoToken = "";

    public IEnumerator GetNextDialogue(
        VetDialogueRequest requestData,
        Action<VetDialogueResponse> onSuccess,
        Action<string> onError)
    {
        string url = backendBaseUrl + "/api/vet/next";
        string json = JsonUtility.ToJson(requestData);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(demoToken))
            {
                request.SetRequestHeader("x-demo-token", demoToken);
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
                yield break;
            }

            VetDialogueResponse response =
                JsonUtility.FromJson<VetDialogueResponse>(request.downloadHandler.text);

            if (response == null)
            {
                onError?.Invoke("Could not parse vet dialogue response.");
                yield break;
            }

            onSuccess?.Invoke(response);
        }
    }

    public IEnumerator GetNextDialogueFromAudio(
        byte[] wavBytes,
        VetDialogueRequest requestData,
        Action<VetDialogueResponse> onSuccess,
        Action<string> onError)
    {
        if (wavBytes == null || wavBytes.Length == 0)
        {
            onError?.Invoke("No audio data to send.");
            yield break;
        }

        string url = backendBaseUrl + "/api/vet/voice-next";

        WWWForm form = new WWWForm();
        form.AddField("requestJson", JsonUtility.ToJson(requestData));
        form.AddBinaryData("audio", wavBytes, "player_response.wav", "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            if (!string.IsNullOrEmpty(demoToken))
            {
                request.SetRequestHeader("x-demo-token", demoToken);
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(request.error + "\n" + request.downloadHandler.text);
                yield break;
            }

            VetDialogueResponse response =
                JsonUtility.FromJson<VetDialogueResponse>(request.downloadHandler.text);

            if (response == null)
            {
                onError?.Invoke("Could not parse voice dialogue response.");
                yield break;
            }

            onSuccess?.Invoke(response);
        }
    }

    public void SetBackendBaseUrl(string url)
    {
        backendBaseUrl = url;
    }
}