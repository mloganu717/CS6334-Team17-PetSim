using UnityEngine;

public class WaterBowl : PetInteractable
{
    [SerializeField] private float     thirstRestore = 100f;
    [SerializeField] private float     cooldown      = 5f;
    [SerializeField] private Transform waterMesh;

    [Header("Drinking")]
    [SerializeField] private AudioClip drinkingClip;
    [SerializeField] private float     catStopDistance = 0.8f;

    private float       _nextUseTime;
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

    private void Awake()
    {
        if (waterMesh == null)
        {
            var meshTr = transform.Find("WaterMesh");
            if (meshTr != null) waterMesh = meshTr;
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

    public void PlayDrinkAudio()
    {
        EnsureCatAudioSource();
        if (drinkingClip != null && _catAudioSource != null)
            _catAudioSource.PlayOneShot(drinkingClip, 10f);
    }

    private void Update()
    {
        if (waterMesh != null)
        {
            bool isFilled = Time.time >= _nextUseTime;
            if (waterMesh.gameObject.activeSelf != isFilled)
                waterMesh.gameObject.SetActive(isFilled);
        }
    }

    public override void Interact(PetStats pet)
    {
        if (Time.time < _nextUseTime)
        {
            pet.RaiseFeedback("The water bowl is empty. Let it refill!");
            return;
        }

        FindAnyObjectByType<CatNeeds>()?.Drink();
        pet.ModifyStat("thirst", thirstRestore);
        _nextUseTime = Time.time + cooldown;
        pet.RaiseFeedback($"The pet drank! Thirst +{thirstRestore}. Refilling in {cooldown}s...");

        PlayDrinkAudio();
    }

    public void DrinkCommand()
    {
        if (Time.time < _nextUseTime)
        {
            PetStats.Instance?.RaiseFeedback("The water bowl is empty! Quick interact to refill.");
            return;
        }

        if (Cat == null) return;

        PetStats pet = PetStats.Instance != null
            ? PetStats.Instance
            : FindAnyObjectByType<PetStats>();

        if (pet != null)
            Cat.GoToAndInteract(transform, this, pet, catStopDistance, 4f);
    }

    public override void PlayerQuickInteract()
    {
        Refill();
        PetStats p = PetStats.Instance ?? FindAnyObjectByType<PetStats>();
        p?.RaiseFeedback("Water ready. Thirst goes up when the cat drinks (menu or autonomous), not from refilling alone.");
    }

    public void Refill()
    {
        _nextUseTime = Time.time;
        if (waterMesh != null)
            waterMesh.gameObject.SetActive(true);
    }
}
