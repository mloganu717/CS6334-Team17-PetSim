using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("Core References")] // again, headers for visual clarity
    [SerializeField] private raycaster playerRaycaster;
    [SerializeField] private MonoBehaviour movementScript;
    [SerializeField] private MovementSettings movementSettings;
    [SerializeField] private Transform holdParent;
    [SerializeField] private GameObject inventoryFullMessage;

    [Header("Settings Menu UI")]
    [SerializeField] private GameObject settingsMenuCanvas;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Image[] settingsButtonImages;
    [SerializeField] private TMP_Text raycastLengthText;
    [SerializeField] private TMP_Text speedText;

    [Header("Inventory UI")]
    [SerializeField] private Image[] inventoryTileBackgrounds;
    [SerializeField] private Image[] inventoryTileThumbnails;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Input Names")]
    [SerializeField] private string teleportButton = "js0";
    [SerializeField] private string openMenuButton = "js1";
    [SerializeField] private string selectButton = "js2";
    [SerializeField] private string actionButton = "js8";
    [SerializeField] private string settingsButton = "Submit";
    [SerializeField] private string verticalAxis = "Vertical";

    [Header("Placement")]
    [SerializeField] private float spawnHeightOffset = 0.5f;
    [SerializeField] private float heldReleaseHeightOffset = 0.5f;
    [SerializeField] private Vector3 heldLocalOffset = new Vector3(0f, -0.2f, 1.2f);

    private StorableObject currentOpenMenuObject;
    private StorableObject lastDestroyedObject;
    private List<StorableObject> storedObjects = new List<StorableObject>();
    private StorableObject heldObject;
    private Coroutine inventoryMessageCoroutine;

    private bool settingsMenuOpen;
    private bool inventoryPanelOpen;
    private bool axisLocked;

    private int settingsIndex;
    private int inventoryIndex;

    private readonly float[] raycastLengths = { 1f, 10f, 50f };
    private int raycastLengthIndex = 1;

    private readonly float[] speedValues = { 20f, 10f, 5f };
    private readonly string[] speedNames = { "High", "Medium", "Low" };
    private int speedIndex = 1;

    public List<StorableObject> StoredObjects => storedObjects;

    private void Start()
    {
        if (settingsMenuCanvas != null) settingsMenuCanvas.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (inventoryFullMessage != null) inventoryFullMessage.SetActive(false);

        // default to main camera if not set in inspector
        if (holdParent == null && Camera.main != null)
            holdParent = Camera.main.transform;

        ApplyRaycastLength();
        ApplySpeed();
        UpdateSettingsVisuals();
        UpdateInventoryVisuals();
    }

    private void Update()
    {
        if (settingsMenuOpen)
        {
            HandleSettingsMenuInput();
            return;
        }

        // open settings (only when not holding something)
        if (Input.GetButtonDown(settingsButton) && heldObject == null)
        {
            OpenSettingsMenu();
            return;
        }

        // while holding an object only allow release + teleport
        if (heldObject != null)
        {
            if (Input.GetButtonDown(actionButton) || Input.GetButtonDown("Fire3"))
                TryReleaseHeldObject();

            if (Input.GetButtonDown(teleportButton) || Input.GetButtonDown("Jump"))
                playerRaycaster.TeleportRigToGround();

            return;
        }

        if ((Input.GetButtonDown(teleportButton) || Input.GetButtonDown("Jump")) && currentOpenMenuObject == null)
            playerRaycaster.TeleportRigToGround();

        if (Input.GetButtonDown(openMenuButton) || Input.GetButtonDown("Fire1"))
            TryOpenObjectMenu();

        if (Input.GetButtonDown(selectButton) || Input.GetButtonDown("Fire2"))
            TrySelectCurrentMenuButton();

        if ((Input.GetButtonDown(actionButton) || Input.GetButtonDown("Fire3")) && currentOpenMenuObject == null)
            TrySpawnLastDestroyedObject();
    }

    private void HandleSettingsMenuInput()
    {
        if (Input.GetButtonDown(settingsButton))
        {
            CloseSettingsAndResume();
            return;
        }

        HandleVerticalNavigation();

        if (Input.GetButtonDown(selectButton) || Input.GetButtonDown("Fire2"))
        {
            if (inventoryPanelOpen)
                TryTakeObjectFromInventory();
            else
                SelectCurrentSettingsItem();
        }
    }

    private void HandleVerticalNavigation()
    {
        float vertical = Input.GetAxisRaw(verticalAxis);

        if (!axisLocked)
        {
            if (vertical > 0.5f)
            {
                MoveSelection(-1); // up decrements
                axisLocked = true;
            }
            else if (vertical < -0.5f)
            {
                MoveSelection(1);
                axisLocked = true;
            }
        }

        if (Mathf.Abs(vertical) < 0.2f)
            axisLocked = false;
    }

    // wraps index in either direction for settings or inventory
    private void MoveSelection(int dir)
    {
        if (inventoryPanelOpen)
        {
            inventoryIndex += dir;
            if (inventoryIndex < 0) inventoryIndex = 2;
            else if (inventoryIndex > 2) inventoryIndex = 0;
            UpdateInventoryVisuals();
        }
        else
        {
            settingsIndex += dir;
            if (settingsIndex < 0) settingsIndex = 4;
            else if (settingsIndex > 4) settingsIndex = 0;
            UpdateSettingsVisuals();
        }
    }

    private void OpenSettingsMenu()
    {
        CloseCurrentObjectMenuOnly();

        settingsMenuOpen = true;
        inventoryPanelOpen = false;
        settingsIndex = 0;
        axisLocked = true;

        if (settingsMenuCanvas != null) settingsMenuCanvas.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        SetMovementEnabled(false);
        playerRaycaster.SetRaycastEnabled(false);

        UpdateSettingsVisuals();
        UpdateInventoryVisuals();
    }

    private void CloseSettingsAndResume()
    {
        settingsMenuOpen = false;
        inventoryPanelOpen = false;
        axisLocked = false;

        if (settingsMenuCanvas != null) settingsMenuCanvas.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        SetMovementEnabled(true);
        playerRaycaster.SetRaycastEnabled(true);
    }

    private void SelectCurrentSettingsItem()
    {
        switch (settingsIndex)
        {
            case 0: CloseSettingsAndResume(); break;
            case 1: CycleRaycastLength(); break;
            case 2: OpenInventoryPanel(); break;
            case 3: CycleSpeed(); break;
            case 4: Application.Quit(); break;
        }
    }

    private void CycleRaycastLength()
    {
        raycastLengthIndex++;
        if (raycastLengthIndex >= raycastLengths.Length)
            raycastLengthIndex = 0;

        ApplyRaycastLength();
        UpdateSettingsVisuals();
    }

    private void ApplyRaycastLength()
    {
        if (playerRaycaster != null)
            playerRaycaster.SetMaxDistance(raycastLengths[raycastLengthIndex]);

        if (raycastLengthText != null)
            raycastLengthText.text = "Raycast Length: " + raycastLengths[raycastLengthIndex].ToString("0") + "m";
    }

    private void CycleSpeed()
    {
        speedIndex++;
        if (speedIndex >= speedValues.Length)
            speedIndex = 0;

        ApplySpeed();
        UpdateSettingsVisuals();
    }

    private void ApplySpeed()
    {
        if (movementSettings != null)
            movementSettings.SetSpeed(speedValues[speedIndex]);

        if (speedText != null)
            speedText.text = "Speed: " + speedNames[speedIndex];
    }

    private void OpenInventoryPanel()
    {
        inventoryPanelOpen = true;
        axisLocked = true;

        inventoryIndex = (storedObjects.Count == 0)
            ? 0
            : Mathf.Clamp(inventoryIndex, 0, storedObjects.Count - 1);

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(true);

        UpdateInventoryVisuals();
    }

    private void TryTakeObjectFromInventory()
    {
        if (storedObjects.Count == 0 || inventoryIndex < 0 || inventoryIndex >= storedObjects.Count)
            return;

        StorableObject obj = storedObjects[inventoryIndex];
        if (obj == null) return;

        storedObjects.RemoveAt(inventoryIndex);
        heldObject = obj;
        heldObject.AttachToCamera(holdParent, heldLocalOffset);

        // keep index in bounds after removal
        if (inventoryIndex >= storedObjects.Count)
            inventoryIndex = Mathf.Max(0, storedObjects.Count - 1);

        CloseSettingsAndResume();
    }

    private void TryReleaseHeldObject()
    {
        if (heldObject == null || !playerRaycaster.IsHittingGround)
            return;

        Vector3 releasePos = playerRaycaster.CurrentGroundPoint + new Vector3(0f, heldReleaseHeightOffset, 0f);
        heldObject.ReleaseFromCamera(releasePos);
        heldObject = null;
    }

    private void TryOpenObjectMenu()
    {
        if (playerRaycaster.CurrentTarget == null) return;

        // try the object itself, then check the parent
        var target = playerRaycaster.CurrentTarget.GetComponent<StorableObject>();
        if (target == null)
            target = playerRaycaster.CurrentTarget.GetComponentInParent<StorableObject>();

        if (target != null)
            OpenMenuForObject(target);
    }

    private void OpenMenuForObject(StorableObject targetObject)
    {
        if (currentOpenMenuObject != null)
            currentOpenMenuObject.ShowMenu(false);

        currentOpenMenuObject = targetObject;
        currentOpenMenuObject.ShowMenu(true);
        SetMovementEnabled(false);
    }

    private void CloseCurrentMenu()
    {
        if (currentOpenMenuObject != null)
        {
            currentOpenMenuObject.ShowMenu(false);
            currentOpenMenuObject = null;
        }
        SetMovementEnabled(true);
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
        if (currentOpenMenuObject == null || playerRaycaster.CurrentMenuButton == null)
            return;

        switch (playerRaycaster.CurrentMenuButton.Action)
        {
            case MenuButtonTarget.MenuAction.Destroy:
                DestroyCurrentObject();
                break;
            case MenuButtonTarget.MenuAction.Store:
                StoreCurrentObject();
                break;
            case MenuButtonTarget.MenuAction.Exit:
                CloseCurrentMenu();
                break;
        }
    }

    private void DestroyCurrentObject()
    {
        if (currentOpenMenuObject == null) return;

        StorableObject obj = currentOpenMenuObject;
        obj.ShowMenu(false);
        currentOpenMenuObject = null;
        SetMovementEnabled(true);

        lastDestroyedObject = obj;
        obj.gameObject.SetActive(false);
        //Debug.Log("destroyed " + obj.name);
    }

    private void StoreCurrentObject()
    {
        if (currentOpenMenuObject == null) return;

        // TODO: maybe make this configurable instead of hardcoded 3
        if (storedObjects.Count >= 3)
        {
            ShowInventoryFullMessage();
            return;
        }

        StorableObject obj = currentOpenMenuObject;
        obj.ShowMenu(false);
        currentOpenMenuObject = null;
        SetMovementEnabled(true);

        storedObjects.Add(obj);
        obj.gameObject.SetActive(false);
    }

    private void TrySpawnLastDestroyedObject()
    {
        if (!playerRaycaster.IsHittingGround || lastDestroyedObject == null)
            return;

        lastDestroyedObject.gameObject.SetActive(true);
        Vector3 spawnPos = playerRaycaster.CurrentGroundPoint + new Vector3(0f, spawnHeightOffset, 0f);
        lastDestroyedObject.transform.position = spawnPos;

        Physics.SyncTransforms();
        lastDestroyedObject = null;
    }

    private void SetMovementEnabled(bool enabled)
    {
        if (movementScript != null)
            movementScript.enabled = enabled;
    }

    private void UpdateSettingsVisuals()
    {
        for (int i = 0; i < settingsButtonImages.Length; i++)
        {
            if (settingsButtonImages[i] != null)
                settingsButtonImages[i].color = (i == settingsIndex) ? selectedColor : normalColor;
        }
    }

    private void UpdateInventoryVisuals()
    {
        for (int i = 0; i < inventoryTileBackgrounds.Length; i++)
        {
            bool hasObj = i < storedObjects.Count && storedObjects[i] != null;

            if (inventoryTileBackgrounds[i] != null)
            {
                inventoryTileBackgrounds[i].gameObject.SetActive(hasObj);

                if (hasObj && i == inventoryIndex && inventoryPanelOpen)
                    inventoryTileBackgrounds[i].color = selectedColor;
                else
                    inventoryTileBackgrounds[i].color = normalColor;
            }

            if (inventoryTileThumbnails[i] != null)
            {
                if (hasObj && storedObjects[i].Thumbnail != null)
                {
                    inventoryTileThumbnails[i].sprite = storedObjects[i].Thumbnail;
                    inventoryTileThumbnails[i].enabled = true;
                }
                else
                {
                    inventoryTileThumbnails[i].sprite = null;
                    inventoryTileThumbnails[i].enabled = false;
                }
            }
        }
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
}
