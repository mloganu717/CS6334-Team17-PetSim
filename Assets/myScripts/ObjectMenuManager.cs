// ObjectMenuManager.cs 
//


using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectMenuManager : MonoBehaviour
{
    // ── Inspector 

    [Header("Menu Canvas (World Space) ")]
    public Canvas menuCanvas;
    public RectTransform destroyButton;
    public RectTransform storeButton;
    public RectTransform exitButton;

    [Tooltip("How far in front of the camera the menu sits.")]
    public float menuDistance = 1.5f;

    [Header("Highlight Colors ")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    [Header("References")]
    public InventoryManager inventoryManager;
    public RaycastManager raycastManager;
    public CharacterMovement playerMovement;

    [Header("Inventory Full Message")]
    public GameObject fullMessageObject;
    public float fullMessageDuration = 2f;

    // ── Public state 

    [HideInInspector] public bool IsMenuOpen = false;

    // ── Private 

    private GameObject targetObject;
    private RectTransform hoveredButton;

    private bool IsBPressed => Input.GetButtonDown("js1") || Input.GetKeyDown(KeyCode.B);

    // ── Unity 

    void Start()
    {
        menuCanvas.gameObject.SetActive(false);
        if (fullMessageObject != null) fullMessageObject.SetActive(false);

        // Ensure each button has a BoxCollider so the physics raycast can hit it
        EnsureButtonCollider(destroyButton);
        EnsureButtonCollider(storeButton);
        EnsureButtonCollider(exitButton);
    }

    void Update()
    {
        if (!IsMenuOpen) return;

        HandleButtonHover();
        HandleButtonConfirm();
    }

    //  Collider setup 

    void EnsureButtonCollider(RectTransform btn)
    {
        if (btn == null) return;
        if (btn.GetComponent<Collider>() != null) return; 

        BoxCollider bc = btn.gameObject.AddComponent<BoxCollider>();
        bc.size = new Vector3(btn.rect.width, btn.rect.height, 1f);
        bc.center = Vector3.zero;
    }

    //  Open / Close 

    public void OpenMenu(GameObject obj)
    {
        if (IsMenuOpen) CloseMenu(false);

        targetObject = obj;
        IsMenuOpen = true;


        menuCanvas.transform.SetParent(null);
        Transform cam = Camera.main.transform;
        menuCanvas.transform.position = cam.position + cam.forward * menuDistance;
        menuCanvas.transform.rotation = Quaternion.LookRotation(
            menuCanvas.transform.position - cam.position);

        menuCanvas.gameObject.SetActive(true);

        // Lock movement 
        if (playerMovement != null) playerMovement.movementLocked = true;

        // Keep raycast enabled 
        if (raycastManager != null) raycastManager.IsDisabled = false;

        ResetHighlights();
    }

    public void CloseMenu(bool restoreMovement = true)
    {
        IsMenuOpen = false;
        targetObject = null;
        hoveredButton = null;
        menuCanvas.gameObject.SetActive(false);

        if (restoreMovement && playerMovement != null)
            playerMovement.movementLocked = false;
    }

    //  Button hover via physics raycast 


    void HandleButtonHover()
    {
        RectTransform newHover = null;

        GameObject hit = raycastManager != null ? raycastManager.CurrentTarget : null;

        if (hit != null)
        {
            // Check if hit object is a button or child of a button
            if (IsOrChildOf(hit, destroyButton)) newHover = destroyButton;
            else if (IsOrChildOf(hit, storeButton)) newHover = storeButton;
            else if (IsOrChildOf(hit, exitButton)) newHover = exitButton;
        }

        if (newHover == hoveredButton) return;

        if (hoveredButton != null) SetButtonColor(hoveredButton, normalColor);
        hoveredButton = newHover;
        if (hoveredButton != null) SetButtonColor(hoveredButton, highlightColor);
    }

    bool IsOrChildOf(GameObject go, RectTransform rt)
        => go == rt.gameObject || go.transform.IsChildOf(rt);

    //  Confirm with B 

    void HandleButtonConfirm()
    {
        if (!IsBPressed || hoveredButton == null) return;

        if (hoveredButton == destroyButton) ExecuteDestroy();
        else if (hoveredButton == storeButton) ExecuteStore();
        else if (hoveredButton == exitButton) ExecuteExit();
    }

    //  Actions 

    void ExecuteDestroy()
    {
        if (targetObject == null) return;
        if (raycastManager != null) raycastManager.RegisterDestroyedObject(targetObject);
        Destroy(targetObject);
        CloseMenu();
        Debug.Log("[ObjectMenu] Destroyed.");
    }

    void ExecuteStore()
    {
        if (targetObject == null) return;

        if (inventoryManager != null && inventoryManager.IsFull)
        {
            StartCoroutine(ShowFullMessage());
            return;
        }

        inventoryManager?.StoreObject(targetObject);
        CloseMenu();
        Debug.Log("[ObjectMenu] Stored.");
    }

    void ExecuteExit()
    {
        CloseMenu();
        Debug.Log("[ObjectMenu] Exited.");
    }

    //  UI helpers 

    void SetButtonColor(RectTransform btn, Color c)
    {
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = c;

        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.color = c;
    }

    void ResetHighlights()
    {
        SetButtonColor(destroyButton, normalColor);
        SetButtonColor(storeButton, normalColor);
        SetButtonColor(exitButton, normalColor);
    }

    IEnumerator ShowFullMessage()
    {
        if (fullMessageObject != null)
        {
            fullMessageObject.SetActive(true);
            yield return new WaitForSeconds(fullMessageDuration);
            fullMessageObject.SetActive(false);
        }
    }
}