using UnityEngine;

/// <summary>
/// Food bowl — restores the pet's hunger, limited servings before needing a refill.
///
/// CHANGES FROM ORIGINAL:
///  - Cat reference is resolved lazily once and cached (not found every EatCommand call).
///  - EatCommand() uses GoToAndInteract() which now exists on CatAIController.
///  - Removed duplicate Awake/Start logic; everything initialises in Awake.
/// </summary>
public class FoodBowl : PetInteractable
{
    [SerializeField] private float hungerRestore = 30f;
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
        _servingsLeft = maxServings;

        if (foodMesh == null)
        {
            var meshTr = transform.Find("FoodMesh");
            if (meshTr != null) foodMesh = meshTr;
        }

        if (foodMesh != null)
            _initialMeshScale = foodMesh.localScale;

        if (Cat != null)
            _catAudioSource = Cat.GetComponent<AudioSource>();

        UpdateVisuals();
    }

    public override void Interact(PetStats pet)
    {
        if (_servingsLeft <= 0)
        {
            Refill();
            pet.RaiseFeedback("You refilled the food bowl!");
            return;
        }

        pet.ModifyStat("hunger", hungerRestore);
        _servingsLeft--;
        pet.RaiseFeedback($"The pet ate! Hunger +{hungerRestore} ({_servingsLeft} servings left)");
        UpdateVisuals();

        if (eatingClip != null && _catAudioSource != null)
            _catAudioSource.PlayOneShot(eatingClip, eatingSoundVolume);
    }

    /// <summary>Called from the object menu Eat button — sends cat to bowl.</summary>
    public void EatCommand()
    {
        if (Cat == null) return;

        PetStats pet = PetStats.Instance != null
            ? PetStats.Instance
            : FindAnyObjectByType<PetStats>();

        if (pet != null)
            Cat.GoToAndInteract(transform, this, pet, catStopDistance, 4f);
    }

    public override void PlayerQuickInteract() => Refill();

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
