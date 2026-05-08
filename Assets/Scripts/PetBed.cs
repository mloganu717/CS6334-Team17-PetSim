using UnityEngine;

public class PetBed : PetInteractable
{
    [SerializeField] private float energyRestore = 100f;
    [SerializeField] private float cooldown      = 10f;

    [Header("Sleeping")]
    [SerializeField] private AudioClip sleepingClip;
    [SerializeField] private float     sleepingSoundVolume = 30f;
    [SerializeField] private float     catStopDistance     = 0.5f;
    [SerializeField] private float     sleepDuration       = 10f;

    private float _nextUseTime;
    private AudioSource _catAudioSource;
    private CatAIController _cat;

    private CatAIController Cat
    {
        get
        {
            if (_cat == null) _cat = FindAnyObjectByType<CatAIController>();
            return _cat;
        }
    }

    private void Start()
    {
        EnsureCatAudioSource();
    }

    private void EnsureCatAudioSource()
    {
        if (_catAudioSource != null) return;
        if (Cat != null)
            _catAudioSource = Cat.GetComponent<AudioSource>();
    }

    public void PlaySleepAudio()
    {
        EnsureCatAudioSource();
        if (sleepingClip != null && _catAudioSource != null)
            _catAudioSource.PlayOneShot(sleepingClip, sleepingSoundVolume);
    }

    public override void Interact(PetStats pet)
    {
        if (Time.time < _nextUseTime)
        {
            pet.RaiseFeedback("The pet just woke up, let it stay awake a while.");
            return;
        }

        pet.ModifyStat("energy", energyRestore);
        _nextUseTime = Time.time + cooldown;
        pet.RaiseFeedback($"The pet took a nap! Energy +{energyRestore}");

        PlaySleepAudio();
    }

    /// <summary>Called from the object menu Send to Bed button.</summary>
    public void SleepCommand()
    {
        if (Cat == null) return;

        PetStats pet = PetStats.Instance != null
            ? PetStats.Instance
            : FindAnyObjectByType<PetStats>();

        if (pet != null)
            Cat.GoToAndInteract(transform, this, pet, catStopDistance, sleepDuration);
    }

    public override void PlayerQuickInteract()
    {
        PetStats pet = FindAnyObjectByType<PetStats>();
        if (pet != null) Interact(pet);
    }
}
