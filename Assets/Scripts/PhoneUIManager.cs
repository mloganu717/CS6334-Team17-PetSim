using TMPro;
using UnityEngine;

public class PhoneUIManager : MonoBehaviour
{
    [Header("Main Canvas")]
    [SerializeField] private GameObject phoneCanvas;

    [Header("Screens")]
    [SerializeField] private GameObject homeScreen;
    [SerializeField] private GameObject vetCallScreen;

    [Header("Vet Call")]
    [SerializeField] private VetCallManager vetCallManager;

    [Header("Placement")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float distanceFromCamera = 1.5f;
    [SerializeField] private float verticalOffset = -0.05f;
    [SerializeField] private bool keepFacingPlayer = true;

    public bool IsOpen => phoneCanvas != null && phoneCanvas.activeSelf;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (phoneCanvas != null)
        {
            phoneCanvas.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (!IsOpen)
            return;

        if (keepFacingPlayer)
        {
            FacePlayer();
        }
    }

    public void OpenHome()
    {
        if (phoneCanvas == null)
            return;

        PlaceInFrontOfPlayer();

        phoneCanvas.SetActive(true);

        if (homeScreen != null)
        {
            homeScreen.SetActive(true);
        }

        if (vetCallScreen != null)
        {
            vetCallScreen.SetActive(false);
        }

        RefreshButtonColliders();
    }

    public void OpenVetCall()
    {
        if (phoneCanvas == null)
            return;

        PlaceInFrontOfPlayer();

        phoneCanvas.SetActive(true);

        if (homeScreen != null)
        {
            homeScreen.SetActive(false);
        }

        if (vetCallScreen != null)
        {
            vetCallScreen.SetActive(true);
        }

        if (vetCallManager != null)
        {
            vetCallManager.ResetCallScreen();
        }

        RefreshButtonColliders();
    }

    public void ShowHomeScreen()
    {
        if (phoneCanvas != null)
        {
            phoneCanvas.SetActive(true);
        }

        if (homeScreen != null)
        {
            homeScreen.SetActive(true);
        }

        if (vetCallScreen != null)
        {
            vetCallScreen.SetActive(false);
        }

        RefreshButtonColliders();
    }

    public void StartVetCall()
    {
        if (vetCallManager != null)
        {
            vetCallManager.StartDemoVetCall();
        }
        else
        {
            Debug.LogWarning("VetCallManager is not assigned on PhoneUIManager.");
        }
    }

    public void EndVetCall()
    {
        if (vetCallManager != null)
        {
            vetCallManager.EndVetCall();
        }
    }

    public void ClosePhone()
    {
        if (vetCallManager != null)
        {
            vetCallManager.EndVetCall();
        }

        if (phoneCanvas != null)
        {
            phoneCanvas.SetActive(false);
        }
    }

    private void PlaceInFrontOfPlayer()
    {
        if (cameraTransform == null || phoneCanvas == null)
            return;

        Vector3 position =
            cameraTransform.position
            + cameraTransform.forward * distanceFromCamera
            + cameraTransform.up * verticalOffset;

        phoneCanvas.transform.position = position;
        FacePlayer();
    }

    private void FacePlayer()
    {
        if (cameraTransform == null || phoneCanvas == null)
            return;

        Vector3 direction = phoneCanvas.transform.position - cameraTransform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            phoneCanvas.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    private void RefreshButtonColliders()
    {
        Canvas.ForceUpdateCanvases();

        if (phoneCanvas == null)
            return;

        MenuButtonTarget[] buttons = phoneCanvas.GetComponentsInChildren<MenuButtonTarget>(true);

        foreach (MenuButtonTarget button in buttons)
        {
            button.UpdateBoxCollider();
        }
    }

    public void SelectVetOption(int optionIndex)
    {
        if (vetCallManager != null)
        {
            vetCallManager.SelectOption(optionIndex);
        }
    }

    public void StartVoiceRecording()
    {
        if (vetCallManager != null)
        {
            vetCallManager.StartVoiceRecording();
        }
    }

    public void StopVoiceRecording()
    {
        if (vetCallManager != null)
        {
            vetCallManager.StopVoiceRecording();
        }
    }
}