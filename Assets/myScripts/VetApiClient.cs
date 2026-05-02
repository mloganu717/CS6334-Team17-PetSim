using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class VetAPIClient : MonoBehaviour
{
    private const string API_URL = "https://api.anthropic.com/v1/messages";
    private const string API_KEY = "YOUR_ANTHROPIC_KEY";
    private const string MODEL = "claude-sonnet-4-20250514";

    private const string SYSTEM_PROMPT =
        "You are a friendly veterinary assistant in a game. " +
        "Answer pet health questions helpfully and concisely in 1-2 sentences. " +
        "Always recommend seeing a real vet for serious concerns.";

    public IEnumerator SendToVet(string userMessage, Action<string> onResponse)
    {
        var body = new RequestBody
        {
            model = MODEL,
            max_tokens = 200,
            system = SYSTEM_PROMPT,
            messages = new[] { new Message { role = "user", content = userMessage } }
        };

        string json = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest(API_URL, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("x-api-key", API_KEY);
        req.SetRequestHeader("anthropic-version", "2023-06-01");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<ApiResponse>(req.downloadHandler.text);
            onResponse?.Invoke(resp.content[0].text);
        }
    }

    [Serializable]
    class RequestBody
    {
        public string model, system;
        public int max_tokens;
        public Message[] messages;
    }
    [Serializable] class Message { public string role, content; }
    [Serializable] class ApiResponse { public ContentBlock[] content; }
    [Serializable] class ContentBlock { public string text; }
}