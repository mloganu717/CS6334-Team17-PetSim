using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryMenu;
    public List<GameObject> menuSlots; 
    public List<Texture> thumbnails; 
    public float selectedScale = 1.2f;

    [Header("External References")]
    public PlayerInteractionController interactionController;
    public CharacterMovement characterMovement;
    public raycaster playerRaycaster;
    [SerializeField] private CharacterMovement movementScript;
    private int currentSlot = 0;
    private bool isInteractable = true;
    private float inputCooldown = 0.25f;

    void Start()
    {
        if (inventoryMenu != null)
            inventoryMenu.SetActive(false);
    }

    void OnEnable()
    {
        
        if (inventoryMenu != null && inventoryMenu.activeSelf)
        {
            if (characterMovement != null) characterMovement.enabled = false;
            if (playerRaycaster != null) playerRaycaster.SetRaycastEnabled(false);
        }
    }

    void Update()
    {
        if (inventoryMenu != null && inventoryMenu.activeSelf && isInteractable)
        {
            float v = Input.GetAxis("Vertical");
            if (Mathf.Abs(v) > 0.5f)
            {
                int direction = v > 0 ? -1 : 1; 
                StartCoroutine(CooldownInput());
                SetSlot(currentSlot + direction);
            }

            if (Input.GetButtonDown("js5") || Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return))
            {
                SelectActiveSlot();
            }
        }
    }
    public void SetMovementEnabled(bool enabled)
        {
            if (movementScript != null)
                movementScript.enabled = enabled;
        }
    public void OpenInventory()
    {
        SetMovementEnabled(false);
        if (inventoryMenu == null) return;
        inventoryMenu.SetActive(true);
        isInteractable = true;
        
        if (characterMovement != null) characterMovement.enabled = false;
        if (playerRaycaster != null) playerRaycaster.SetRaycastEnabled(false);
        if (interactionController != null) interactionController.SetMovementEnabled(false);

        UpdateInventoryUI();
        SetSlot(0);
    }

    public void CloseInventory()
    {
        inventoryMenu.SetActive(false);
        if (characterMovement != null) characterMovement.enabled = true;
        if (playerRaycaster != null) playerRaycaster.SetRaycastEnabled(true);
        if (interactionController != null) interactionController.SetMovementEnabled(true);
        SetMovementEnabled(true);
    }
    public bool IsInventoryOpen()
    {
        return inventoryMenu != null && inventoryMenu.activeSelf;
    }
    private void UpdateInventoryUI()
    {
        var items = interactionController.StoredObjects;
        for (int i = 0; i < menuSlots.Count; i++)
        {
            GameObject slot = menuSlots[i];
            Transform spriteTransform = slot.transform.Find("Sprite");
            if (i < items.Count)
            {
                if (spriteTransform != null)
                {
                    spriteTransform.gameObject.SetActive(true);
                    RawImage img = spriteTransform.GetComponent<RawImage>();
                    if (img != null) img.texture = GetThumbnail(items[i].gameObject.name);
                }
            }
            else
            {
                if (spriteTransform != null) spriteTransform.gameObject.SetActive(false);
            }
        }
    }

    private Texture GetThumbnail(string itemName)
    {
        if (thumbnails == null || thumbnails.Count == 0) return null;
        if (itemName.Contains("Food"))  return thumbnails.Count > 0 ? thumbnails[0] : null;
        if (itemName.Contains("Water")) return thumbnails.Count > 1 ? thumbnails[1] : null;
        if (itemName.Contains("Ball"))  return thumbnails.Count > 2 ? thumbnails[2] : null;
        if (itemName.Contains("Bed"))   return thumbnails.Count > 3 ? thumbnails[3] : null;
        if (itemName.Contains("Brush")) return thumbnails.Count > 4 ? thumbnails[4] : null;
        if (itemName.Contains("Clock")) return thumbnails.Count > 5 ? thumbnails[5] : null;
        if (itemName.Contains("Phone")) return thumbnails.Count > 6 ? thumbnails[6] : null;
        return thumbnails[0];
    }

    private void SetSlot(int newIndex)
    {
        if (menuSlots == null || menuSlots.Count == 0) return;
        
        if (newIndex < 0) newIndex = menuSlots.Count - 1;
        else if (newIndex >= menuSlots.Count) newIndex = 0;

        menuSlots[currentSlot].GetComponent<Image>().color = Color.white;
        menuSlots[currentSlot].transform.localScale = Vector3.one;

        currentSlot = newIndex;

        menuSlots[currentSlot].GetComponent<Image>().color = Color.yellow;
        menuSlots[currentSlot].transform.localScale = new Vector3(selectedScale, selectedScale, 1.0f);
    }

    private void SelectActiveSlot()
    {
        var items = interactionController.StoredObjects;
        if (currentSlot < items.Count)
        {
            StorableObject selected = items[currentSlot];
            CloseInventory();
            interactionController.StartPlacement(selected);
        }
    }

    private IEnumerator CooldownInput()
    {
        isInteractable = false;
        yield return new WaitForSeconds(inputCooldown);
        isInteractable = true;
    }
}