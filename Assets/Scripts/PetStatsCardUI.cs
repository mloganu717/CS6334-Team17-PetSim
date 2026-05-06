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

        public void UpdateRow(float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = 0;
                fillImage.fillAmount = value / 100f;
            }

            if (valueText != null)
            {
                valueText.text = Mathf.RoundToInt(value).ToString();
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

            if (fillTransform != null)
            {
                row.fillImage = fillTransform.GetComponent<Image>();
            }
        }

        if (row.valueText == null)
        {
            Transform valueTransform = rowTransform.Find("ValueText");

            if (valueTransform != null)
            {
                row.valueText = valueTransform.GetComponent<TMP_Text>();
            }
        }
    }

    private void UpdateVisuals()
    {
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
}