using UnityEngine;

// water bowl
// restores the pet's thirst when the cat walks over and drinks
public class WaterBowl : PetInteractable
{
    [SerializeField] private float thirstRestore = 40f;
    [SerializeField] private float cooldown = 5f; // seconds before bowl refills
    [SerializeField] private Transform waterMesh; // the water visual, hides when empty

    [Header("Drinking")]
    [SerializeField] private AudioClip drinkingClip; // assign in inspector
    [SerializeField] private float catStopDistance = 0.8f; // how close the cat gets to the bowl

    private float nextUseTime;
    private AudioSource catAudioSource; // plays the drinking sound on the cat
    private CatAIController cat;

    private void Awake()
    {
        // try to find the water mesh automatically if not assigned
        if (waterMesh == null)
        {
            var meshTr = transform.Find("WaterMesh");
            if (meshTr != null) waterMesh = meshTr;
        }
    }

    private void Start()
    {
        // grab cat references at start so we dont have to find them every time
        cat = FindAnyObjectByType<CatAIController>();
        if (cat != null)
            catAudioSource = cat.GetComponent<AudioSource>();
    }

    private void Update()
    {
        // show or hide water mesh depending on cooldown
        if (waterMesh != null)
        {
            bool isFilled = Time.time >= nextUseTime;
            if (waterMesh.gameObject.activeSelf != isFilled)
                waterMesh.gameObject.SetActive(isFilled);
        }
    }

    public override void Interact(PetStats pet)
    {
        // dont let the cat drink if the bowl is on cooldown
        if (Time.time < nextUseTime)
        {
            pet.RaiseFeedback("The water bowl is empty. Let it refill!");
            return;
        }

        // restore thirst and start cooldown
        pet.ModifyStat("thirst", thirstRestore);
        nextUseTime = Time.time + cooldown;
        pet.RaiseFeedback($"The pet drank the water! Thirst +{thirstRestore}. Refilling in {cooldown}s...");

        // play drinking sound on the cat
        if (drinkingClip != null && catAudioSource != null)
            catAudioSource.PlayOneShot(drinkingClip, 10f); 
    }

    // called when player selects the drink button from the object menu
    public void DrinkCommand()
    {
        if (cat == null) return;

        PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
        if (pet != null)
            cat.GoToAndInteract(transform, this, pet, catStopDistance, 4f); // send cat to bowl
    }

    // quick interact just refills the bowl instantly
    public override void PlayerQuickInteract()
    {
        Refill();
    }

    public void Refill()
    {
        nextUseTime = Time.time; // reset cooldown
        if (waterMesh != null) waterMesh.gameObject.SetActive(true);
    }
}