using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    CharacterController charCntrl;

    [Tooltip("The speed at which the character will move.")]
    public float speed = 5f;

    [Tooltip("The camera representing where the character is looking.")]
    public GameObject cameraObj;

    [Tooltip("Should be checked if using the Bluetooth Controller to move. If using keyboard, leave this unchecked.")]
    public bool joyStickMode;

    // Set by menu managers to block input without disabling gravity
    [HideInInspector] public bool movementLocked = false;

    void Start()
    {
        charCntrl = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (movementLocked)
        {
            // Keep gravity applied even when movement is locked
            charCntrl.SimpleMove(Vector3.zero);
            return;
        }

        float horComp = Input.GetAxis("Horizontal");
        float vertComp = Input.GetAxis("Vertical");

        if (joyStickMode)
        {
            horComp = Input.GetAxis("Vertical");
            vertComp = Input.GetAxis("Horizontal") * -1;
        }

        Vector3 moveVect = Vector3.zero;

        Vector3 cameraLook = cameraObj.transform.forward;
        cameraLook.y = 0f;
        cameraLook = cameraLook.normalized;

        Vector3 forwardVect = cameraLook;
        Vector3 rightVect = Vector3.Cross(forwardVect, Vector3.up).normalized * -1;

        moveVect += rightVect * horComp;
        moveVect += forwardVect * vertComp;
        moveVect *= speed;

        charCntrl.SimpleMove(moveVect);
    }
}
