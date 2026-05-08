using UnityEngine;

public class FoodBowl : PetInteractable
{
    [SerializeField] private float hungerRestore = 100f;
    [SerializeField] private int   maxServings   = 3;
    [SerializeField] private Transform foodMesh;

    [Header("Eating")]
    [SerializeField] private AudioClip eatingClip;
    [SerializeField] private float     eatingSoundVolume = 3f;
    [SerializeField] private float     catStopDistance   = 0.8f;

    private int         _servingsLeft;
    private Vector3     _initialMeshScale;
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
        _servingsLeft = 0;

        if (foodMesh == null)
        {
            var meshTr = transform.Find("FoodMesh");
            if (meshTr != null) foodMesh = meshTr;
        }

        if (foodMesh != null)
            _initialMeshScale = foodMesh.localScale;

        EnsureCatAudioSource();

        UpdateVisuals();
    }

    private void EnsureCatAudioSource()
    {
        if (_catAudioSource != null) return;
        if (Cat != null)
            _catAudioSource = Cat.GetComponent<AudioSource>();
    }

    public void PlayEatAudio()
    {
        EnsureCatAudioSource();
        if (eatingClip != null && _catAudioSource != null)
            _catAudioSource.PlayOneShot(eatingClip, eatingSoundVolume);
    }

    public override void Interact(PetStats pet)
    {
        if (_servingsLeft <= 0)
        {
            Refill();
            pet.RaiseFeedback("You refilled the food bowl. ");
            return;
        }

        FindAnyObjectByType<CatNeeds>()?.Eat();
        pet.ModifyStat("hunger", hungerRestore);
        _servingsLeft--;
        pet.RaiseFeedback($"The pet ate! Hunger +{hungerRestore} ({_servingsLeft} servings left)");
        UpdateVisuals();

        PlayEatAudio();
    }

    public void EatCommand()
    {
        if (_servingsLeft <= 0)
        {
            PetStats.Instance?.RaiseFeedback("The food bowl is empty! Quick interact to refill.");
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
        p?.RaiseFeedback("Food bowl refilled.");
    }

    public void Refill()
    {
        _servingsLeft = maxServings;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (foodMesh == null) return;

        float fillPct = (float)_servingsLeft / maxServings;
        foodMesh.gameObject.SetActive(fillPct > 0);
        foodMesh.localScale = new Vector3(
            _initialMeshScale.x,
            _initialMeshScale.y * fillPct,
            _initialMeshScale.z);
    }
}
