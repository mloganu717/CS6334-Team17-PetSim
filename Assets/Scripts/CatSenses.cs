using UnityEngine;

public class CatSenses : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float senseRadius  = 12f;
    [SerializeField] private float scanInterval = 0.5f;
    [SerializeField] private LayerMask senseLayer = Physics.AllLayers;

    [Header("Tags")]
    [SerializeField] private string foodTag   = "CatFood";
    [SerializeField] private string waterTag  = "CatWater";
    [SerializeField] private string litterTag = "CatLitter";
    [SerializeField] private string toyTag    = "CatToy";

    public Transform NearestFood   { get; private set; }
    public Transform NearestWater  { get; private set; }
    public Transform NearestLitter { get; private set; }
    public Transform NearestToy    { get; private set; }

    private float _nextScanTime;

    private void Update()
    {
        if (Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + scanInterval;
            Scan();
        }
    }

    private void Scan()
    {
        NearestFood = NearestWater = NearestLitter = NearestToy = null;

        float bestFood   = float.MaxValue;
        float bestWater  = float.MaxValue;
        float bestLitter = float.MaxValue;
        float bestToy    = float.MaxValue;

        var hits = Physics.OverlapSphere(transform.position, senseRadius, senseLayer);

        foreach (var hit in hits)
        {
            float dist = Vector3.SqrMagnitude(hit.transform.position - transform.position);

            if (hit.CompareTag(foodTag)   && dist < bestFood)   { bestFood   = dist; NearestFood   = hit.transform; }
            if (hit.CompareTag(waterTag)  && dist < bestWater)  { bestWater  = dist; NearestWater  = hit.transform; }
            if (hit.CompareTag(litterTag) && dist < bestLitter) { bestLitter = dist; NearestLitter = hit.transform; }
            if (hit.CompareTag(toyTag)    && dist < bestToy)    { bestToy    = dist; NearestToy    = hit.transform; }
        }
    }

    public bool IsNearTarget(Transform target, float radius)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= radius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.15f);
        Gizmos.DrawSphere(transform.position, senseRadius);
    }
}
