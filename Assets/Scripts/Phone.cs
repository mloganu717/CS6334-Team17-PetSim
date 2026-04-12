using UnityEngine;

public class Phone : PetInteractable
{
    public override void Interact(PetStats pet)
    {
        // Standard 'Use' could do anything, but let's default to a generic phone menu message
        pet.RaiseFeedback("Phone: Main Menu opened.");
    }

    public void CallVet(PetStats pet)
    {
        // Fully restores all stats
        pet.ModifyStat("health", 100f);
        pet.ModifyStat("hunger", 100f);
        pet.ModifyStat("thirst", 100f);
        pet.ModifyStat("energy", 100f);
        pet.RaiseFeedback("Vet Service: Your pet has been fully restored and given a 'Clean bill of health'!");
    }

    public void OrderFood(PetStats pet)
    {
        // Good food order
        pet.ModifyStat("hunger", 60f);
        pet.RaiseFeedback("Delivery: Premium pet food served! Hunger +60.");
    }
}
