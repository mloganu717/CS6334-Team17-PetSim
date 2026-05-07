using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private raycaster playerRaycaster;
    [SerializeField] private CharacterMovement movementScript;
    [SerializeField] private MovementSettings movementSettings;
    [SerializeField] private Transform holdParent;
    [SerializeField] private GameObject inventoryFullMessage;
    [SerializeField] private PhoneUIManager phoneUIManager;

    [Header("Settings Menu UI")]
    [SerializeField] private GameObject settingsMenuCanvas;

    [Header("Input Names")] // button mappings, tested in the bluetoothmapping scene
    [SerializeField] private string teleportButton = "js3"; // y
    [SerializeField] private string openMenuButton = "js2"; // x
    [SerializeField] private string selectButton = "js5"; // b
    [SerializeField] private string actionButton = "js10"; // a
    [SerializeField] private string settingsButton = "Submit"; // settings
    // [SerializeField] private string verticalAxis   = "Vertical"; 
    [SerializeField] private InventoryController inventoryUI;

    [Header("Placement")]
    [SerializeField] private float spawnHeightOffset = 0.5f;
    [SerializeField] private float heldReleaseHeightOffset = 0.5f;
    [SerializeField] private Vector3 heldLocalOffset = new Vector3(0f, -0.2f, 1.2f);

    // object interaction
    private StorableObject currentOpenMenuObject;
    private StorableObject lastDestroyedObject;
    private List<StorableObject> storedObjects = new List<StorableObject>();
    private StorableObject heldObject;
    private Coroutine inventoryMessageCoroutine;

    public List<StorableObject> StoredObjects => storedObjects;

    private void Start()
    {
        if (settingsMenuCanvas != null) settingsMenuCanvas.SetActive(false);
        if (inventoryFullMessage != null) inventoryFullMessage.SetActive(false);

        if (holdParent == null && Camera.main != null)
            holdParent = Camera.main.transform;
    }

    private void Update()
    {
        if (settingsMenuCanvas != null && settingsMenuCanvas.activeSelf)
        {
            return; 
        }

        if (inventoryUI != null && inventoryUI.IsInventoryOpen())
        {
            return; 
        }

        if (phoneUIManager != null && phoneUIManager.IsOpen)
        {
            if (Input.GetButtonDown(selectButton) || Input.GetButtonDown("Fire2"))
                TrySelectCurrentMenuButton();

            return;
        }

        if ((Input.GetButtonDown(settingsButton) || Input.GetButtonDown("js7") || Input.GetButtonDown("js0")) && heldObject == null)
        {
            OpenSettingsMenu();
            return;
        }

        // open inventory
        if (Input.GetButtonDown("js1") || Input.GetKeyDown(KeyCode.I) && heldObject == null)
        {
            if (inventoryUI != null) inventoryUI.OpenInventory();
            return;
        }

        // while holding an object — only allow release + teleport
        if (heldObject != null)
        {
            if (Input.GetButtonDown(actionButton) || Input.GetButtonDown("Fire3"))
                TryReleaseHeldObject();

            if (Input.GetButtonDown(teleportButton) || Input.GetButtonDown("Jump"))
                playerRaycaster.TeleportRigToGround();
            return;
        }
        // extra inputs for various devices
        if (Input.GetButtonDown(teleportButton) || Input.GetButtonDown("Jump"))
            playerRaycaster.TeleportRigToGround();

        if (Input.GetButtonDown(openMenuButton) || Input.GetButtonDown("Fire1")) 
            TryOpenObjectMenu();

        if (Input.GetButtonDown(selectButton) || Input.GetButtonDown("Fire2"))
            TrySelectCurrentMenuButton();

        if (Input.GetButtonDown(actionButton) || Input.GetButtonDown("Fire3"))
            HandlePlayerAction();
    }





    private void OpenSettingsMenu()
    {
        CloseCurrentObjectMenuOnly();

        if (settingsMenuCanvas != null) settingsMenuCanvas.SetActive(true);

        SetMovementEnabled(false);
        playerRaycaster.SetRaycastEnabled(false);
    }







    private void HandlePlayerAction()
    {
        
        if (playerRaycaster.CurrentTarget != null)
        {
            var interactable = playerRaycaster.CurrentTarget.GetComponent<PetInteractable>();
            if (interactable == null)
                interactable = playerRaycaster.CurrentTarget.GetComponentInParent<PetInteractable>();

            if (interactable != null)
            {
                interactable.PlayerQuickInteract();
                return;
            }
        }

        
        TrySpawnLastDestroyedObject();
    }


    private void TryOpenObjectMenu()
    {
        if (playerRaycaster.CurrentTarget == null) return;

        var target = playerRaycaster.CurrentTarget.GetComponent<StorableObject>();
        if (target == null)
            target = playerRaycaster.CurrentTarget.GetComponentInParent<StorableObject>();

        // don't open menu if ball is still rolling
        var ball = target?.GetComponent<ToyBall>();
        if (ball != null && ball.IsRolling) return;

        if (target != null)
            OpenMenuForObject(target);
    }

    private void OpenMenuForObject(StorableObject targetObject)
    {
        if (currentOpenMenuObject != null)
            currentOpenMenuObject.ShowMenu(false);

        currentOpenMenuObject = targetObject;
        currentOpenMenuObject.ShowMenu(true);

        ShowFeedback("Selected " + GetObjectDisplayName(targetObject.gameObject) + ". Choose an option.");
    }

    private void CloseCurrentMenu()
    {
        if (currentOpenMenuObject != null)
        {
            currentOpenMenuObject.ShowMenu(false);
            currentOpenMenuObject = null;
        }
    }

    private void CloseCurrentObjectMenuOnly()
    {
        if (currentOpenMenuObject != null)
        {
            currentOpenMenuObject.ShowMenu(false);
            currentOpenMenuObject = null;
        }
    }

    private void TrySelectCurrentMenuButton()
    {
        if (playerRaycaster.CurrentMenuButton == null)
            return;

        MenuButtonTarget.MenuAction action = playerRaycaster.CurrentMenuButton.Action;

        // These buttons are allowed to work even when no object menu is open.
        // This is needed for the big Phone UI screen.
        switch (action)
        {
            case MenuButtonTarget.MenuAction.CallVet:
                CallVet();
                return;

            case MenuButtonTarget.MenuAction.OrderFood:
                OrderFood();
                return;

            case MenuButtonTarget.MenuAction.PhoneHome:
                if (phoneUIManager != null)
                    phoneUIManager.ShowHomeScreen();
                return;

            case MenuButtonTarget.MenuAction.StartVetCall:
                if (phoneUIManager != null)
                    phoneUIManager.StartVetCall();
                return;

            case MenuButtonTarget.MenuAction.EndVetCall:
                if (phoneUIManager != null)
                    phoneUIManager.EndVetCall();
                return;

            case MenuButtonTarget.MenuAction.StartVoiceRecording:
                if (phoneUIManager != null)
                    phoneUIManager.StartVoiceRecording();
                return;

            case MenuButtonTarget.MenuAction.StopVoiceRecording:
                if (phoneUIManager != null)
                    phoneUIManager.StopVoiceRecording();
                return;

            case MenuButtonTarget.MenuAction.ClosePhone:
                if (phoneUIManager != null)
                    phoneUIManager.ClosePhone();
                return;

            case MenuButtonTarget.MenuAction.VetOption0:
                if (phoneUIManager != null)
                    phoneUIManager.SelectVetOption(0);
                return;

            case MenuButtonTarget.MenuAction.VetOption1:
                if (phoneUIManager != null)
                    phoneUIManager.SelectVetOption(1);
                return;

            case MenuButtonTarget.MenuAction.VetOption2:
                if (phoneUIManager != null)
                    phoneUIManager.SelectVetOption(2);
                return;
        }

        // Everything below this point requires an actual object menu to be open.
        if (currentOpenMenuObject == null)
            return;

        var catMenu = currentOpenMenuObject.GetComponent<CatObjectMenu>();
        if (catMenu == null)
            catMenu = currentOpenMenuObject.GetComponentInParent<CatObjectMenu>();

        switch (action)
        {
            case MenuButtonTarget.MenuAction.Destroy:
                DestroyCurrentObject();
                break;

            case MenuButtonTarget.MenuAction.Store:
                StoreCurrentObject();
                break;

            case MenuButtonTarget.MenuAction.Exit:
                if (catMenu != null)
                    catMenu.CloseMenu();
                else
                    CloseCurrentMenu();
                break;

            case MenuButtonTarget.MenuAction.Use:
                UseCurrentObject();
                break;

            case MenuButtonTarget.MenuAction.Pet:
                if (catMenu != null)
                    catMenu.PetCat();
                break;

            case MenuButtonTarget.MenuAction.ShowStats:
                if (catMenu != null)
                    catMenu.ShowStats();
                break;

            case MenuButtonTarget.MenuAction.Play:
                if (catMenu != null)
                    catMenu.PlayWithCat();
                break;

            case MenuButtonTarget.MenuAction.Drink:
                var bowl = currentOpenMenuObject.GetComponent<WaterBowl>();
                if (bowl != null)
                    bowl.DrinkCommand();
                CloseCurrentMenu();
                break;

            case MenuButtonTarget.MenuAction.Eat:
                var foodBowl = currentOpenMenuObject.GetComponent<FoodBowl>();
                if (foodBowl != null)
                    foodBowl.EatCommand();
                CloseCurrentMenu();
                break;

            case MenuButtonTarget.MenuAction.Sleep:
                var bed = currentOpenMenuObject.GetComponent<PetBed>();
                if (bed != null)
                    bed.SleepCommand();
                CloseCurrentMenu();
                break;
        }
    }

    private void CallVet()
    {
        CloseCurrentObjectMenuOnly();

        if (phoneUIManager != null)
        {
            phoneUIManager.OpenVetCall();
        }
        else
        {
            Debug.LogWarning("PhoneUIManager is not assigned on PlayerInteractionController.");
        }
    }

    private void OrderFood()
    {
        CloseCurrentObjectMenuOnly();

        Debug.Log("Order Food selected. Implement this later.");

        if (phoneUIManager != null)
        {
            phoneUIManager.OpenHome();
        }
    }

    private void UseCurrentObject()
    {
        if (currentOpenMenuObject == null) return;

        var ball = currentOpenMenuObject.GetComponent<ToyBall>();
        if (ball != null)
        {
            ball.KickBall(playerRaycaster.transform.forward);
            CloseCurrentMenu();
            return;
        }

        var interactable = currentOpenMenuObject.GetComponent<PetInteractable>();
        if (interactable == null)
            interactable = currentOpenMenuObject.GetComponentInParent<PetInteractable>();

        if (interactable != null)
        {
            PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
            if (pet != null)
            {
                var cat = FindAnyObjectByType<CatAIController>();
                if (cat != null)
                    cat.GoToAndInteract(currentOpenMenuObject.transform, interactable, pet);
                else
                    interactable.Interact(pet);
            }
        }

        CloseCurrentMenu();
    }

    private void DestroyCurrentObject()
    {
        if (currentOpenMenuObject == null) return;

        StorableObject obj = currentOpenMenuObject;
        string objectName = GetObjectDisplayName(obj.gameObject);

        obj.ShowMenu(false);
        currentOpenMenuObject = null;

        lastDestroyedObject = obj;
        obj.gameObject.SetActive(false);

        ShowFeedback(objectName + " removed. Press A while pointing at the ground to spawn it again.");
    }

    private void StoreCurrentObject()
    {
        if (currentOpenMenuObject == null) return;

        if (storedObjects.Count >= 3)
        {
            ShowInventoryFullMessage();
            ShowFeedback("Inventory is full. You can only store 3 objects.");
            return;
        }

        StorableObject obj = currentOpenMenuObject;
        string objectName = GetObjectDisplayName(obj.gameObject);

        obj.ShowMenu(false);
        currentOpenMenuObject = null;

        storedObjects.Add(obj);
        obj.gameObject.SetActive(false);

        ShowFeedback(objectName + " stored in inventory.");
    }

    private void TryReleaseHeldObject()
    {
        if (heldObject == null || !playerRaycaster.IsHittingGround) return;

        string objectName = GetObjectDisplayName(heldObject.gameObject);

        Vector3 releasePos = playerRaycaster.CurrentGroundPoint + new Vector3(0f, heldReleaseHeightOffset, 0f);
        heldObject.ReleaseFromCamera(releasePos);
        heldObject = null;

        if (playerRaycaster != null)
            playerRaycaster.gameObject.SetActive(true);

        ShowFeedback(objectName + " placed.");
    }

    private void TrySpawnLastDestroyedObject()
    {
        if (!playerRaycaster.IsHittingGround || lastDestroyedObject == null) return;

        string objectName = GetObjectDisplayName(lastDestroyedObject.gameObject);

        lastDestroyedObject.gameObject.SetActive(true);
        Vector3 spawnPos = playerRaycaster.CurrentGroundPoint + new Vector3(0f, spawnHeightOffset, 0f);
        lastDestroyedObject.transform.position = spawnPos;

        Physics.SyncTransforms();
        lastDestroyedObject = null;

        ShowFeedback(objectName + " spawned.");
    }

    public void SetMovementEnabled(bool enabled)
    {
        if (movementScript != null)
            movementScript.enabled = enabled;
    }

    private void ShowInventoryFullMessage()
    {
        if (inventoryMessageCoroutine != null)
            StopCoroutine(inventoryMessageCoroutine);
        inventoryMessageCoroutine = StartCoroutine(ShowInventoryFullMessageRoutine());
    }

    private IEnumerator ShowInventoryFullMessageRoutine()
    {
        if (inventoryFullMessage != null)
            inventoryFullMessage.SetActive(true);

        yield return new WaitForSeconds(2f);

        if (inventoryFullMessage != null)
            inventoryFullMessage.SetActive(false);

        inventoryMessageCoroutine = null;
    }

    public void StartPlacement(StorableObject storable)
    {
        if (storable == null) return;

        if (storedObjects.Contains(storable))
            storedObjects.Remove(storable);

        storable.gameObject.SetActive(true);
        storable.transform.SetParent(holdParent);
        storable.transform.localPosition = heldLocalOffset;
        storable.transform.localRotation = Quaternion.identity;

        heldObject = storable;

        if (playerRaycaster != null)
            playerRaycaster.gameObject.SetActive(false);

        ShowFeedback("Holding " + GetObjectDisplayName(storable.gameObject) + ". Point at the ground and press A to place it.");
    }

    private void ShowFeedback(string message)
    {
        PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

        if (pet != null)
        {
            pet.RaiseFeedback(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    private string GetObjectDisplayName(GameObject obj)
    {
        if (obj == null)
            return "Object";

        return obj.name.Replace("_", " ");
    }
}
