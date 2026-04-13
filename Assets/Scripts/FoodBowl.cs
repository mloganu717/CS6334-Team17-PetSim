using UnityEngine;

// food bowl
// restores the pet's hunger, has limited servings before needing a refill
public class FoodBowl : PetInteractable
{
    [SerializeField] private float hungerRestore = 30f;
    [SerializeField] private int maxServings = 3;
    [SerializeField] private Transform foodMesh; // visual indicator, shrinks as food depletes

    [Header("Eating")]
    [SerializeField] private AudioClip eatingClip; // assign in inspector
    [SerializeField] private float eatingSoundVolume = 3f; // loud by default
    [SerializeField] private float catStopDistance = 0.8f; // how close cat gets to bowl

    private int servingsLeft;
    private Vector3 initialMeshScale;
    private AudioSource catAudioSource;
    private CatAIController cat;

    private void Awake()
    {
        servingsLeft = maxServings;

        // try to find food mesh automatically if not assigned
        if (foodMesh == null)
        {
            var meshTr = transform.Find("FoodMesh");
            if (meshTr != null) foodMesh = meshTr;
        }

        if (foodMesh != null)
            initialMeshScale = foodMesh.localScale;

        UpdateVisuals();
    }

    private void Start()
    {
        // grab cat references at start
        cat = FindAnyObjectByType<CatAIController>();
        if (cat != null)
            catAudioSource = cat.GetComponent<AudioSource>();
    }

    public override void Interact(PetStats pet)
    {
        // if bowl is empty just refill
        if (servingsLeft <= 0)
        {
            Refill();
            pet.RaiseFeedback("You refilled the food bowl!");
            return;
        }

        // restore hunger and use a serving
        pet.ModifyStat("hunger", hungerRestore);
        servingsLeft--;
        pet.RaiseFeedback($"The pet ate! Hunger +{hungerRestore} ({servingsLeft} servings left)");
        UpdateVisuals();

        // play eating sound on the cat
        if (eatingClip != null && catAudioSource != null)
            catAudioSource.PlayOneShot(eatingClip, eatingSoundVolume);
    }

    // called when player selects the eat button from the object menu
    public void EatCommand()
    {
        if (cat == null) return;

        PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
        if (pet != null)
            cat.GoToAndInteract(transform, this, pet, catStopDistance, 8f);
    }

    // quick interact just refills the bowl
    public override void PlayerQuickInteract()
    {
        Refill();
    }

    public void Refill()
    {
        servingsLeft = maxServings;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (foodMesh != null)
        {
            float fillPct = (float)servingsLeft / maxServings;
            foodMesh.gameObject.SetActive(fillPct > 0);

            // scale height of food down as it depletes
            foodMesh.localScale = new Vector3(
                initialMeshScale.x,
                initialMeshScale.y * fillPct,
                initialMeshScale.z
            );
        }
    }
}