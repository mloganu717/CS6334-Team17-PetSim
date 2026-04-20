// InteractableObject.cs 

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class InteractableObject : MonoBehaviour
{
    [Header(" Physics Settings ")]
    public float linearDrag = 1f;
    public float angularDrag = 1f;
    [Tooltip("Freeze rotation so objects don't tumble from collisions.")]
    public bool freezeRotation = true;

    //  Private state 

    [HideInInspector] public bool IsHovered = false;

    //  Unity 

    void Awake()
    {
        ConfigureRigidbody();
        ConfigureLayer();
    }

    //  Called by RaycastManager via SendMessage 

    void OnPointerEnter()
    {
        IsHovered = true;
    }

    void OnPointerExit()
    {
        IsHovered = false;
    }

    //  Helpers 

    void ConfigureRigidbody()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearDamping = linearDrag;
        rb.angularDamping = angularDrag;

        if (freezeRotation)
            rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void ConfigureLayer()
    {
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer != -1)
            gameObject.layer = layer;
        else
            Debug.LogWarning($"[InteractableObject] Layer 'Interactable' not found on '{name}'. " +
                              "Create it in Edit > Project Settings > Tags and Layers.");
    }
}