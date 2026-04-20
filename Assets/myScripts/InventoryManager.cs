// InventoryManager.cs
//


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    //  Inspector 

    [Header("Inventory Panel")]
    public GameObject inventoryPanel;
    public Image[] inventorySlots = new Image[3];
    public Color normalSlotColor = Color.white;
    public Color highlightSlotColor = Color.yellow;

    [Header("Panel Position ")]
    [Tooltip("How far in front of the camera the panel sits.")]
    public float panelDistance = 1.5f;

    [Header("Grab ")]
    public Transform grabAnchor;
    public RaycastManager raycastManager;
    public CharacterMovement playerMovement;
    public XRCardboardController xrController;

    [Tooltip("Scale of the object while held at the anchor. Increase if it looks too small.")]
    [Range(0.05f, 1f)]
    public float grabbedObjectScale = 0.4f;       

    //  Public state 

    public bool IsFull => storedObjects.Count >= MaxSlots;
    public bool IsPanelOpen { get; private set; } = false;

    //  Private 

    private const int MaxSlots = 3;

    private List<GameObject> storedObjects = new List<GameObject>();
    private List<Sprite> thumbnailSprites = new List<Sprite>();
    private List<Vector3> originalScales = new List<Vector3>();   

    private int selectedIndex = 0;
    private float navCooldownTime = 0.25f;
    private float navTimer = 0f;

    private GameObject grabbedObject = null;
    private Vector3 grabbedOriginalScale = Vector3.one;           

    private bool IsBPressed => Input.GetButtonDown("js1") || Input.GetKeyDown(KeyCode.B);
    private bool IsAPressed => Input.GetButtonDown("js0") || Input.GetKeyDown(KeyCode.A);
    private float JoyY => Input.GetAxis("Vertical");

    //  Unity 

    void Start()
    {
        if (inventoryPanel == null)
            Debug.LogError("[InventoryManager] 'inventoryPanel' is not assigned in the Inspector!");
        else
            inventoryPanel.SetActive(false);

        RefreshSlotVisuals();
    }

    void Update()
    {
        // Drop grabbed object with A 
        if (grabbedObject != null && IsAPressed)
        {
            ReleaseGrabbedObject();
            return;
        }

        if (!IsPanelOpen) return;

        PositionPanelInFrontOfCamera();

        HandleClose();
        HandleNavigation();
        HandleSelection();
    }

    //  Panel positioning 

    void PositionPanelInFrontOfCamera()
    {
        if (inventoryPanel == null) return;
        Transform cam = Camera.main.transform;
        inventoryPanel.transform.position = cam.position + cam.forward * panelDistance;
        inventoryPanel.transform.rotation = Quaternion.LookRotation(
            inventoryPanel.transform.position - cam.position);
    }

    //  Store / Remove 

    public void StoreObject(GameObject obj)
    {
        if (IsFull)
        {
            Debug.LogWarning("[Inventory] Full — cannot store more objects.");
            return;
        }

        originalScales.Add(obj.transform.localScale);           

        Sprite thumb = GenerateThumbnail(obj);
        thumbnailSprites.Add(thumb);

        obj.SetActive(false);
        storedObjects.Add(obj);

        RefreshSlotVisuals();
        Debug.Log($"[Inventory] Stored '{obj.name}'. Count={storedObjects.Count}");
    }

    public void RemoveObjectAt(int index)
    {
        if (index < 0 || index >= storedObjects.Count) return;
        storedObjects.RemoveAt(index);
        thumbnailSprites.RemoveAt(index);
        if (index < originalScales.Count) originalScales.RemoveAt(index);
        RefreshSlotVisuals();
    }

    //  Panel open/close 

    public void OpenPanel()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryManager] Cannot open — 'inventoryPanel' is null.");
            return;
        }

        IsPanelOpen = true;
        selectedIndex = 0;
        navTimer = navCooldownTime;

        PositionPanelInFrontOfCamera();
        inventoryPanel.SetActive(true);

        RefreshSlotVisuals();
        HighlightSlot(selectedIndex);

        if (playerMovement != null) playerMovement.movementLocked = true;
        if (xrController != null) xrController.lookLocked = true;
        if (raycastManager != null) raycastManager.IsDisabled = true;

        Debug.Log("[Inventory] Panel opened.");
    }

    public void ClosePanel()
    {
        IsPanelOpen = false;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (playerMovement != null) playerMovement.movementLocked = false;
        if (xrController != null) xrController.lookLocked = false;
        if (raycastManager != null) raycastManager.IsDisabled = false;

        Debug.Log("[Inventory] Panel closed.");
    }

    //  Navigation 

    void HandleClose()
    {
        bool isXPressed = Input.GetButtonDown("js2") || Input.GetKeyDown(KeyCode.X);
        bool isBandEmpty = IsBPressed && storedObjects.Count == 0;
        if (isXPressed || isBandEmpty) ClosePanel();
    }

    void HandleNavigation()
    {
        if (storedObjects.Count == 0) return;

        navTimer += Time.deltaTime;
        if (navTimer < navCooldownTime) return;

        float axis = JoyY;
        if (Mathf.Abs(axis) < 0.5f) return;

        navTimer = 0f;
        int prev = selectedIndex;

        if (axis > 0.5f) selectedIndex = Mathf.Max(0, selectedIndex - 1);
        if (axis < -0.5f) selectedIndex = Mathf.Min(storedObjects.Count - 1, selectedIndex + 1);

        if (selectedIndex != prev)
        {
            SetSlotColor(prev, normalSlotColor);
            HighlightSlot(selectedIndex);
        }
    }

    void HandleSelection()
    {
        if (!IsBPressed) return;
        if (storedObjects.Count == 0) return;
        GrabObjectFromInventory(selectedIndex);
    }

    //  Grab / Release 

    void GrabObjectFromInventory(int index)
    {
        if (index < 0 || index >= storedObjects.Count) return;

        grabbedObject = storedObjects[index];

        grabbedOriginalScale = index < originalScales.Count
            ? originalScales[index]
            : grabbedObject.transform.localScale;

        grabbedObject.SetActive(true);

        Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.detectCollisions = false; }

        if (grabAnchor != null)
        {
            grabbedObject.transform.SetParent(grabAnchor);
            grabbedObject.transform.localPosition = new Vector3(0.3f, -0.3f, 0.6f);
            grabbedObject.transform.localRotation = Quaternion.identity;
            grabbedObject.transform.localScale = Vector3.one * grabbedObjectScale; // FIX 5
        }

        RemoveObjectAt(index);
        ClosePanel();

        Debug.Log($"[Inventory] Grabbed '{grabbedObject.name}'. Press A to release.");
    }

    void ReleaseGrabbedObject()
    {
        if (grabbedObject == null) return;

        // current (anchor) position — which can be inside geometry on Android.
        Vector3 dropPos = FindDropPosition();

        grabbedObject.transform.SetParent(null);
        grabbedObject.transform.localScale = grabbedOriginalScale; 

        Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.detectCollisions = true; }

        // Get half-height so the object sits on the surface
        float halfH = GetHalfHeight(grabbedObject);
        grabbedObject.transform.position = dropPos + Vector3.up * halfH;

        Debug.Log($"[Inventory] Released '{grabbedObject.name}' at {grabbedObject.transform.position}");
        grabbedObject = null;
    }


    Vector3 FindDropPosition()
    {
        Transform cam = Camera.main.transform;


        if (raycastManager != null && raycastManager.DidHit)
            return raycastManager.CurrentHit.point;


        if (Physics.Raycast(cam.position, Vector3.down, out RaycastHit downHit, 50f))
            return downHit.point;


        Vector3 aheadFlat = cam.position + cam.forward * 1.5f;
        aheadFlat.y = 0f;
        return aheadFlat;
    }

    float GetHalfHeight(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null && col.bounds.extents.y > 0) return col.bounds.extents.y;
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null && rend.bounds.extents.y > 0) return rend.bounds.extents.y;
        return obj.transform.localScale.y * 0.5f;
    }

    //  Visuals 

    void RefreshSlotVisuals()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;

            bool hasItem = i < storedObjects.Count
                        && i < thumbnailSprites.Count
                        && thumbnailSprites[i] != null;

            if (hasItem)
            {
                inventorySlots[i].sprite = thumbnailSprites[i];
                inventorySlots[i].color = normalSlotColor;
            }
            else
            {
                inventorySlots[i].sprite = null;
                inventorySlots[i].color = new Color(1, 1, 1, 0.2f);
            }
            inventorySlots[i].enabled = true;
        }
    }

    void HighlightSlot(int index)
    {
        if (index >= 0 && index < inventorySlots.Length)
            SetSlotColor(index, highlightSlotColor);
    }

    void SetSlotColor(int index, Color c)
    {
        if (index >= 0 && index < inventorySlots.Length && inventorySlots[index] != null)
            inventorySlots[index].color = c;
    }

    //  Thumbnail generation 

    Sprite GenerateThumbnail(GameObject obj)
    {
        const int size = 128;

        GameObject camGO = new GameObject("__ThumbnailCam__");
        Camera thumbCam = camGO.AddComponent<Camera>();
        RenderTexture rt = new RenderTexture(size, size, 16);

        thumbCam.targetTexture = rt;
        thumbCam.clearFlags = CameraClearFlags.SolidColor;
        thumbCam.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
        thumbCam.orthographic = true;
        thumbCam.cullingMask = ~0;

        Bounds bounds = GetObjectBounds(obj);
        float dist = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 2f;

        camGO.transform.position = bounds.center + new Vector3(0.3f, 0.5f, -dist);
        camGO.transform.LookAt(bounds.center);
        thumbCam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.5f;

        thumbCam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        Destroy(camGO);
        rt.Release();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    Bounds GetObjectBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}