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

    [Header("Line")]
    [SerializeField] private float lineWidth = 0.01f;

    [Header("Visual Offset")]
    [SerializeField] private Vector3 visualOffsetLocal = new Vector3(0.02f, -0.02f, 0.08f);

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
        Vector3 origin = rayOrigin.position;
        Vector3 dir = rayOrigin.forward;

        // offset the line visually so it doesn't sit on the controller centre
        Vector3 visualOrigin = origin
            + rayOrigin.right * visualOffsetLocal.x
            + rayOrigin.up * visualOffsetLocal.y
            + rayOrigin.forward * visualOffsetLocal.z;

        Vector3 endPoint = origin + dir * maxDistance;
        isHittingGround = false;

        int combinedMask = interactableMask | groundMask | menuButtonMask;

        RaycastHit hit;
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

        lineRenderer.SetPosition(0, visualOrigin);
        lineRenderer.SetPosition(1, endPoint);
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

    private void OnDisable()
    {
        ClearCurrentTarget();
        ClearCurrentMenuButton();
    }
}
