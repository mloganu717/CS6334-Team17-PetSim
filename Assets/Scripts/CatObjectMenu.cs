using UnityEngine;

public class CatObjectMenu : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuObject;

    [Header("Pet Settings")]
    [SerializeField] private float happinessBoost = 15f;

    [Header("Pet Audio - assign clips that reflect mood")]
    [SerializeField] private AudioClip purrClip;        // happy 
    [SerializeField] private AudioClip contentClip;     //  content
    [SerializeField] private AudioClip angryClip;       // wary — hiss or growl

    [Header("Pet Audio Thresholds")]
    [Tooltip("Happiness above this plays the purr clip")]
    [SerializeField] private float happyAudioThreshold = 70f;
    [Tooltip("Happiness above this plays the content clip")]
    [SerializeField] private float contentAudioThreshold = 40f;

    [Header("Play Settings")]
    [SerializeField] private string toyBallName = "ToyBall";
    [SerializeField] private SettingsMenu settingsMenu;

    private AudioSource _audioSource;
    private CatAIController _catAI;
    private CatMood _catMood;

    private void Awake()
    {
        _catAI = GetComponent<CatAIController>();
        _catMood = GetComponent<CatMood>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.spatialBlend = 1f;
        _audioSource.playOnAwake = false;
    }

    public void PetCat()
    {
        PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

        // Read happiness before the boost so audio reflects current mood
        float currentHappiness = pet != null ? pet.Happiness : 50f;

        if (pet != null)
        {
            pet.ModifyStat("happiness", happinessBoost);
            pet.RaiseFeedback(PetFeedbackMessage(currentHappiness));
        }

        _catMood?.Reward(4f);

        PlayPetAudio(currentHappiness);

        CloseMenu();
    }

    private void PlayPetAudio(float happiness)
    {
        if (_audioSource == null) return;

        AudioClip clip;

        if (_catMood != null && _catMood.IsWary)
            clip = angryClip;
        else if (happiness >= happyAudioThreshold)
            clip = purrClip;
        else if (happiness >= contentAudioThreshold)
            clip = contentClip;
        else
            clip = angryClip;

        if (clip != null)
            _audioSource.PlayOneShot(clip);
    }

    private string PetFeedbackMessage(float happiness)
    {
        if (_catMood != null && _catMood.IsWary)
            return "The cat hisses and backs away!";
        if (happiness >= happyAudioThreshold)
            return "The cat purrs loudly and leans in!";
        if (happiness >= contentAudioThreshold)
            return "The cat seems okay with being pet.";
        return "The cat looks angry and swipes at you!";
    }

    public void PlayWithCat()
    {
        GameObject toyBall = GameObject.Find(toyBallName);
        if (toyBall == null)
        {
            Debug.LogWarning("ToyBall not found in scene. Make sure it's named: " + toyBallName);
            CloseMenu();
            return;
        }

        var interactable = toyBall.GetComponent<PetInteractable>()
                        ?? toyBall.GetComponentInChildren<PetInteractable>();
        PetStats pet = PetStats.Instance != null
            ? PetStats.Instance
            : FindAnyObjectByType<PetStats>();

        if (_catAI != null && interactable != null && pet != null)
            _catAI.GoToAndInteract(toyBall.transform, interactable, pet);
        else
            Debug.LogWarning("CatObjectMenu.PlayWithCat: missing CatAI, PetInteractable, or PetStats.");

        CloseMenu();
    }

    public void ShowStats()
    {
        if (settingsMenu != null)
            settingsMenu.OpenPetStatsCardStandalone();

        CloseMenu();
    }

    public void CloseMenu()
    {
        if (menuObject != null)
            menuObject.SetActive(false);
    }

    public void OpenMenu()
    {
        if (menuObject != null)
            menuObject.SetActive(true);
    }
}