using UnityEngine;

public class CatObjectMenu : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuObject;

    [Header("Pet Settings")]
    [SerializeField] private float happinessBoost = 15f;
    [SerializeField] private AudioClip purrClip;

    [Header("Play Settings")]
    [SerializeField] private string toyBallName = "ToyBall";
    [SerializeField] private SettingsMenu settingsMenu;    
    private AudioSource audioSource;
    private CatAIController catAI;

    private void Awake()
    {
        catAI = GetComponent<CatAIController>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.playOnAwake = false;
    }

    public void PetCat()
    {
        if (PetStats.Instance != null)
        {
            PetStats.Instance.ModifyStat("happiness", happinessBoost);
            PetStats.Instance.RaiseFeedback("The cat purrs happily!");
        }

        if (purrClip != null && audioSource != null)
            audioSource.PlayOneShot(purrClip);

        CloseMenu();
    }

    public void PlayWithCat()
    {
        GameObject toyBall = GameObject.Find(toyBallName);
        if (toyBall == null)
        {
            Debug.LogWarning("ToyBall not found in scene.");
            CloseMenu();
            return;
        }

        if (catAI != null)
        {
            var interactable = toyBall.GetComponent<PetInteractable>();
            if (interactable == null)
                interactable = toyBall.GetComponentInChildren<PetInteractable>();

            PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

            if (interactable != null && pet != null)
                catAI.GoToAndInteract(toyBall.transform, interactable, pet);
            else
                Debug.LogWarning("ToyBall has no PetInteractable or PetStats not found.");
        }

        CloseMenu();
    }

    public void ShowStats() //stats card
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