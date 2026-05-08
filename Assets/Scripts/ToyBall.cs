using System.Collections;
using UnityEngine;


public class ToyBall : PetInteractable
{
    [SerializeField] private float happinessGain    = 100f;
    [SerializeField] private float energyCost       = 10f;
    [SerializeField] private float minEnergyRequired = 10f;

    [Header("Kick settings")]
    [SerializeField] private float     pushForce = 8f;
    [SerializeField] private AudioClip meowClip;
    [SerializeField] private AudioClip boingClip;

    public bool IsRolling => _rb != null && _rb.linearVelocity.magnitude > 0.1f;

    private Rigidbody       _rb;
    private AudioSource     _catAudioSource;
    private Canvas          _ballMenu;
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
        _rb = GetComponent<Rigidbody>();

        EnsureCatAudioSource();

        var menus = GetComponentsInChildren<Canvas>(true);
        if (menus.Length > 0) _ballMenu = menus[0];
    }

    private void LateUpdate()
    {
        if (_ballMenu != null && _ballMenu.gameObject.activeSelf)
        {
            _ballMenu.transform.position = transform.position + Vector3.up * 0.5f;

            if (Camera.main != null)
                _ballMenu.transform.rotation = Quaternion.LookRotation(
                    _ballMenu.transform.position - Camera.main.transform.position);
        }
    }

    private void EnsureCatAudioSource()
    {
        if (_catAudioSource != null) return;
        if (Cat != null)
            _catAudioSource = Cat.GetComponent<AudioSource>();
    }

    public void PlayInteractAudio()
    {
        EnsureCatAudioSource();
        if (meowClip != null && _catAudioSource != null)
            _catAudioSource.PlayOneShot(meowClip);
        if (boingClip != null)
            AudioSource.PlayClipAtPoint(boingClip, transform.position, 0.85f);
    }

    public void KickBall(Vector3 direction)
    {
        // Hide any open menus
        foreach (var menu in GetComponentsInChildren<Canvas>(true))
            menu.gameObject.SetActive(false);

        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(direction.normalized * pushForce, ForceMode.Impulse);

        EnsureCatAudioSource();
        if (boingClip != null)
            AudioSource.PlayClipAtPoint(boingClip, transform.position, 1f);

        if (meowClip != null && _catAudioSource != null)
            _catAudioSource.PlayOneShot(meowClip);

        StartCoroutine(SendCatToBallAfterDelay());
    }

    private IEnumerator SendCatToBallAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        PetStats pet = PetStats.Instance != null
            ? PetStats.Instance
            : FindAnyObjectByType<PetStats>();

        if (Cat != null && pet != null)
            Cat.GoToAndInteract(transform, this, pet, 1.5f, 3f);
    }

    public override void Interact(PetStats pet)
    {
        if (pet.Energy < minEnergyRequired)
        {
            pet.RaiseFeedback("The pet is too tired to play!");
            return;
        }

        pet.ModifyStat("happiness", happinessGain);
        pet.ModifyStat("energy",   -energyCost);
        pet.RaiseFeedback($"The pet played with the ball! Happiness +{happinessGain}");
        PlayInteractAudio();
    }

    public override void PlayerQuickInteract()
    {
        PetStats pet = FindAnyObjectByType<PetStats>();
        if (pet != null) Interact(pet);
    }
}
