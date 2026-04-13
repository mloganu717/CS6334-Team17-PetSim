using System.Collections;
using UnityEngine;

public class Phone : PetInteractable
{
    [Header("Audio")]
    [SerializeField] private AudioClip dialingClip;   // plays first
    [SerializeField] private AudioClip ringingClip;   // plays second
    [SerializeField] private AudioClip busyClip;      // plays third
    [SerializeField] private float volume = 3f;

    private AudioSource audioSource;

    private void Start()
    {
        // add audiosource to the phone itself
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 1f; // 3d sound
        audioSource.playOnAwake = false;
    }

    public override void Interact(PetStats pet)
    {
        pet.RaiseFeedback("Phone: Main Menu opened.");
    }

    public void CallVet(PetStats pet)
    {
        // fully restores all stats
        pet.ModifyStat("hunger", 100f);
        pet.ModifyStat("thirst", 100f);
        pet.ModifyStat("energy", 100f);
        pet.RaiseFeedback("Vet Service: Your pet has been fully restored!");

        // play dialing -> ringing -> busy sequence
        StartCoroutine(PlayCallSequence());
    }

    public void OrderFood(PetStats pet)
    {
        // good food order
        pet.ModifyStat("hunger", 60f);
        pet.RaiseFeedback("Delivery: Premium pet food served! Hunger +60.");
    }

    private IEnumerator PlayCallSequence()
    {
        // play dialing first
        if (dialingClip != null)
        {
            audioSource.PlayOneShot(dialingClip, volume);
            yield return new WaitForSeconds(dialingClip.length);
        }

        // then ringing
        if (ringingClip != null)
        {
            audioSource.PlayOneShot(ringingClip, volume);
            yield return new WaitForSeconds(ringingClip.length);
        }

        // then busy signal
        if (busyClip != null)
        {
            audioSource.PlayOneShot(busyClip, volume);
        }
    }

    public override void PlayerQuickInteract()
    {
        PetStats pet = FindAnyObjectByType<PetStats>();
        if (pet != null) Interact(pet);
    }
}