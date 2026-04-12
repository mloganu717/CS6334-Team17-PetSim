using UnityEngine;

// pet bed
public class PetBed : PetInteractable
{
    [SerializeField] private float energyRestore = 50f;
    [SerializeField] private float cooldown = 10f; // seconds before the bed can be used again

    private float nextUseTime;

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
    }

    public override void PlayerQuickInteract()
    {
        PetStats pet = FindAnyObjectByType<PetStats>();
        if (pet != null) Interact(pet);
    }
}
