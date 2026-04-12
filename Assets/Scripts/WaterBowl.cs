using UnityEngine;

// water bowl
// restores the pet's thirst
public class WaterBowl : PetInteractable
{
    [SerializeField] private float thirstRestore = 40f;
    [SerializeField] private float cooldown = 5f; // seconds before the bowl can be used again
    [SerializeField] private Transform waterMesh; // visual indicator

    private float nextUseTime;

    private void Awake()
    {
        if (waterMesh == null)
        {
            var meshTr = transform.Find("WaterMesh");
            if (meshTr != null) waterMesh = meshTr;
        }
    }

    private void Update()
    {
        if (waterMesh != null)
        {
            // show water only when cooldown is finished
            bool isFilled = Time.time >= nextUseTime;
            if (waterMesh.gameObject.activeSelf != isFilled)
                waterMesh.gameObject.SetActive(isFilled);
        }
    }

    public override void Interact(PetStats pet)
    {
        if (Time.time < nextUseTime)
        {
            pet.RaiseFeedback("The water bowl is empty. Let it refill!");
            return;
        }

        pet.ModifyStat("thirst", thirstRestore);
        nextUseTime = Time.time + cooldown;
        pet.RaiseFeedback($"The pet drank the water! Thirst +{thirstRestore}. Refilling in {cooldown}s...");
    }

    public override void PlayerQuickInteract()
    {
        Refill();
    }

    public void Refill()
    {
        nextUseTime = Time.time;
        if (waterMesh != null) waterMesh.gameObject.SetActive(true);
    }
}
