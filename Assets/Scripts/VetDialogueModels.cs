using System;
using System.Collections.Generic;

[Serializable]
public class VetDialogueRequest
{
    public List<VetDialogueHistoryItem> history = new List<VetDialogueHistoryItem>();
    public VetPetStatsSnapshot petStats = new VetPetStatsSnapshot();
}

[Serializable]
public class VetDialogueHistoryItem
{
    public string role;
    public string content;

    public VetDialogueHistoryItem(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}

[Serializable]
public class VetPetStatsSnapshot
{
    public float hunger;
    public float thirst;
    public float happiness;
    public float energy;
    public float hygiene;
}

[Serializable]
public class VetDialogueResponse
{
    public string vetMessage;
    public string[] playerOptions;
    public bool isComplete;
    public string prescription;
    public string feedback;
    public string userTranscript;
}