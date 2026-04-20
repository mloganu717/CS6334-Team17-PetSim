// RaycastManager.cs
//
// Button mapping (Android Bluetooth Gamepad):
//   js0 = A  → Spawn last-destroyed object at ground position
//   js1 = B  → Confirm hovered menu item  (handled in ObjectMenuManager / SettingsMenuManager)
//   js2 = X  → Open Object menu on hovered interactable
//   js3 = Y  → Teleport to ground
//   js9 = Start/OK → Open Settings menu
//
//   • Shoot a raycast, draw a LineRenderer, highlight hovered interactables
//   • Teleport (Y) and Spawn (A)
//   • Open Object menu (X) – delegates to ObjectMenuManager


using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RaycastManager : MonoBehaviour
{
    //  Inspector 

    [Header("─── Raycast ───────────────────────────────────────────────")]
    public float raycastDistance = 10f;           // changed at runtime by Settings menu
    public LayerMask raycastMask;                 

    [Header("─── Line Renderer ─────────────────────────────────────────")]
    public float lineWidth = 0.01f;
    public Color lineColor = Color.cyan;
    public Material lineMaterial;                 

    [Header("─── Highlight ──────────────────────────────────────────────")]
    public Color outlineColor = Color.white;
    [Range(0f, 10f)]
    public float outlineWidth = 5f;

    [Header("─── Teleportation (Y button / js3) ────────────────────────")]
    public Transform playerRoot;
    public LayerMask floorLayer;
    public LayerMask obstacleLayer;
    public float characterRadius = 0.3f;

    [Header("─── Destroy / Spawn (A button / js0) ─────────────────────")]
    public LayerMask interactableLayer;

    [Header("─── References ──────────────────────────────────────────────")]
    public ObjectMenuManager objectMenuManager;   

    //  Public state (read by menu managers) 

    [HideInInspector] public RaycastHit CurrentHit;
    [HideInInspector] public bool DidHit;
    [HideInInspector] public GameObject CurrentTarget;

 
    [HideInInspector] public bool IsDisabled = false;

    //  Private 

    private LineRenderer lr;
    private GameObject previousTarget;
    private GameObject lastDestroyedCopy = null;

    //  Button helpers 

    private bool IsYPressed => Input.GetButtonDown("js3") || Input.GetKeyDown(KeyCode.Y);
    private bool IsAPressed => Input.GetButtonDown("js0") || Input.GetKeyDown(KeyCode.A);
    private bool IsXPressed => Input.GetButtonDown("js2") || Input.GetKeyDown(KeyCode.X);

    //  Unity 

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        if (lineMaterial != null) lr.material = lineMaterial;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
    }

    void Update()
    {
        // Android Debug
        for (int i = 0; i < 15; i++)
        {
            if (Input.GetKeyDown("joystick button " + i))
                Debug.Log("Physical Controller Press: joystick button " + i);
        }

        if (IsDisabled)
        {
            lr.enabled = false;
            ClearHover();
            return;
        }

        lr.enabled = true;
        CastRay();
        HandleHoverTransitions();
        HandleObjectMenuOpen();
        HandleTeleport();
        HandleSpawn();


        if (Input.GetButtonDown("js9") || Input.GetKeyDown("joystick button 9"))
        {

            Debug.Log("Start Button Pressed - Opening Settings");
        }
    }

    //  Raycast + Line 

    void CastRay()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        DidHit = Physics.Raycast(ray, out CurrentHit, raycastDistance, raycastMask);
        CurrentTarget = DidHit ? CurrentHit.collider.gameObject : null;

        // Start line slightly in front of camera
        Vector3 lineStart = ray.origin + ray.direction * 0.1f;
        Vector3 lineEnd = DidHit ? CurrentHit.point : ray.origin + ray.direction * raycastDistance;

        lr.SetPosition(0, lineStart);
        lr.SetPosition(1, lineEnd);
    }

    void HandleHoverTransitions()
    {
        if (CurrentTarget == previousTarget) return;

        if (previousTarget != null)
        {
            if (!IsUIElement(previousTarget))
            {
                if (IsInteractable(previousTarget))
                    SetOutline(previousTarget, false);
                previousTarget.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
            }
        }

        if (CurrentTarget != null)
        {
            if (!IsUIElement(CurrentTarget))
            {
                if (IsInteractable(CurrentTarget))
                    SetOutline(CurrentTarget, true);
                CurrentTarget.SendMessage("OnPointerEnter", SendMessageOptions.DontRequireReceiver);
            }
        }

        previousTarget = CurrentTarget;
    }

    void ClearHover()
    {
        if (previousTarget != null)
        {
            if (!IsUIElement(previousTarget))
            {
                if (IsInteractable(previousTarget))
                    SetOutline(previousTarget, false);
                previousTarget.SendMessage("OnPointerExit", SendMessageOptions.DontRequireReceiver);
            }
            previousTarget = null;
        }
        CurrentTarget = null;
        DidHit = false;
    }

    bool IsUIElement(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("UI")
            || obj.GetComponent<UnityEngine.UI.Button>() != null
            || obj.GetComponent<TMPro.TMP_Text>() != null;
    }

    // Object Menu (X button) 

    void HandleObjectMenuOpen()
    {
        if (!IsXPressed) return;
        if (CurrentTarget == null || !IsInteractable(CurrentTarget)) return;
        if (objectMenuManager == null) return;

        objectMenuManager.OpenMenu(CurrentTarget);
    }

    // Teleport (Y button, floor only) 

    void HandleTeleport()
    {
        if (!IsYPressed || !DidHit) return;

        // If an Object menu is open, Y should not teleport
        if (objectMenuManager != null && objectMenuManager.IsMenuOpen) return;

        bool isFloor = ((1 << CurrentHit.collider.gameObject.layer) & floorLayer) != 0;
        if (!isFloor) return;

        Vector3 landingPos = CurrentHit.point + Vector3.up * 0.01f;
        Vector3 checkCenter = landingPos + Vector3.up * characterRadius;

        if (Physics.CheckSphere(checkCenter, characterRadius, obstacleLayer))
        {
            Debug.Log("[Teleport] Landing spot blocked.");
            return;
        }

        if (playerRoot == null) { Debug.LogError("[Teleport] playerRoot not assigned."); return; }

        if (playerRoot.parent != null)
        {
            playerRoot.parent.position = landingPos;
            playerRoot.localPosition = Vector3.zero;
        }
        else
        {
            playerRoot.position = landingPos;
        }

        Debug.Log($"[Teleport] Moved player to {landingPos}");
    }

    // Spawn last-destroyed object (A button, ground only)

    void HandleSpawn()
    {
        if (!IsAPressed || !DidHit) return;

        // Only spawn when NOT holding an inventory object 
        if (objectMenuManager != null && objectMenuManager.IsMenuOpen) return;

        bool isFloor = ((1 << CurrentHit.collider.gameObject.layer) & floorLayer) != 0;
        if (!isFloor) return;

        if (lastDestroyedCopy == null)
        {
            Debug.Log("[Spawn] No destroyed object to spawn.");
            return;
        }

        float halfH = GetHalfHeight(lastDestroyedCopy);
        Vector3 spawnPos = CurrentHit.point + CurrentHit.normal * halfH;

        GameObject spawned = Instantiate(lastDestroyedCopy, spawnPos, Quaternion.identity);
        spawned.SetActive(true);

        Outline o = spawned.GetComponent<Outline>();
        if (o != null) o.enabled = false;

        Debug.Log($"[Spawn] Spawned '{spawned.name.Replace("(Clone)", "").Trim()}' at {spawnPos}");

        Destroy(lastDestroyedCopy);
        lastDestroyedCopy = null;
    }

    // Called by ObjectMenuManager after Destroy action 

    public void RegisterDestroyedObject(GameObject obj)
    {
        if (lastDestroyedCopy != null) Destroy(lastDestroyedCopy);

        lastDestroyedCopy = Instantiate(obj);
        lastDestroyedCopy.SetActive(false);

        Outline copyOutline = lastDestroyedCopy.GetComponent<Outline>();
        if (copyOutline != null) copyOutline.enabled = false;

        Debug.Log($"[RaycastManager] Saved copy of '{obj.name}' for spawn.");
    }

    // Helper functions

    bool IsInteractable(GameObject obj)
        => obj != null && ((1 << obj.layer) & interactableLayer) != 0;

    void SetOutline(GameObject target, bool enable)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null) return;
        if (enable)
        {
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
        }
        outline.enabled = enable;
    }


    float GetHalfHeight(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        Renderer rend = obj.GetComponent<Renderer>();
        if (col != null && col.bounds.extents.y > 0) return col.bounds.extents.y;
        if (rend != null && rend.bounds.extents.y > 0) return rend.bounds.extents.y;
        return obj.transform.localScale.y * 0.5f;
    }


    public void SetRaycastLength(float length) => raycastDistance = length;
}