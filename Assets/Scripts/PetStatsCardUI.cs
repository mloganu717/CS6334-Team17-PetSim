using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetStatsCardUI : MonoBehaviour
{
    [System.Serializable]
    public class StatRow
    {
        public Image fillImage;
        public TMP_Text valueText;
        public Text valueTextLegacy;

        public void UpdateRow(float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = 0;
                fillImage.fillAmount = value / 100f;
                fillImage.color = GetStatBarColor(fillImage.fillAmount);
            }

            string s = value.ToString("F1") + "%";
            if (valueText != null)
            {
                valueText.text = s;
                if (valueText is TextMeshProUGUI ugui)
                    ugui.ForceMeshUpdate(true);
            }
            else if (valueTextLegacy != null)
            {
                valueTextLegacy.text = s;
            }
        }
    }

    [Header("Source")]
    [SerializeField] private PetStats petStats;

    [Header("Rows")]
    [SerializeField] private StatRow hungerRow = new StatRow();
    [SerializeField] private StatRow thirstRow = new StatRow();
    [SerializeField] private StatRow happinessRow = new StatRow();
    [SerializeField] private StatRow hygieneRow = new StatRow();
    [SerializeField] private StatRow energyRow = new StatRow();

    private void Awake()
    {
        TryAutoFindReferences();
    }

    private void OnEnable()
    {
        TryAutoFindReferences();
        UpdateVisuals();
    }

    private void Update()
    {
        UpdateVisuals();

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetButtonDown("js5")) //close stats card with js5 or Q button on keyboard
        {
            CloseCard();
        }
    }

    private void CloseCard()
    {
        // re-enable movement
        foreach (var cm in FindObjectsByType<CharacterMovement>(FindObjectsSortMode.None))
        {
            cm.movementLocked = false;
            cm.enabled = true;
        }

        // re-enable raycast
        var ray = FindAnyObjectByType<raycaster>();
        if (ray != null)
            ray.SetRaycastEnabled(true);

        // unlock XR look
        var xr = FindAnyObjectByType<XRCardboardController>();
        if (xr != null)
            xr.lookLocked = false;

        // tell SettingsMenu to clean up its state if it's open
        var settingsMenu = FindAnyObjectByType<SettingsMenu>();
        if (settingsMenu != null && settingsMenu.gameObject.activeSelf)
            settingsMenu.CloseMenu();

        gameObject.SetActive(false);
    }

    private void TryAutoFindReferences()
    {
        if (petStats == null)
        {
            petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
        }

        AutoBindRow(hungerRow, "StatsContainer/HungerRow");
        AutoBindRow(thirstRow, "StatsContainer/ThirstRow");
        AutoBindRow(happinessRow, "StatsContainer/HappinessRow");
        AutoBindRow(hygieneRow, "StatsContainer/HygieneRow");
        AutoBindRow(energyRow, "StatsContainer/EnergyRow");

        // Alternate layout: StatsContainer is not parent of rows
        if (hungerRow.fillImage == null)
            AutoBindRow(hungerRow, "HungerRow");
        if (thirstRow.fillImage == null)
            AutoBindRow(thirstRow, "ThirstRow");
        if (happinessRow.fillImage == null)
            AutoBindRow(happinessRow, "HappinessRow");
        if (hygieneRow.fillImage == null)
            AutoBindRow(hygieneRow, "HygieneRow");
        if (energyRow.fillImage == null)
            AutoBindRow(energyRow, "EnergyRow");
    }

    private void AutoBindRow(StatRow row, string rowPath)
    {
        if (row == null)
            return;

        Transform rowTransform = transform.Find(rowPath);

        if (rowTransform == null)
            return;

        if (row.fillImage == null)
        {
            Transform fillTransform = rowTransform.Find("BarBG/Fill");
            if (fillTransform == null)
                fillTransform = FindNamedDescendant(rowTransform, "Fill");

            if (fillTransform != null)
                row.fillImage = fillTransform.GetComponent<Image>();
        }

        if (row.valueText == null && row.valueTextLegacy == null)
        {
            Transform valueTransform = FindNamedDescendant(rowTransform, "ValueText");

            if (valueTransform != null)
            {
                var ugui = valueTransform.GetComponent<TextMeshProUGUI>()
                    ?? valueTransform.GetComponentInChildren<TextMeshProUGUI>(true);
                if (ugui != null)
                    row.valueText = ugui;
                else
                {
                    row.valueText = valueTransform.GetComponent<TMP_Text>()
                        ?? valueTransform.GetComponentInChildren<TMP_Text>(true);
                    if (row.valueText == null)
                    {
                        row.valueTextLegacy = valueTransform.GetComponent<Text>()
                            ?? valueTransform.GetComponentInChildren<Text>(true);
                    }
                }
            }
        }
    }

    private void UpdateVisuals()
    {
        TryAutoFindReferences();

        if (petStats == null)
        {
            petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
        }

        if (petStats == null)
            return;

        hungerRow.UpdateRow(petStats.Hunger);
        thirstRow.UpdateRow(petStats.Thirst);
        happinessRow.UpdateRow(petStats.Happiness);
        hygieneRow.UpdateRow(petStats.Hygiene);
        energyRow.UpdateRow(petStats.Energy);
    }

    private static Transform FindNamedDescendant(Transform root, string exactName)
    {
        if (root == null) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == exactName)
                return t;
        }
        return null;
    }

    private static Color GetStatBarColor(float fillAmount)
    {
        if (fillAmount > 0.6f) return Color.green;
        if (fillAmount > 0.3f) return new Color(1f, 0.64f, 0f);
        return Color.red;
    }
}
