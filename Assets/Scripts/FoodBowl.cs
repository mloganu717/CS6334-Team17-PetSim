using UnityEngine;

//food bowl
//restores the pet's hunger,has limited servings before needing a refill.
public class FoodBowl : PetInteractable
{
    [SerializeField] private float hungerRestore = 30f;
    [SerializeField] private int maxServings = 3;
    [SerializeField] private Transform foodMesh; // visual indicator

    private int servingsLeft;
    private Vector3 initialMeshScale;

    private void Awake()
    {
        servingsLeft = maxServings;
        if (foodMesh == null)
        {
            var meshTr = transform.Find("FoodMesh");
            if (meshTr != null) foodMesh = meshTr;
        }

        if (foodMesh != null)
            initialMeshScale = foodMesh.localScale;
            
        UpdateVisuals();
    }

    public override void Interact(PetStats pet)
    {
        if (servingsLeft <= 0)
        {
            // if bowl is empty, player refills it
            Refill();
            pet.RaiseFeedback("You refilled the food bowl!");
            return;
        }

        pet.ModifyStat("hunger", hungerRestore);
        servingsLeft--;
        pet.RaiseFeedback($"You fed the pet! Hunger +{hungerRestore} ({servingsLeft} servings left)");
        UpdateVisuals();
    }

    public override void PlayerQuickInteract()
    {
        Refill();
    }

    public void Refill()
    {
        servingsLeft = maxServings;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (foodMesh != null)
        {
            float fillPct = (float)servingsLeft / maxServings;
            foodMesh.gameObject.SetActive(fillPct > 0);
            
            // scale height of the food down as it goes empty
            foodMesh.localScale = new Vector3(
                initialMeshScale.x, 
                initialMeshScale.y * fillPct, 
                initialMeshScale.z
            );
        }
    }
}
