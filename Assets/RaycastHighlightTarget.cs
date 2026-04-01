using UnityEngine;

public class RaycastHighlightTarget : MonoBehaviour
{
    [SerializeField] private Outline outline;

    void Start()
    {
        if (outline == null) // null checks so the editor stops throwing errors
            outline = GetComponent<Outline>();

        if (outline != null)
            outline.enabled = false;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outline != null)
            outline.enabled = highlighted;
    }

    private void OnDisable()
    {
        if (outline != null)
            outline.enabled = false;
    }
}
