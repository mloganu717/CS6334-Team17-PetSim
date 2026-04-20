using UnityEngine;

public class CatMood : MonoBehaviour
{
    [Header("Starting affinity (0 = feral, 100 = devoted)")]
    [SerializeField, Range(0f, 100f)] private float startingAffinity = 40f;

    [Header("Passive decay toward neutral when ignored")]
    [SerializeField] private float decayRate   = 0.5f;
    [SerializeField] private float decayTarget = 50f;

    [Header("Thresholds")]
    public float followThreshold  = 45f;
    public float runAwayThreshold = 15f;
    public float playThreshold    = 60f;
    public float purringThreshold = 75f;

    public float Affinity { get; private set; }

    public bool IsWary     => Affinity < runAwayThreshold;
    public bool IsNeutral  => Affinity >= runAwayThreshold  && Affinity < followThreshold;
    public bool IsFriendly => Affinity >= followThreshold   && Affinity < purringThreshold;
    public bool IsDevoted  => Affinity >= purringThreshold;

    private void Awake()
    {
        Affinity = Mathf.Clamp(startingAffinity, 0f, 100f);
    }

    private void Update()
    {
        if (!Mathf.Approximately(Affinity, decayTarget))
        {
            float delta = decayRate * Time.deltaTime / 60f;
            Affinity = Mathf.MoveTowards(Affinity, decayTarget, delta);
        }
    }

    public void Reward(float amount = 5f)   => Affinity = Mathf.Clamp(Affinity + amount, 0f, 100f);
    public void Penalise(float amount = 8f) => Affinity = Mathf.Clamp(Affinity - amount, 0f, 100f);

    public void OnFed()      => Reward(12f);
    public void OnWatered()  => Reward(6f);
    public void OnGroomed()  => Reward(8f);
    public void OnStartled() => Penalise(10f);
}
