using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class raycaster : MonoBehaviour
{
    [Header("References")] // headers so i can add all this stuff more easily
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private Transform rigRoot;

    [Header("Raycast")]
    [SerializeField] private bool raycastEnabled = true;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private LayerMask menuButtonMask;

    [Header("UI")]
    [SerializeField] private TMPro.TMP_Text tooltipText;

    [Header("Line")]
    [SerializeField] private float lineWidth = 0.003f;

    [Header("Visual Offset")]
    [SerializeField] private Vector3 visualOffsetLocal = new Vector3(0.01f, -0.05f, 0.2f); // Pushed Z forward to 0.2 to avoid near-clip flickering

    private LineRenderer lineRenderer;
    private RaycastHighlightTarget currentTarget;
    private MenuButtonTarget currentMenuButton;
    private bool isHittingGround;
    private Vector3 currentGroundPoint;

    public RaycastHighlightTarget CurrentTarget => currentTarget;
    public MenuButtonTarget CurrentMenuButton => currentMenuButton;
    public bool IsHittingGround => isHittingGround;
    public Vector3 CurrentGroundPoint => currentGroundPoint;
    public float MaxDistance => maxDistance;
    public bool RaycastEnabled => raycastEnabled;

    private void Awake()
    {
        if (rayOrigin == null)
            rayOrigin = transform;

        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
    }

    private void Update() 
    {
        if (!raycastEnabled)
        {
            lineRenderer.enabled = false;
            ClearCurrentTarget();
            ClearCurrentMenuButton();
            isHittingGround = false;
            return;
        }

        lineRenderer.enabled = true;
        UpdateRaycast();
    }

    private void UpdateRaycast()
    {
        // calculate the offset
        Vector3 origin = rayOrigin.position 
            + rayOrigin.right * visualOffsetLocal.x 
            + rayOrigin.up * visualOffsetLocal.y 
            + rayOrigin.forward * visualOffsetLocal.z;

        Vector3 dir = rayOrigin.forward;
        Vector3 endPoint = origin + dir * maxDistance;
        isHittingGround = false;

        int combinedMask = interactableMask | groundMask | menuButtonMask;

        RaycastHit hit;
        // the ray now physically shoots from the offset position forward
        if (Physics.Raycast(origin, dir, out hit, maxDistance, combinedMask, QueryTriggerInteraction.Collide))
        {
            endPoint = hit.point;
            int layer = hit.collider.gameObject.layer;

            if (IsLayerInMask(layer, menuButtonMask))
            {
                SetCurrentMenuButton(hit.collider.GetComponentInParent<MenuButtonTarget>());
                ClearCurrentTarget();
            }
            else
            {
                ClearCurrentMenuButton();

                if (IsLayerInMask(layer, interactableMask))
                    SetCurrentTarget(hit.collider.GetComponentInParent<RaycastHighlightTarget>());
                else
                    ClearCurrentTarget();

                if (IsLayerInMask(layer, groundMask))
                {
                    isHittingGround = true;
                    currentGroundPoint = hit.point;
                }
            }
        }
        else
        {
            ClearCurrentTarget();
            ClearCurrentMenuButton();
        }

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPoint);

        UpdateTooltip(endPoint);
    }

    private void UpdateTooltip(Vector3 endPoint)
    {
        if (tooltipText == null) return;

        if (currentTarget != null)
        {
            tooltipText.gameObject.SetActive(true);
            tooltipText.text = currentTarget.DisplayName;

            // display above the object rather than at the exact hit point
            Vector3 objectTop = currentTarget.transform.position + Vector3.up * 0.3f;
            tooltipText.transform.position = objectTop;

            // face camera
            if (Camera.main != null)
                tooltipText.transform.rotation = Quaternion.LookRotation(tooltipText.transform.position - Camera.main.transform.position);
        }
        else
        {
            tooltipText.gameObject.SetActive(false);
        }
    }

    private void SetCurrentTarget(RaycastHighlightTarget newTarget)
    {
        if (currentTarget == newTarget) return;

        if (currentTarget != null)
            currentTarget.SetHighlighted(false);

        currentTarget = newTarget;

        if (currentTarget != null)
            currentTarget.SetHighlighted(true);
    }

    private void ClearCurrentTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.SetHighlighted(false);
            currentTarget = null;
        }
    }

    private void SetCurrentMenuButton(MenuButtonTarget newButton)
    {
        if (currentMenuButton == newButton) return;

        if (currentMenuButton != null)
            currentMenuButton.SetHovered(false);

        currentMenuButton = newButton;

        if (currentMenuButton != null)
            currentMenuButton.SetHovered(true);
    }

    private void ClearCurrentMenuButton()
    {
        if (currentMenuButton != null)
        {
            currentMenuButton.SetHovered(false);
            currentMenuButton = null;
        }
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void SetupLineRenderer()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        if (lineRenderer.material == null)
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lineRenderer.material = new Material(shader);
        }
    }

    public void TeleportRigToGround()
    {
        if (!isHittingGround || rigRoot == null)
            return;

        Vector3 headOffset = rayOrigin.position - rigRoot.position;
        headOffset.y = 0f;

        Vector3 newPos = currentGroundPoint - headOffset;
        newPos.y = rigRoot.position.y;
        rigRoot.position = newPos;

        Physics.SyncTransforms();
    }

    public void SetRaycastEnabled(bool enabled)
    {
        raycastEnabled = enabled;

        if (!raycastEnabled)
        {
            ClearCurrentTarget();
            ClearCurrentMenuButton();
            isHittingGround = false;
        }
    }

    public void SetMaxDistance(float newDistance)
    {
        maxDistance = newDistance;
    }

    private float raycastLengthBeforeUI;

    public void RaycastForUI() 
    {
        raycastLengthBeforeUI = maxDistance;
        maxDistance = 99f; // long reach for UI
    }

    public void PreviousRaycastLength() 
    {
        maxDistance = raycastLengthBeforeUI;
    }

    private void OnDisable()
    {
        ClearCurrentTarget();
        ClearCurrentMenuButton();
    }
}
