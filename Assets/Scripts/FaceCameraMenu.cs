using UnityEngine;

public class FaceCameraMenu : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool onlyRotateOnY = true;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
            return;

        if (onlyRotateOnY)
        {
            Vector3 lookDirection = transform.position - cameraTransform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
        else
        {
            transform.rotation = cameraTransform.rotation;
        }
    }
}