using UnityEngine;

//grooming brush
//grooming the pet boosts both hygiene and happiness
public class GroomingBrush : PetInteractable
{
    [SerializeField] private float hygieneGain = 25f;
    [SerializeField] private float happinessGain = 10f;
    [SerializeField] private float cooldown = 8f; //seconds between grooming sessions

    private float nextUseTime;

    public override void Interact(PetStats pet)
    {
        if (Time.time < nextUseTime)
        {
            pet.RaiseFeedback("The pet doesn't need brushing again yet.");
            return;
        }

        pet.ModifyStat("hygiene", hygieneGain);
        pet.ModifyStat("happiness", happinessGain);
        nextUseTime = Time.time + cooldown;
        pet.RaiseFeedback($"You groomed the pet! Hygiene +{hygieneGain}, Happiness +{happinessGain}");
    }

    public override void PlayerQuickInteract()
    {
        PetStats pet = FindAnyObjectByType<PetStats>();
        if (pet != null) Interact(pet);
    }
}
