using UnityEngine;
using System;

public enum NeedType { None, Food, Water, Litter }

public class CatNeeds : MonoBehaviour
{
    [Header("Hunger")]
    [SerializeField, Range(0f, 100f)] private float startHunger = 10f;
    [SerializeField] private float hungerRatePerMinute = 4f;
    [SerializeField] public float hungerThreshold = 70f;
    [SerializeField] private float hungerSatisfied = 5f;

    [Header("Thirst")]
    [SerializeField, Range(0f, 100f)] private float startThirst = 10f;
    [SerializeField] private float thirstRatePerMinute = 6f;
    [SerializeField] public float thirstThreshold = 65f;
    [SerializeField] private float thirstSatisfied = 5f;

    [Header("Bladder")]
    [SerializeField, Range(0f, 100f)] private float startBladder = 0f;
    [SerializeField] private float bladderRatePerMinute = 3f;
    [SerializeField] public float bladderThreshold = 80f;
    [SerializeField] private float bladderRelieved = 0f;

    public float Hunger  { get; private set; }
    public float Thirst  { get; private set; }
    public float Bladder { get; private set; }

    public bool IsHungry    => Hunger  >= hungerThreshold;
    public bool IsThirsty   => Thirst  >= thirstThreshold;
    public bool NeedsLitter => Bladder >= bladderThreshold;

    public NeedType MostUrgentNeed
    {
        get
        {
            if (NeedsLitter) return NeedType.Litter;
            if (IsHungry)    return NeedType.Food;
            if (IsThirsty)   return NeedType.Water;
            return NeedType.None;
        }
    }

    public event Action OnAte;
    public event Action OnDrank;
    public event Action OnUsedLitter;

    private void Awake()
    {
        Hunger  = Mathf.Clamp(startHunger,  0f, 100f);
        Thirst  = Mathf.Clamp(startThirst,  0f, 100f);
        Bladder = Mathf.Clamp(startBladder, 0f, 100f);
    }

    private void Update()
    {
        float dt = Time.deltaTime / 60f;

        Hunger  = Mathf.Clamp(Hunger  + hungerRatePerMinute  * dt, 0f, 100f);
        Thirst  = Mathf.Clamp(Thirst  + thirstRatePerMinute  * dt, 0f, 100f);
        Bladder = Mathf.Clamp(Bladder + bladderRatePerMinute * dt, 0f, 100f);
    }

    public void Eat()
    {
        Hunger  = hungerSatisfied;
        Bladder = Mathf.Clamp(Bladder + 15f, 0f, 100f);
        OnAte?.Invoke();
    }

    public void Drink()
    {
        Thirst  = thirstSatisfied;
        Bladder = Mathf.Clamp(Bladder + 8f, 0f, 100f);
        OnDrank?.Invoke();
    }

    public void RelieveBladder()
    {
        Bladder = bladderRelieved;
        OnUsedLitter?.Invoke();
    }
}
