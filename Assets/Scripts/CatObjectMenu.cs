using UnityEngine;

public class CatObjectMenu : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuObject;

    [Header("Pet settings")]
    [SerializeField] private float happinessBoost = 15f;
    [SerializeField] private AudioClip purrClip;

    [Header("Play settings")]
    [SerializeField] private string toyBallName = "ToyBall";
    [SerializeField] private SettingsMenu settingsMenu;

    private AudioSource _audioSource;
    private CatAIController _catAI;
    private CatMood _catMood;

    private void Awake()
    {
        _catAI   = GetComponent<CatAIController>();
        _catMood = GetComponent<CatMood>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.spatialBlend = 1f;
        _audioSource.playOnAwake  = false;
    }

    public void PetCat()
    {
        if (PetStats.Instance != null)
        {
            PetStats.Instance.ModifyStat("happiness", happinessBoost);
            PetStats.Instance.RaiseFeedback("The cat purrs happily!");
        }

        _catMood?.Reward(4f);

        if (purrClip != null && _audioSource != null)
            _audioSource.PlayOneShot(purrClip);

        CloseMenu();
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
