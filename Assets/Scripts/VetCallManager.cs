using System.Collections;
using TMPro;
using UnityEngine;

public class VetCallManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text resultText;

    [Header("Demo Behavior")]
    [SerializeField] private MedicineType demoPrescription = MedicineType.EnergyMedicine;
    [SerializeField] private float connectDelay = 1.0f;
    [SerializeField] private float listeningDelay = 1.5f;
    [SerializeField] private float analyzingDelay = 1.5f;

    [Header("Optional Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip callStartedClip;
    [SerializeField] private AudioClip callEndedClip;

    private Coroutine currentCallRoutine;
    private bool callInProgress;

    public void StartDemoVetCall()
    {
        if (currentCallRoutine != null)
        {
            StopCoroutine(currentCallRoutine);
        }

        currentCallRoutine = StartCoroutine(DemoVetCallRoutine());
    }

    public void EndVetCall()
    {
        if (currentCallRoutine != null)
        {
            StopCoroutine(currentCallRoutine);
            currentCallRoutine = null;
        }

        callInProgress = false;

        if (statusText != null)
        {
            statusText.text = "Call ended.";
        }

        if (resultText != null)
        {
            resultText.text = "";
        }

        if (audioSource != null && callEndedClip != null)
        {
            audioSource.PlayOneShot(callEndedClip);
        }
    }

    public void ResetCallScreen()
    {
        callInProgress = false;

        if (currentCallRoutine != null)
        {
            StopCoroutine(currentCallRoutine);
            currentCallRoutine = null;
        }

        if (statusText != null)
        {
            statusText.text = "Press Start Call to begin.";
        }

        if (resultText != null)
        {
            resultText.text = "Prescription will appear here.";
        }
    }

    private IEnumerator DemoVetCallRoutine()
    {
        callInProgress = true;

        if (audioSource != null && callStartedClip != null)
        {
            audioSource.PlayOneShot(callStartedClip);
        }

        if (statusText != null)
        {
            statusText.text = "Connecting to virtual vet...";
        }

        if (resultText != null)
        {
            resultText.text = "";
        }

        yield return new WaitForSeconds(connectDelay);

        if (statusText != null)
        {
            statusText.text = "Listening to your pet concern...";
        }

        yield return new WaitForSeconds(listeningDelay);

        if (statusText != null)
        {
            statusText.text = "Analyzing symptoms...";
        }

        yield return new WaitForSeconds(analyzingDelay);

        ApplyMedicine(demoPrescription);

        if (statusText != null)
        {
            statusText.text = "Vet consultation complete.";
        }

        if (resultText != null)
        {
            resultText.text = "Vet recommends: " + GetMedicineDisplayName(demoPrescription);
        }

        callInProgress = false;
        currentCallRoutine = null;
    }

    private void ApplyMedicine(MedicineType medicine)
    {
        PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

        if (pet == null)
        {
            Debug.LogWarning("No PetStats object found. Medicine effect was not applied.");
            return;
        }

        switch (medicine)
        {
            case MedicineType.NoMedicineNeeded:
                pet.RaiseFeedback("Vet: No medicine needed right now.");
                break;

            case MedicineType.HungerMedicine:
                pet.ModifyStat("hunger", 50f);
                pet.RaiseFeedback("Vet prescribed Hunger Medicine. Hunger improved.");
                break;

            case MedicineType.ThirstMedicine:
                pet.ModifyStat("thirst", 50f);
                pet.RaiseFeedback("Vet prescribed Hydration Drops. Thirst improved.");
                break;

            case MedicineType.EnergyMedicine:
                pet.ModifyStat("energy", 50f);
                pet.RaiseFeedback("Vet prescribed Energy Medicine. Energy improved.");
                break;

            case MedicineType.HappinessMedicine:
                pet.ModifyStat("happiness", 50f);
                pet.RaiseFeedback("Vet prescribed Happiness Medicine. Mood improved.");
                break;
        }
    }

    private string GetMedicineDisplayName(MedicineType medicine)
    {
        switch (medicine)
        {
            case MedicineType.NoMedicineNeeded:
                return "No Medicine Needed";

            case MedicineType.HungerMedicine:
                return "Hunger Medicine";

            case MedicineType.ThirstMedicine:
                return "Hydration Drops";

            case MedicineType.EnergyMedicine:
                return "Energy Medicine";

            case MedicineType.HappinessMedicine:
                return "Happiness Medicine";

            default:
                return "Unknown Medicine";
        }
    }
}