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
        if (currentOpenMenuObject == null || playerRaycaster.CurrentMenuButton == null) return;
        var catMenu = currentOpenMenuObject.GetComponent<CatObjectMenu>();
        if (catMenu == null)
            catMenu = currentOpenMenuObject.GetComponentInParent<CatObjectMenu>();

        switch (playerRaycaster.CurrentMenuButton.Action)
        {
            case MenuButtonTarget.MenuAction.Destroy: DestroyCurrentObject(); break;
            case MenuButtonTarget.MenuAction.Store:   StoreCurrentObject();   break;
            case MenuButtonTarget.MenuAction.Exit:    if (catMenu != null) catMenu.CloseMenu(); else CloseCurrentMenu(); break;
            case MenuButtonTarget.MenuAction.Use:     UseCurrentObject();     break;
            case MenuButtonTarget.MenuAction.CallVet: CallVet();            break;
            case MenuButtonTarget.MenuAction.OrderFood: OrderFood();        break;
            case MenuButtonTarget.MenuAction.Pet:      if (catMenu != null) catMenu.PetCat();      break;
            case MenuButtonTarget.MenuAction.ShowStats: if (catMenu != null) catMenu.ShowStats();  break;
            case MenuButtonTarget.MenuAction.Play:     if (catMenu != null) catMenu.PlayWithCat(); break;
            case MenuButtonTarget.MenuAction.Drink:
                var bowl = currentOpenMenuObject.GetComponent<WaterBowl>();
                if (bowl != null) bowl.DrinkCommand();
                CloseCurrentMenu();
                break;
            case MenuButtonTarget.MenuAction.Eat:
                var foodBowl = currentOpenMenuObject.GetComponent<FoodBowl>();
                if (foodBowl != null) foodBowl.EatCommand();
                CloseCurrentMenu();
                break;
            case MenuButtonTarget.MenuAction.Sleep:
                var bed = currentOpenMenuObject.GetComponent<PetBed>();
                if (bed != null) bed.SleepCommand();
                CloseCurrentMenu();
                break;
        }
    }

    private void CallVet()
    {
        if (currentOpenMenuObject == null) return;
        var phone = currentOpenMenuObject.GetComponent<Phone>();
        if (phone != null)
        {
            PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
            if (pet != null) phone.CallVet(pet);
        }
        CloseCurrentMenu();
    }

    private void OrderFood()
    {
        if (currentOpenMenuObject == null) return;
        var phone = currentOpenMenuObject.GetComponent<Phone>();
        if (phone != null)
        {
            PetStats pet = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
            if (pet != null) phone.OrderFood(pet);
        }
        CloseCurrentMenu();
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
        obj.ShowMenu(false);
        currentOpenMenuObject = null;

        lastDestroyedObject = obj;
        obj.gameObject.SetActive(false);
    }

    private void StoreCurrentObject()
    {
        if (currentOpenMenuObject == null) return;

        if (storedObjects.Count >= 3)
        {
            ShowInventoryFullMessage();
            return;
        }

        StorableObject obj = currentOpenMenuObject;
        obj.ShowMenu(false);
        currentOpenMenuObject = null;

        storedObjects.Add(obj);
        obj.gameObject.SetActive(false);
    }

    private void TryReleaseHeldObject()
    {
        if (heldObject == null || !playerRaycaster.IsHittingGround) return;

        Vector3 releasePos = playerRaycaster.CurrentGroundPoint + new Vector3(0f, heldReleaseHeightOffset, 0f);
        heldObject.ReleaseFromCamera(releasePos);
        heldObject = null;

        // Re-enable the laser pointer after releasing the object
        if (playerRaycaster != null)
            playerRaycaster.gameObject.SetActive(true);
    }

    private void TrySpawnLastDestroyedObject()
    {
        if (!playerRaycaster.IsHittingGround || lastDestroyedObject == null) return;

        lastDestroyedObject.gameObject.SetActive(true);
        Vector3 spawnPos = playerRaycaster.CurrentGroundPoint + new Vector3(0f, spawnHeightOffset, 0f);
        lastDestroyedObject.transform.position = spawnPos;

        Physics.SyncTransforms();
        lastDestroyedObject = null;
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
    }
}
