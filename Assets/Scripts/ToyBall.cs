using UnityEngine;

//toy ball
//playing with the pet boosts happiness but costs energy
public class ToyBall : PetInteractable
{
    [SerializeField] private float happinessGain = 20f;
    [SerializeField] private float energyCost = 10f;
    [SerializeField] private float minEnergyRequired = 10f;

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
