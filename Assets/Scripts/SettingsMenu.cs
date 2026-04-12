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

    private Image[] statFillImages;
    private TMPro.TMP_Text[] statValueTexts;
    private bool petStatsCardOpen;

    void Start()
    {
        SetButton(0);
        if (petStatsCard != null) petStatsCard.SetActive(false);

        // Apply starting settings
        if (playerRaycaster != null && raycastLengths.Count > currentRaycastLengthIndex)
            playerRaycaster.SetMaxDistance(raycastLengths[currentRaycastLengthIndex]);
            
        if (movementSettings != null && speeds.Count > currentSpeedIndex)
            movementSettings.SetSpeed(speeds[currentSpeedIndex]);
    }

    void Update()
    {
        // 1. Handle Pet Stats Card state
        if (petStatsCardOpen)
        {
            HandlePetStatsUpdate();
            return;
        }

        // 2. Handle Settings Menu state
        if (interactable) {
            int verticalJs = (Input.GetAxisRaw("Vertical") == 0) ? 0 : Math.Sign(Input.GetAxisRaw("Vertical") * -1);

            if (verticalJs != 0) {
                StartCoroutine(CooldownMenu());
                SetButton((currentButton + verticalJs) % menuButtons.Count);
            }

            // Confirm selection
            if (Input.GetButtonDown("js5")) { 
                StartCoroutine(CooldownMenu());
                FlashButtonGreen(currentButton);
                SelectCurrentItem();
            }

            // Back button logic to close
            if (Input.GetButtonDown("Submit") || Input.GetButtonDown("js7") || Input.GetButtonDown("js0")) {
                CloseMenu();
            }
        }
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
        // Emulate the 7 options originally defined in PlayerInteractionController
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
        
        // Also fire Inspector button events
        if (menuButtons.Count > currentButton && menuButtons[currentButton] != null) {
            menuButtons[currentButton].onClick.Invoke();
        }
    }

    public void CloseMenu() { 
        if (characterMovement != null) characterMovement.enabled = true;
        if (playerInteractionController != null) playerInteractionController.enabled = true;
        if (playerRaycaster != null) playerRaycaster.SetRaycastEnabled(true);
        gameObject.SetActive(false);
    }

    // --- Settings Behaviors ---

    public void FlashButtonGreen(int index) {
        if (index >= 0 && index < menuButtons.Count && menuButtons[index] != null) {
            StartCoroutine(FlashGreenRoutine(menuButtons[index].GetComponent<Image>()));
        }
    }

    private IEnumerator FlashGreenRoutine(Image img) {
        if (img == null) yield break;
        img.color = Color.green;
        // Use realtime so changes to Time.timeScale don't freeze the flash effect
        yield return new WaitForSecondsRealtime(0.15f); 
        if (img != null && gameObject.activeInHierarchy) {
            // Restore to yellow if it is still the currently hovered button
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
        
        if (settingsMenuContainer != null) settingsMenuContainer.SetActive(false);
        petStatsCard.SetActive(true);

        // Fetch UI Fill and Text elements if not done yet
        if (statFillImages == null || statValueTexts == null)
        {
            var fills = new List<Image>();
            var texts = new List<TMPro.TMP_Text>();
            
            foreach (var img in petStatsCard.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.name == "Fill") fills.Add(img);
            }
            
            foreach (var txt in petStatsCard.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (txt.gameObject.name == "ValueText") texts.Add(txt);
            }
            
            statFillImages = fills.ToArray();
            statValueTexts = texts.ToArray();
        }
    }

    private void HandlePetStatsUpdate()
    {
        if (petStatsCardOpen && PetStats.Instance != null && statFillImages != null && statFillImages.Length >= 5)
        {
            float hunger = PetStats.Instance.Hunger;
            float thirst = PetStats.Instance.Thirst;
            float happiness = PetStats.Instance.Happiness;
            float hygiene = PetStats.Instance.Hygiene;
            float energy = PetStats.Instance.Energy;

            statFillImages[0].fillAmount = hunger / 100f;
            statFillImages[1].fillAmount = thirst / 100f;
            statFillImages[2].fillAmount = happiness / 100f;
            statFillImages[3].fillAmount = hygiene / 100f;
            statFillImages[4].fillAmount = energy / 100f;

            // Update bar colors (Green -> Yellow -> Red)
            for (int i = 0; i < 5; i++)
            {
                statFillImages[i].color = GetStatColor(statFillImages[i].fillAmount);
            }

            if (statValueTexts != null && statValueTexts.Length >= 5)
            {
                statValueTexts[0].text = Mathf.RoundToInt(hunger).ToString() + "%";
                statValueTexts[1].text = Mathf.RoundToInt(thirst).ToString() + "%";
                statValueTexts[2].text = Mathf.RoundToInt(happiness).ToString() + "%";
                statValueTexts[3].text = Mathf.RoundToInt(hygiene).ToString() + "%";
                statValueTexts[4].text = Mathf.RoundToInt(energy).ToString() + "%";
            }
        }

        if (Input.GetButtonDown("js5") || Input.GetButtonDown("js0") || Input.GetButtonDown("js7") || Input.GetButtonDown("Submit"))
        {
            petStatsCardOpen = false;
            petStatsCard.SetActive(false);
            if (settingsMenuContainer != null) settingsMenuContainer.SetActive(true);
        }
    }

    private Color GetStatColor(float fillAmount)
    {
        if (fillAmount > 0.6f) return Color.green;
        if (fillAmount > 0.3f) return new Color(1f, 0.64f, 0f); // Orange
        return Color.red;
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

        // Lockdown: Stop movement and raycasting while in settings
        if (characterMovement != null) characterMovement.enabled = false;
        if (playerRaycaster != null) playerRaycaster.SetRaycastEnabled(false);
    }
}
