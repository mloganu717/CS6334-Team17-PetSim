using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VetCallManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text vetMessageText;
    [SerializeField] private TMP_Text transcriptText;

    [Header("Fallback Option UI")]
    [SerializeField] private TMP_Text[] optionTexts;
    [SerializeField] private GameObject[] optionButtons;

    [Header("Control Buttons")]
    [SerializeField] private GameObject startCallButton;
    [SerializeField] private GameObject endCallButton;
    [SerializeField] private GameObject homeButton;
    [SerializeField] private GameObject startRecordingButton;
    [SerializeField] private GameObject stopRecordingButton;

    [Header("Backend")]
    [SerializeField] private VetDialogueClient dialogueClient;

    [Header("Voice")]
    [SerializeField] private VetVoiceRecorder voiceRecorder;

    [Header("Fallback")]
    [SerializeField] private MedicineType fallbackPrescription = MedicineType.EnergyMedicine;

    private readonly List<VetDialogueHistoryItem> history = new List<VetDialogueHistoryItem>();
    private VetDialogueResponse currentResponse;
    private Coroutine currentRoutine;
    private bool callActive;

    public void ResetCallScreen()
    {
        StopCurrentRoutine();

        history.Clear();
        currentResponse = null;
        callActive = false;

        if (statusText != null)
            statusText.text = "Press Start Call to begin.";

        if (resultText != null)
            resultText.text = "Prescription will appear here.";

        if (vetMessageText != null)
            vetMessageText.text = "The virtual veterinarian is ready.";

        if (transcriptText != null)
            transcriptText.text = "You said: ";

        SetOptionsVisible(false);
        SetCallControls(
            startVisible: true,
            endVisible: false,
            homeVisible: true,
            startRecordVisible: false,
            stopRecordVisible: false
        );
    }

    public void StartDemoVetCall()
    {
        StopCurrentRoutine();

        history.Clear();
        callActive = true;

        if (statusText != null)
            statusText.text = "Connecting to virtual vet...";

        if (resultText != null)
            resultText.text = "";

        if (vetMessageText != null)
            vetMessageText.text = "";

        if (transcriptText != null)
            transcriptText.text = "You said: ";

        SetOptionsVisible(false);
        SetCallControls(
            startVisible: false,
            endVisible: true,
            homeVisible: true,
            startRecordVisible: false,
            stopRecordVisible: false
        );

        currentRoutine = StartCoroutine(RequestInitialDialogue());
    }

    public void EndVetCall()
    {
        StopCurrentRoutine();

        callActive = false;

        if (voiceRecorder != null && voiceRecorder.IsRecording)
        {
            voiceRecorder.StopRecordingAndGetWav();
        }

        if (statusText != null)
            statusText.text = "Call ended.";

        SetOptionsVisible(false);
        SetCallControls(
            startVisible: true,
            endVisible: false,
            homeVisible: true,
            startRecordVisible: false,
            stopRecordVisible: false
        );
    }

    public void StartVoiceRecording()
    {
        if (!callActive)
            return;

        if (voiceRecorder == null)
        {
            if (statusText != null)
                statusText.text = "Voice recorder is not assigned.";
            return;
        }

        bool started = voiceRecorder.StartRecording();

        if (started)
        {
            if (statusText != null)
                statusText.text = "Recording... Press Stop Recording when finished.";

            SetCallControls(
                startVisible: false,
                endVisible: true,
                homeVisible: true,
                startRecordVisible: false,
                stopRecordVisible: true
            );
        }
        else
        {
            if (statusText != null)
                statusText.text = "Microphone permission needed. Try Start Recording again after accepting.";
        }
    }

    public void StopVoiceRecording()
    {
        if (!callActive)
            return;

        if (voiceRecorder == null)
            return;

        byte[] wavBytes = voiceRecorder.StopRecordingAndGetWav();

        if (wavBytes == null || wavBytes.Length == 0)
        {
            if (statusText != null)
                statusText.text = "No audio recorded. Try again.";

            SetCallControls(
                startVisible: false,
                endVisible: true,
                homeVisible: true,
                startRecordVisible: true,
                stopRecordVisible: false
            );

            return;
        }

        if (statusText != null)
            statusText.text = "Transcribing your response...";

        SetCallControls(
            startVisible: false,
            endVisible: true,
            homeVisible: true,
            startRecordVisible: false,
            stopRecordVisible: false
        );

        StopCurrentRoutine();
        currentRoutine = StartCoroutine(RequestDialogueFromVoice(wavBytes));
    }

    public void SelectOption(int optionIndex)
    {
        if (!callActive)
            return;

        if (currentResponse == null)
            return;

        if (currentResponse.playerOptions == null)
            return;

        if (optionIndex < 0 || optionIndex >= currentResponse.playerOptions.Length)
            return;

        string selected = currentResponse.playerOptions[optionIndex];

        history.Add(new VetDialogueHistoryItem("player", selected));

        if (transcriptText != null)
            transcriptText.text = "You said: " + selected;

        if (statusText != null)
            statusText.text = "Sending response...";

        SetOptionsVisible(false);

        StopCurrentRoutine();
        currentRoutine = StartCoroutine(RequestInitialDialogue());
    }

    private IEnumerator RequestInitialDialogue()
    {
        if (dialogueClient == null)
        {
            UseLocalFallback();
            yield break;
        }

        VetDialogueRequest request = new VetDialogueRequest();
        request.history = history;
        request.petStats = GetPetStatsSnapshot();

        yield return dialogueClient.GetNextDialogue(
            request,
            HandleResponse,
            error =>
            {
                Debug.LogWarning("Vet backend failed: " + error);
                UseLocalFallback();
            }
        );
    }

    private IEnumerator RequestDialogueFromVoice(byte[] wavBytes)
    {
        if (dialogueClient == null)
        {
            UseLocalFallback();
            yield break;
        }

        VetDialogueRequest request = new VetDialogueRequest();
        request.history = history;
        request.petStats = GetPetStatsSnapshot();

        yield return dialogueClient.GetNextDialogueFromAudio(
            wavBytes,
            request,
            HandleVoiceResponse,
            error =>
            {
                Debug.LogWarning("Voice vet backend failed: " + error);

                if (statusText != null)
                    statusText.text = "Voice request failed. Try again.";

                SetCallControls(
                    startVisible: false,
                    endVisible: true,
                    homeVisible: true,
                    startRecordVisible: true,
                    stopRecordVisible: false
                );
            }
        );
    }

    private void HandleVoiceResponse(VetDialogueResponse response)
    {
        if (response != null && !string.IsNullOrEmpty(response.userTranscript))
        {
            history.Add(new VetDialogueHistoryItem("player", response.userTranscript));

            if (transcriptText != null)
                transcriptText.text = "You said: " + response.userTranscript;
        }

        HandleResponse(response);
    }

    private void HandleResponse(VetDialogueResponse response)
    {
        currentResponse = response;

        if (response == null)
        {
            UseLocalFallback();
            return;
        }

        if (!string.IsNullOrEmpty(response.vetMessage))
        {
            history.Add(new VetDialogueHistoryItem("vet", response.vetMessage));
        }

        if (statusText != null)
        {
            statusText.text = response.isComplete
                ? "Vet consultation complete."
                : "Vet is waiting for your voice response.";
        }

        if (vetMessageText != null)
            vetMessageText.text = response.vetMessage;

        if (response.isComplete)
        {
            callActive = false;
            SetOptionsVisible(false);

            SetCallControls(
                startVisible: true,
                endVisible: false,
                homeVisible: true,
                startRecordVisible: false,
                stopRecordVisible: false
            );

            ApplyPrescription(response.prescription, response.feedback);
        }
        else
        {
            ShowOptions(response.playerOptions);

            SetCallControls(
                startVisible: false,
                endVisible: true,
                homeVisible: true,
                startRecordVisible: true,
                stopRecordVisible: false
            );
        }

        RefreshVisibleButtonColliders();
    }

    private void ShowOptions(string[] options)
    {
        // For voice mode, keep option buttons hidden.
        // Leave this method here as fallback if you want to re-enable text choices later.
        SetOptionsVisible(false);
    }

    private void SetOptionsVisible(bool visible)
    {
        if (optionButtons == null)
            return;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
                optionButtons[i].SetActive(visible);
        }
    }

    private void SetCallControls(
        bool startVisible,
        bool endVisible,
        bool homeVisible,
        bool startRecordVisible,
        bool stopRecordVisible)
    {
        if (startCallButton != null)
            startCallButton.SetActive(startVisible);

        if (endCallButton != null)
            endCallButton.SetActive(endVisible);

        if (homeButton != null)
            homeButton.SetActive(homeVisible);

        if (startRecordingButton != null)
            startRecordingButton.SetActive(startRecordVisible);

        if (stopRecordingButton != null)
            stopRecordingButton.SetActive(stopRecordVisible);

        RefreshVisibleButtonColliders();
    }

    private VetPetStatsSnapshot GetPetStatsSnapshot()
    {
        VetPetStatsSnapshot snapshot = new VetPetStatsSnapshot();

        PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

        if (pet != null)
        {
            snapshot.hunger = pet.Hunger;
            snapshot.thirst = pet.Thirst;
            snapshot.happiness = pet.Happiness;
            snapshot.energy = pet.Energy;
            snapshot.hygiene = pet.Hygiene;
        }

        return snapshot;
    }

    private void ApplyPrescription(string prescription, string feedback)
    {
        MedicineType medicine;

        if (!System.Enum.TryParse(prescription, out medicine))
            medicine = fallbackPrescription;

        PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

        if (pet == null)
            return;

        switch (medicine)
        {
            case MedicineType.NoMedicineNeeded:
                pet.RaiseFeedback("Vet: No medicine needed.");
                break;

            case MedicineType.HungerMedicine:
                pet.ModifyStat("hunger", 50f);
                pet.RaiseFeedback("Vet prescribed Hunger Medicine.");
                break;

            case MedicineType.ThirstMedicine:
                pet.ModifyStat("thirst", 50f);
                pet.RaiseFeedback("Vet prescribed Hydration Drops.");
                break;

            case MedicineType.EnergyMedicine:
                pet.ModifyStat("energy", 50f);
                pet.RaiseFeedback("Vet prescribed Energy Medicine.");
                break;

            case MedicineType.HappinessMedicine:
                pet.ModifyStat("happiness", 50f);
                pet.RaiseFeedback("Vet prescribed Happiness Medicine.");
                break;

            case MedicineType.HygieneMedicine:
                pet.ModifyStat("hygiene", 50f);
                pet.RaiseFeedback("Vet prescribed Hygiene Medicine.");
                break;
        }

        if (resultText != null)
        {
            resultText.text =
                "Prescription: " + medicine +
                "\n\n" + feedback;
        }
    }

    private void UseLocalFallback()
    {
        callActive = false;

        if (statusText != null)
            statusText.text = "Vet consultation complete.";

        if (vetMessageText != null)
            vetMessageText.text = "Your cat seems low on energy. I recommend Energy Medicine.";

        if (resultText != null)
            resultText.text = "Prescription: Energy Medicine";

        SetOptionsVisible(false);

        SetCallControls(
            startVisible: true,
            endVisible: false,
            homeVisible: true,
            startRecordVisible: false,
            stopRecordVisible: false
        );

        ApplyPrescription(fallbackPrescription.ToString(), "Fallback prescription applied.");
    }

    private void StopCurrentRoutine()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
    }

    private void RefreshVisibleButtonColliders()
    {
        Canvas.ForceUpdateCanvases();

        MenuButtonTarget[] buttons = GetComponentsInChildren<MenuButtonTarget>(true);

        foreach (MenuButtonTarget button in buttons)
        {
            if (button.gameObject.activeInHierarchy)
                button.UpdateBoxCollider();
        }
    }
}