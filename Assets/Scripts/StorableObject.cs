using UnityEngine;

public class StorableObject : MonoBehaviour
{
    [SerializeField] private GameObject objectMenuCanvas;
    [SerializeField] private Sprite thumbnail; 

    private Transform originalParent;
    private Collider[] childColliders;
    private Rigidbody[] childRigidbodies;

    public GameObject ObjectMenuCanvas => objectMenuCanvas;
    public Sprite Thumbnail => thumbnail;

    private void Awake()
    {
        originalParent = transform.parent;
        childColliders = GetComponentsInChildren<Collider>(true);
        childRigidbodies = GetComponentsInChildren<Rigidbody>(true);
        ShowMenu(false);
    }

    private void Update()
    {
        if (objectMenuCanvas != null && objectMenuCanvas.activeInHierarchy && Camera.main != null)
        {
            
            objectMenuCanvas.transform.rotation = Quaternion.LookRotation(objectMenuCanvas.transform.position - Camera.main.transform.position);
            
            
            Vector3 euler = objectMenuCanvas.transform.eulerAngles;
            euler.x = 0f; 
            euler.z = 0f; 
            objectMenuCanvas.transform.eulerAngles = euler;
        }
    }

    public void ShowMenu(bool show)
    {
        if (objectMenuCanvas != null)
            objectMenuCanvas.SetActive(show);
    }

    public void AttachToCamera(Transform holdParent, Vector3 localOffset)
    {
        ShowMenu(false);
        gameObject.SetActive(true);

        foreach (var col in childColliders)
            col.enabled = false;

        foreach (var rb in childRigidbodies)
            rb.isKinematic = true;

        transform.SetParent(holdParent);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
    }

    public void ReleaseFromCamera(Vector3 worldPosition)
    {
        transform.SetParent(originalParent);
        transform.position = worldPosition;

        foreach (var col in childColliders)
            col.enabled = true;

        foreach (var rb in childRigidbodies)
            rb.isKinematic = false;

        Physics.SyncTransforms();
    }
}
