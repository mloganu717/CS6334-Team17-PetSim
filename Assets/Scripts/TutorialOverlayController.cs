using UnityEngine;

public class TutorialOverlayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject tutorialCanvas;
    [SerializeField] private raycaster playerRaycaster;
    [SerializeField] private CharacterMovement movementScript;

    [Header("Input")]
    [SerializeField] private string okButton = "Submit";
    [SerializeField] private string alternateOkButton = "js7";

    [Header("Startup Protection")]
    [SerializeField] private float minimumShowTime = 0.75f;

    private bool tutorialOpen;
    private float canCloseAfterTime;
    private bool buttonsReleasedAfterOpening;

    private void Start()
    {
        ShowTutorial();
    }

    private void Update()
    {
        if (!tutorialOpen)
            return;

        if (Time.time < canCloseAfterTime)
            return;

        bool okHeld =
            Input.GetButton(okButton) ||
            Input.GetButton(alternateOkButton);

        if (!buttonsReleasedAfterOpening)
        {
            if (!okHeld)
            {
                buttonsReleasedAfterOpening = true;
            }

            return;
        }

        if (Input.GetButtonDown(okButton) || Input.GetButtonDown(alternateOkButton))
        {
            HideTutorial();
        }
    }

    private void ShowTutorial()
    {
        tutorialOpen = true;
        buttonsReleasedAfterOpening = false;
        canCloseAfterTime = Time.time + minimumShowTime;

        if (tutorialCanvas != null)
            tutorialCanvas.SetActive(true);

        if (movementScript != null)
            movementScript.enabled = false;

        if (playerRaycaster != null)
            playerRaycaster.SetRaycastEnabled(false);
    }

    private void HideTutorial()
    {
        tutorialOpen = false;

        if (tutorialCanvas != null)
            tutorialCanvas.SetActive(false);

        if (movementScript != null)
            movementScript.enabled = true;

        if (playerRaycaster != null)
            playerRaycaster.SetRaycastEnabled(true);

        PetStats.Instance?.RaiseFeedback("Tutorial closed. Look at an object and press X to interact.");
    }
}