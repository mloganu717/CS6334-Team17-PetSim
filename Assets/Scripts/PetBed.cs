using UnityEngine;

// pet bed
// cat walks over and stays on top, plays sleeping sound
public class PetBed : PetInteractable
{
    [SerializeField] private float energyRestore = 50f;
    [SerializeField] private float cooldown = 10f;

    [Header("Sleeping")]
    [SerializeField] private AudioClip sleepingClip; // assign in inspector
    [SerializeField] private float sleepingSoundVolume = 30f; // loud
    [SerializeField] private float catStopDistance = 0.5f; // get right on top of bed
    [SerializeField] private float sleepDuration = 10f; // how long cat stays on bed

    private float nextUseTime;
    private AudioSource catAudioSource;
    private CatAIController cat;

    private void Start()
    {
        cat = FindAnyObjectByType<CatAIController>();
        if (cat != null)
            catAudioSource = cat.GetComponent<AudioSource>();
    }

    public override void Interact(PetStats pet)
    {
        
        if (Time.time < nextUseTime)
        {
            pet.RaiseFeedback("The pet just woke up, let it stay awake a while.");
            return;
        }

        pet.ModifyStat("energy", energyRestore);
        nextUseTime = Time.time + cooldown;
        pet.RaiseFeedback($"The pet took a nap! Energy +{energyRestore}");

        // play sleeping sound on cat
        if (sleepingClip != null && catAudioSource != null)
            catAudioSource.PlayOneShot(sleepingClip, sleepingSoundVolume);
        Debug.Log("sleepingClip: " + sleepingClip + " | catAudioSource: " + catAudioSource);
    }

    // called from object menu Send to Bed button
    public void SleepCommand()
    {
        if (cat == null) return;

        PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
        if (pet != null)
            cat.GoToAndInteract(transform, this, pet, catStopDistance, sleepDuration);
    }

    public override void PlayerQuickInteract()
    {
        PetStats pet = FindAnyObjectByType<PetStats>();
        if (pet != null) Interact(pet);
    }
}