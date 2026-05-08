using UnityEngine;
using UnityEngine.Events;

public class PetStats : MonoBehaviour
{
    public static PetStats Instance { get; private set; }

    [Header("Stats (0-100)")]
    [SerializeField, Range(0, 100)] private float hunger = 100f;
    [SerializeField, Range(0, 100)] private float thirst = 100f;
    [SerializeField, Range(0, 100)] private float happiness = 100f;
    [SerializeField, Range(0, 100)] private float energy = 100f;
    [SerializeField, Range(0, 100)] private float hygiene = 100f;

    [Header("Decay Rates (per second)")]
    [SerializeField] private float hungerDecay = 1f;
    [SerializeField] private float thirstDecay = 1.5f;
    [SerializeField] private float happinessDecay = 0.5f;
    [SerializeField] private float energyDecay = 0.3f;
    [SerializeField] private float hygieneDecay = 0.2f;

    [Header("Events")]
    public UnityEvent<string> OnFeedback;

    public float Hunger => hunger;
    public float Thirst => thirst;
    public float Happiness => happiness;
    public float Energy => energy;
    public float Hygiene => hygiene;

    // Average of all five stats; useful for overall wellness checks
    public float OverallHealth => (hunger + thirst + happiness + energy + hygiene) / 5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        hunger = Mathf.Max(0, hunger - hungerDecay * Time.deltaTime);
        thirst = Mathf.Max(0, thirst - thirstDecay * Time.deltaTime);
        happiness = Mathf.Max(0, happiness - happinessDecay * Time.deltaTime);
        energy = Mathf.Max(0, energy - energyDecay * Time.deltaTime);
        hygiene = Mathf.Max(0, hygiene - hygieneDecay * Time.deltaTime);
    }

    public void ModifyStat(string stat, float amount)
    {
        switch (stat)
        {
            case "hunger": hunger = Mathf.Clamp(hunger + amount, 0, 100); break;
            case "thirst": thirst = Mathf.Clamp(thirst + amount, 0, 100); break;
            case "happiness": happiness = Mathf.Clamp(happiness + amount, 0, 100); break;
            case "energy": energy = Mathf.Clamp(energy + amount, 0, 100); break;
            case "hygiene": hygiene = Mathf.Clamp(hygiene + amount, 0, 100); break;
        }
    }

    public void RaiseFeedback(string message)
    {
        OnFeedback?.Invoke(message);
    }
}