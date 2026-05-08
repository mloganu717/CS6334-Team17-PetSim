using UnityEngine;

[RequireComponent(typeof(CatNeeds))]
public class CatStatBridge : MonoBehaviour
{
    [Header("Sync strength (0 = no sync, 1 = instant snap)")]
    [SerializeField, Range(0f, 1f)] private float syncSpeed = 0.1f;

    private CatNeeds _needs;
    private PetStats _petStats;

    private void Awake()
    {
        _needs    = GetComponent<CatNeeds>();
        _petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

        _needs.OnAte        += OnCatAte;
        _needs.OnDrank      += OnCatDrank;
        _needs.OnUsedLitter += OnCatUsedLitter;
    }

    private void OnDestroy()
    {
        if (_needs == null) return;
        _needs.OnAte        -= OnCatAte;
        _needs.OnDrank      -= OnCatDrank;
        _needs.OnUsedLitter -= OnCatUsedLitter;
    }

    private void LateUpdate()
    {
        if (_petStats == null)
            _petStats = PetStats.Instance;

        if (_petStats == null) return;

        float dt = Time.deltaTime;

        // CatNeeds hunger 0 = full, 100 = starving
        // PetStats hunger 100 = full, 0 = empty — flip the scale
        float targetHunger = 100f - _needs.Hunger;
        float targetThirst = 100f - _needs.Thirst;

        float hungerDelta = (targetHunger - _petStats.Hunger) * syncSpeed * dt;
        float thirstDelta = (targetThirst - _petStats.Thirst) * syncSpeed * dt;

        if (Mathf.Abs(hungerDelta) > 0.01f) _petStats.ModifyStat("hunger", hungerDelta);
        if (Mathf.Abs(thirstDelta) > 0.01f) _petStats.ModifyStat("thirst", thirstDelta);
    }

    private void OnCatAte()
    {
        _petStats?.ModifyStat("hunger", 100f);
    }

    private void OnCatDrank()
    {
        _petStats?.ModifyStat("thirst", 100f);
    }

    private void OnCatUsedLitter()
    {
        _petStats?.ModifyStat("hygiene", 100f);
    }
}
