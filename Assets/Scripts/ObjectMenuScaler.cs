using UnityEngine;


//might use this later
public class ObjectMenuScaler : MonoBehaviour
{
    [SerializeField] private float scaleMultiplier = 0.1f;
    [SerializeField] private Transform target; 

    void Update()
    {
        Transform cam = target != null ? target : Camera.main?.transform;
        if (cam == null) return;

        float dist = Vector3.Distance(cam.position, transform.position);
        float scale = dist * scaleMultiplier;
        transform.localScale = Vector3.one * scale;
    }
}