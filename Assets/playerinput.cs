using UnityEngine;

public class playerinput : MonoBehaviour
{
    [SerializeField] private raycaster raycaster;

    private void Update()
    {
        // teleport on either joystick button or keyboard,
        if (Input.GetButtonDown("js0") || Input.GetButtonDown("Jump"))
            raycaster.TeleportRigToGround();
    }
}
