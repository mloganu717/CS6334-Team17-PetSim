// SettingsMenuManager.cs 
//


using UnityEngine;
using TMPro;

public class SettingsMenuManager : MonoBehaviour
{
    [Header("─── Settings Panel (must be World Space Canvas) ───────────────")]
    public Canvas settingsCanvas;      
    public GameObject settingsPanel;        

    [Tooltip("How far in front of camera the settings panel sits (metres).")]
    public float panelDistance = 1.5f;

    [Header("─── Menu Item Texts (0=Resume 1=RayLength 2=Inventory 3=Speed 4=Quit)")]
    public TMP_Text[] menuItemTexts = new TMP_Text[5];

    [Header("─── Highlight ───────────────────────────────────────────────")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;

    [Header("─── References ──────────────────────────────────────────────")]
    public RaycastManager raycastManager;
    public ObjectMenuManager objectMenuManager;
    public InventoryManager inventoryManager;
    public CharacterMovement playerMovement;
    public XRCardboardController xrController;

    // Private 

    private bool IsOpen = false;
    private int selectedIndex = 0;

    private float[] raycastLengths = { 1f, 10f, 50f };
    private int raycastLengthIndex = 1;

    private float[] speedValues = { 5f, 10f, 20f };
    private string[] speedLabels = { "Low", "Medium", "High" };
    private int speedIndex = 1;

    private float navTimer = 0f;
    private float navCooldown = 0.25f;

    private bool IsOKPressed => Input.GetButtonDown("js9") || Input.GetKeyDown(KeyCode.Return);
    private bool IsBPressed => Input.GetButtonDown("js1") || Input.GetKeyDown(KeyCode.B);
    private float JoyY => Input.GetAxis("Vertical");

    //  Unity 

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        UpdateMenuLabels();
    }

    void Update()
    {
        if (!IsOpen) { if (IsOKPressed) OpenMenu(); return; }

        // Keep panel locked in front of camera while open
        PositionPanelInFrontOfCamera();

        HandleNavigation();
        HandleSelection();
    }

    //  Panel positioning 

    void PositionPanelInFrontOfCamera()
    {
        if (settingsPanel == null) return;
        Transform cam = Camera.main.transform;
        settingsPanel.transform.position = cam.position + cam.forward * panelDistance;
        settingsPanel.transform.rotation = Quaternion.LookRotation(
            settingsPanel.transform.position - cam.position);
    }

    //  Open / Close 

    public void OpenMenu()
    {
        IsOpen = true;
        selectedIndex = 0;
        navTimer = navCooldown; 

        // Position before activating 
        PositionPanelInFrontOfCamera();
        settingsPanel.SetActive(true);

        UpdateMenuLabels();
        HighlightItem(selectedIndex);

        // Lock movement and look 
        if (playerMovement != null) playerMovement.movementLocked = true;
        if (xrController != null) xrController.lookLocked = true;
        if (raycastManager != null) raycastManager.IsDisabled = true;

        if (objectMenuManager != null && objectMenuManager.IsMenuOpen)
            objectMenuManager.CloseMenu(false);

        Debug.Log("[SettingsMenu] Opened.");
    }

    public void CloseMenu()
    {
        IsOpen = false;
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (playerMovement != null) playerMovement.movementLocked = false;
        if (xrController != null) xrController.lookLocked = false;
        if (raycastManager != null) raycastManager.IsDisabled = false;

        Debug.Log("[SettingsMenu] Closed.");
    }

    //  Navigation (joystick Y) 

    void HandleNavigation()
    {
        navTimer += Time.deltaTime;
        if (navTimer < navCooldown) return;
        float axis = JoyY;
        if (Mathf.Abs(axis) < 0.5f) return;
        navTimer = 0f;
        int prev = selectedIndex;
        if (axis > 0.5f) selectedIndex = (selectedIndex - 1 + 5) % 5;
        if (axis < -0.5f) selectedIndex = (selectedIndex + 1) % 5;
        if (selectedIndex != prev) { SetItemColor(prev, normalColor); HighlightItem(selectedIndex); }
    }

    //  Selection (B button) 

    void HandleSelection()
    {
        if (!IsBPressed) return;
        switch (selectedIndex)
        {
            case 0: SelectResume(); break;
            case 1: SelectRaycastLength(); break;
            case 2: SelectInventory(); break;
            case 3: SelectSpeed(); break;
            case 4: SelectQuit(); break;
        }
    }

    //  Actions 

    void SelectResume() { CloseMenu(); }

    void SelectInventory()
    {
        CloseMenu();
        inventoryManager?.OpenPanel();
    }

    void SelectRaycastLength()
    {
        raycastLengthIndex = (raycastLengthIndex + 1) % raycastLengths.Length;
        if (raycastManager != null) raycastManager.SetRaycastLength(raycastLengths[raycastLengthIndex]);
        UpdateMenuLabels();
        Debug.Log($"[SettingsMenu] Raycast length → {raycastLengths[raycastLengthIndex]}m");
    }

    void SelectSpeed()
    {
        speedIndex = (speedIndex + 1) % speedValues.Length;
        ApplySpeed(speedValues[speedIndex]);
        UpdateMenuLabels();
        Debug.Log($"[SettingsMenu] Speed → {speedLabels[speedIndex]}");
    }

    void SelectQuit()
    {
        Debug.Log("[SettingsMenu] Quit.");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    //  Helper Functions

    void ApplySpeed(float speed)
    {
        if (playerMovement == null) return;
        var t = playerMovement.GetType();
        var f = t.GetField("moveSpeed") ?? t.GetField("speed") ?? t.GetField("movementSpeed");
        if (f != null) { f.SetValue(playerMovement, speed); return; }
        var p = t.GetProperty("moveSpeed") ?? t.GetProperty("speed");
        if (p != null) p.SetValue(playerMovement, speed);
    }

    void UpdateMenuLabels()
    {
        if (menuItemTexts.Length < 5) return;
        if (menuItemTexts[0] != null) menuItemTexts[0].text = "Resume";
        if (menuItemTexts[1] != null) menuItemTexts[1].text = $"Raycast Length: {raycastLengths[raycastLengthIndex]}m";
        if (menuItemTexts[2] != null) menuItemTexts[2].text = "Inventory";
        if (menuItemTexts[3] != null) menuItemTexts[3].text = $"Speed: {speedLabels[speedIndex]}";
        if (menuItemTexts[4] != null) menuItemTexts[4].text = "Quit";
    }

    void HighlightItem(int index) => SetItemColor(index, highlightColor);

    void SetItemColor(int index, Color c)
    {
        if (index >= 0 && index < menuItemTexts.Length && menuItemTexts[index] != null)
            menuItemTexts[index].color = c;
    }
}