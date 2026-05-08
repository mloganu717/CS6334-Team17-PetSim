using UnityEngine;

public class GroomingBrush : PetInteractable
{
    [SerializeField] private float hygieneGain = 100f;
    [SerializeField] private float happinessGain = 100f;
    [SerializeField] private float cooldown = 8f;

    private float _nextUseTime;

    public override void Interact(PetStats pet)
    {
        if (Time.time < _nextUseTime)
        {
            pet.RaiseFeedback("The pet doesn't need brushing again yet.");
            return;
        }

        pet.ModifyStat("hygiene", hygieneGain);
        pet.ModifyStat("happiness", happinessGain);
        _nextUseTime = Time.time + cooldown;
        pet.RaiseFeedback($"You groomed the pet! Hygiene +{hygieneGain}, Happiness +{happinessGain}");

        var mood = FindAnyObjectByType<CatMood>();
        mood?.OnGroomed();
    }

    public override void PlayerQuickInteract()
    {
        PetStats pet = FindAnyObjectByType<PetStats>();
        if (pet != null) Interact(pet);
    }
}
