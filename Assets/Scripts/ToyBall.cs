using System.Collections;

using UnityEngine;

//toy ball
//playing with the pet boosts happiness but costs energy
public class ToyBall : PetInteractable
{

    [SerializeField] private float happinessGain = 20f;
    [SerializeField] private float energyCost = 10f;
    [SerializeField] private float minEnergyRequired = 10f;

    [Header("Kick Settings")]
    [SerializeField] private float pushForce = 8f;
    [SerializeField] private AudioClip meowClip;
    [SerializeField] private AudioClip boingClip; // assign in inspector

    public bool IsRolling => rb != null && rb.linearVelocity.magnitude > 0.1f;
    private Rigidbody rb;
    private CatAIController cat;
    private AudioSource catAudioSource;
    private Canvas ballMenu;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        cat = FindAnyObjectByType<CatAIController>();
        if (cat != null)
            catAudioSource = cat.GetComponent<AudioSource>();

        var menus = GetComponentsInChildren<Canvas>(true);
        if (menus.Length > 0) ballMenu = menus[0];
    }

    private void LateUpdate()
    {
        if (ballMenu != null && ballMenu.gameObject.activeSelf)
        {
            // keep menu upright and above the ball regardless of ball rotation
            ballMenu.transform.position = transform.position + Vector3.up * 0.5f;
            
            if (Camera.main != null)
                ballMenu.transform.rotation = Quaternion.LookRotation(
                    ballMenu.transform.position - Camera.main.transform.position);
        }
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cat = FindAnyObjectByType<CatAIController>();
        if (cat != null)
            catAudioSource = cat.GetComponent<AudioSource>();
    }

    public void KickBall(Vector3 direction)
    {
        var menus = GetComponentsInChildren<Canvas>(true);
        foreach (var menu in menus)
            menu.gameObject.SetActive(false);

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direction.normalized * pushForce, ForceMode.Impulse);

        // play boing on the ball itself
       AudioSource.PlayClipAtPoint(boingClip, transform.position, 100f);

        // play meow on the cat
        if (meowClip != null && catAudioSource != null)
            catAudioSource.PlayOneShot(meowClip);

        StartCoroutine(SendCatToBallAfterDelay());
    }

    private IEnumerator SendCatToBallAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
        if (cat != null && pet != null)
            cat.GoToAndInteract(transform, this, pet, 1.5f); // stop 1.5 units away from ball
    }

    public override void Interact(PetStats pet)
    {
        if (pet.Energy < minEnergyRequired)
        {
            pet.RaiseFeedback("The pet is too tired to play!");
            return;
        }

        pet.ModifyStat("happiness", happinessGain);
        pet.ModifyStat("energy", -energyCost);
        pet.RaiseFeedback($"The pet played with the ball! Happiness +{happinessGain}");
    }

    public override void PlayerQuickInteract()
    {
        PetStats pet = FindAnyObjectByType<PetStats>();
        if (pet != null) Interact(pet);
    }
}