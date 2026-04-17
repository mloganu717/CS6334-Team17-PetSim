#if !UNITY_EDITOR
using Google.XR.Cardboard;
using UnityEngine.XR.Management;
#endif
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SpatialTracking;

public class XRCardboardController : MonoBehaviour
{
    [SerializeField]
    Transform cameraTransform = default;
    [SerializeField]
    GameObject vrGroup = default;
    [SerializeField]
    GameObject standardGroup = default;
    [SerializeField]
    XRCardboardInputModule vrInputModule = default;
    [SerializeField]
    StandaloneInputModule standardInputModule = default;
    [SerializeField, Range(.05f, 2)]
    float dragRate = .2f;

    // ── FIX: set this to true while any menu is open to freeze look rotation ──
    [HideInInspector] public bool lookLocked = false;

    TrackedPoseDriver poseDriver;
    Camera cam;
    Quaternion initialRotation;
    Quaternion attitude;
    Vector2 dragDegrees;
    float defaultFov;

#if UNITY_EDITOR
    Vector3 lastMousePos;
    bool vrActive;
#endif

    void Awake()
    {
        cam = cameraTransform.GetComponent<Camera>();
        poseDriver = cameraTransform.GetComponent<TrackedPoseDriver>();
        defaultFov = cam.fieldOfView;
        initialRotation = cameraTransform.rotation;
    }

    void Start()
    {
#if UNITY_EDITOR
        SetObjects(vrActive);
#else
        SetObjects(UnityEngine.XR.XRSettings.enabled);
#endif
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape))
#else
        if (Api.IsCloseButtonPressed)
#endif
            DisableVR();

        // ── FIX: skip ALL rotation input when a menu is open ──────────────────
        if (lookLocked)
        {
            // Also disable the TrackedPoseDriver (gyro) while locked.
            // We toggle it each frame so it gracefully resumes when unlocked.
            if (poseDriver != null) poseDriver.enabled = false;
            return;
        }

        // Re-enable gyro tracking when not locked
        if (poseDriver != null) poseDriver.enabled = true;

#if UNITY_EDITOR
        if (vrActive)
            SimulateVR();
        else
            SimulateDrag();
#else
        if (UnityEngine.XR.XRSettings.enabled)
        {
            // VR mode: TrackedPoseDriver handles rotation, nothing to do here
        }
        else
        {
            CheckDrag();
        }
#endif

        attitude = initialRotation * Quaternion.Euler(dragDegrees.x, 0, 0);
        cameraTransform.rotation = Quaternion.Euler(0, -dragDegrees.y, 0) * attitude;
    }

    public void ResetCamera()
    {
        cameraTransform.rotation = initialRotation;
        dragDegrees = Vector2.zero;
    }

    public void DisableVR()
    {
#if UNITY_EDITOR
        vrActive = false;
#else
        var xrManager = XRGeneralSettings.Instance.Manager;
        if (xrManager.isInitializationComplete)
        {
            xrManager.StopSubsystems();
            xrManager.DeinitializeLoader();
        }
#endif
        SetObjects(false);
        ResetCamera();
        cam.ResetAspect();
        cam.fieldOfView = defaultFov;
        cam.ResetProjectionMatrix();
        cam.ResetWorldToCameraMatrix();
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }

    public void EnableVR() => EnableVRCoroutine();

    public Coroutine EnableVRCoroutine()
    {
        return StartCoroutine(enableVRRoutine());

        IEnumerator enableVRRoutine()
        {
            SetObjects(true);
#if UNITY_EDITOR
            yield return null;
            vrActive = true;
#else
            var xrManager = XRGeneralSettings.Instance.Manager;
            if (!xrManager.isInitializationComplete)
                yield return xrManager.InitializeLoader();
            xrManager.StartSubsystems();
#endif
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            ResetCamera();
        }
    }

    void SetObjects(bool vrActive)
    {
        standardGroup.SetActive(!vrActive);
        vrGroup.SetActive(vrActive);
        standardInputModule.enabled = !vrActive;
        vrInputModule.enabled = vrActive;
        poseDriver.enabled = vrActive;
    }

    void CheckDrag()
    {
        if (Input.touchCount <= 0)
            return;

        Touch touch = Input.GetTouch(0);
        dragDegrees.x += touch.deltaPosition.y * dragRate;
        dragDegrees.y += touch.deltaPosition.x * dragRate;
    }

#if UNITY_EDITOR
    void SimulateVR()
    {
        var mousePos = Input.mousePosition;
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            var delta = mousePos - lastMousePos;
            dragDegrees.x -= delta.y * dragRate;
            dragDegrees.y -= delta.x * dragRate;
        }
        lastMousePos = mousePos;
    }

    void SimulateDrag()
    {
        var mousePos = Input.mousePosition;
        if (Input.GetMouseButton(0))
        {
            var delta = mousePos - lastMousePos;
            dragDegrees.x += delta.y * dragRate;
            dragDegrees.y += delta.x * dragRate;
        }
        lastMousePos = mousePos;
    }
#endif
}