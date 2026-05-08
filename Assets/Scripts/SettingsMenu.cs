using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsMenu : MonoBehaviour
{
    [Header("Core UI")]
    public List<Button> menuButtons = new List<Button>();
    public bool interactable = true;
    public int currentButton = 0;

    [Header("Feature Settings")]
    public List<float> speeds = new List<float>(){ 2f, 5f, 10f }; 
    public string[] speedLabels = new string[]{ "Slow", "Normal", "Fast" };
    public int currentSpeedIndex = 1; 

    public List<float> raycastLengths = new List<float>(){ 5f, 10f, 20f }; 
    public int currentRaycastLengthIndex = 1;

    public string mainMenuSceneName = "MainMenu";

    [Header("References")]
    public CharacterMovement characterMovement;
    public MovementSettings movementSettings;
    public InventoryController inventory;
    public raycaster playerRaycaster;
    public PlayerInteractionController playerInteractionController;
    public GameObject petStatsCard;
    public GameObject settingsMenuContainer;

    [SerializeField] private XRCardboardController xrCardboardController;

    private bool petStatsCardOpen;
    private bool openedFromCat = false;
    private float petStatsOpenTime;

    private void Awake()
    {
        if (xrCardboardController == null)
            xrCardboardController = FindAnyObjectByType<XRCardboardController>();

        if (playerInteractionController != null && playerInteractionController.PlayerMovement != null)
            characterMovement = playerInteractionController.PlayerMovement;
    }

    private static void LockAllCharacterMovement()
    {
        foreach (var cm in UnityEngine.Object.FindObjectsByType<CharacterMovement>(FindObjectsSortMode.None))
        {
            cm.enabled = true;
            cm.movementLocked = true;
        }
    }

    private void UnlockAllCharacterMovementIfAllowed()
    {
        if (inventory != null && inventory.IsInventoryOpen())
            return;
        foreach (var cm in UnityEngine.Object.FindObjectsByType<CharacterMovement>(FindObjectsSortMode.None))
        {
            cm.movementLocked = false;
            cm.enabled = true;
        }
    }

    void Start()
    {
        SetButton(0);
        if (petStatsCard != null) petStatsCard.SetActive(false);

        // apply starting settings
        if (playerRaycaster != null && raycastLengths.Count > currentRaycastLengthIndex)
            playerRaycaster.SetMaxDistance(raycastLengths[currentRaycastLengthIndex]);
            
        if (movementSettings != null && speeds.Count > currentSpeedIndex)
            movementSettings.SetSpeed(speeds[currentSpeedIndex]);
    }

    void Update()
    {
        // pet Stats Card state
        if (petStatsCardOpen)
        {
            HandlePetStatsUpdate();
            return;
        }

        // handle Settings Menu state
        if (interactable) {
            int verticalJs = (Input.GetAxisRaw("Vertical") == 0) ? 0 : Math.Sign(Input.GetAxisRaw("Vertical") * -1);

            if (verticalJs != 0) {
                StartCoroutine(CooldownMenu());
                SetButton((currentButton + verticalJs) % menuButtons.Count);
            }

            // confirm selection
            if (Input.GetButtonDown("js5")) { 
                StartCoroutine(CooldownMenu());
                FlashButtonGreen(currentButton);
                SelectCurrentItem();
            }

            if (Input.GetButtonDown("Submit") || Input.GetButtonDown("js7") || Input.GetButtonDown("js0")) {
                CloseMenu();
            }
        }
    }
    public void OpenPetStatsCardStandalone() //can open petstats card without menu open
    {
    openedFromCat = true;
    gameObject.SetActive(true); // open the settings canvas
    if (settingsMenuContainer != null) settingsMenuContainer.SetActive(false); // hide buttons
    OpenPetStatsCard(); // show stats card
    }   


    public void SetButton(int newButton) {
        if (menuButtons.Count == 0) return;

        if (newButton < 0) {
            newButton = menuButtons.Count + newButton;
        }

        if (menuButtons[currentButton] != null) {
            menuButtons[currentButton].GetComponent<Image>().color = Color.white;
        }
        
        currentButton = newButton;
        
        if (menuButtons[currentButton] != null) {
            menuButtons[currentButton].GetComponent<Image>().color = Color.yellow;
        }
    }

    private void SelectCurrentItem()
    {
        //  7 options
        switch (currentButton)
        {
            case 0: CloseMenu(); break;
            case 1: CycleRaycastLength(); break;
            case 2: OpenInventoryPanel(); break;
            case 3: OpenPetStatsCard(); break;
            case 4: CycleSpeed(); break;
            case 5: ReturnToMainMenu(); break;
            case 6: QuitApplication(); break;
        }
        
        //  button events
        if (menuButtons.Count > currentButton && menuButtons[currentButton] != null) {
            menuButtons[currentButton].onClick.Invoke();
        }
    }

    public void CloseMenu() { 
        UnlockAllCharacterMovementIfAllowed();
        if (xrCardboardController != null)
            xrCardboardController.lookLocked = false;
        if (playerInteractionController != null) playerInteractionController.enabled = true;
        if (playerRaycaster != null && (inventory == null || !inventory.IsInventoryOpen()))
            playerRaycaster.SetRaycastEnabled(true);
        gameObject.SetActive(false);
    }

    //settings behaviors 

    public void FlashButtonGreen(int index) {
        if (index >= 0 && index < menuButtons.Count && menuButtons[index] != null) {
            StartCoroutine(FlashGreenRoutine(menuButtons[index].GetComponent<Image>()));
        }
    }

    private IEnumerator FlashGreenRoutine(Image img) {
        if (img == null) yield break;
        img.color = Color.green;
        // use realtime so changes to Time.timeScale don't freeze the flash effect
        yield return new WaitForSecondsRealtime(0.15f); 
        if (img != null && gameObject.activeInHierarchy) {
            // restore to yellow if it is still the currently hovered button
            img.color = (menuButtons.IndexOf(img.GetComponent<Button>()) == currentButton) ? Color.yellow : Color.white;
        }
    }

    public void CycleRaycastLength()
    {
        if (playerRaycaster == null) return;
        currentRaycastLengthIndex = (currentRaycastLengthIndex + 1) % raycastLengths.Count;
        float nextDist = raycastLengths[currentRaycastLengthIndex];
        
        playerRaycaster.SetMaxDistance(nextDist);
        UpdateSettingButtonText(1, "Raycast: " + nextDist + "m");
    }

    public void OpenInventoryPanel()
    {
        if (inventory != null) inventory.OpenInventory();
        CloseMenu();
    }

    public void CycleSpeed()
    {
        if (movementSettings == null) return;
        currentSpeedIndex = (currentSpeedIndex + 1) % speeds.Count;
        float nextSpeed = speeds[currentSpeedIndex];
        string nextLabel = speedLabels[currentSpeedIndex];
        
        movementSettings.SetSpeed(nextSpeed);
        UpdateSettingButtonText(4, "Speed: " + nextLabel);
    }

    public void OpenPetStatsCard()
    {
        if (petStatsCard == null) return;
        petStatsCardOpen = true;
        petStatsOpenTime = Time.unscaledTime;

        if (settingsMenuContainer != null) settingsMenuContainer.SetActive(false);
        petStatsCard.SetActive(true);

        EnsurePetStatsCardUiDriver();

        ApplyPetStatsModalLock();
    }

    private void EnsurePetStatsCardUiDriver()
    {
        if (petStatsCard == null) return;
        if (petStatsCard.GetComponent<PetStatsCardUI>() == null)
            petStatsCard.AddComponent<PetStatsCardUI>();
    }

    private void ApplyPetStatsModalLock()
    {
        LockAllCharacterMovement();
        if (xrCardboardController != null)
            xrCardboardController.lookLocked = true;
    }

    private void ClearPetStatsLookLockOnly()
    {
        if (xrCardboardController != null)
            xrCardboardController.lookLocked = false;
    }

    private void HandlePetStatsUpdate()
    {
        // Bars and ValueText are driven by PetStatsCardUI on petStatsCard (added at runtime if missing).

        if (Time.unscaledTime < petStatsOpenTime + 0.3f) return;

        if (AnyInputPressedToDismissStatsCard())
        {
            petStatsCardOpen = false;
            petStatsCard.SetActive(false);
            
            if (openedFromCat)
            {
                openedFromCat = false;
                CloseMenu();
            }
            else
            {
                ClearPetStatsLookLockOnly();
                if (settingsMenuContainer != null) settingsMenuContainer.SetActive(true);
            }
        }
    }

    private static bool AnyInputPressedToDismissStatsCard()
    {
        if (Input.anyKeyDown)
            return true;

        for (int i = 0; i <= 14; i++)
        {
            if (Input.GetButtonDown("js" + i))
                return true;
        }

        if (Input.GetButtonDown("Submit") || Input.GetButtonDown("Cancel") ||
            Input.GetButtonDown("Jump") || Input.GetButtonDown("Fire1") ||
            Input.GetButtonDown("Fire2") || Input.GetButtonDown("Fire3"))
            return true;

        for (int i = 0; i < 20; i++)
        {
            if (Input.GetKeyDown("joystick button " + i))
                return true;
        }

        return false;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitApplication()
    {
        Application.Quit();
        
    }

    private void UpdateSettingButtonText(int index, string newText)
    {
        if (index < 0 || index >= menuButtons.Count) return;
        var btn = menuButtons[index];
        if (btn == null) return;

        var text = btn.GetComponentInChildren<UnityEngine.UI.Text>();
        if (text != null) text.text = newText;
        else
        {
            var tmp = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null) tmp.text = newText;
        }
    }

    public IEnumerator CooldownMenu() {
        interactable = false;
        yield return new WaitForSeconds(0.20f);
        interactable = true;
    }

    void OnEnable()
    {
        interactable = true;
        petStatsCardOpen = false;
        SetButton(currentButton);

        // Lock all CharacterMovement
        LockAllCharacterMovement();
        if (playerRaycaster != null) playerRaycaster.SetRaycastEnabled(false);
    }
}
