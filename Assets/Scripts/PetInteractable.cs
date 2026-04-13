using UnityEngine;


public abstract class PetInteractable : MonoBehaviour
{
    public abstract void Interact(PetStats pet);
    public virtual void PlayerQuickInteract() { } // default is empty
}
