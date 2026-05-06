using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class VetDialogueClient : MonoBehaviour
{
    [SerializeField] private string backendBaseUrl = "http://localhost:3000";

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

    public void SetBackendBaseUrl(string url)
    {
        backendBaseUrl = url;
    }
}