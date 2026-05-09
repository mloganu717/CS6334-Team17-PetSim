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
    [SerializeField] private string bedTag    = "CatBed";

    public Transform NearestFood   { get; private set; }
    public Transform NearestWater  { get; private set; }
    public Transform NearestLitter { get; private set; }
    public Transform NearestToy    { get; private set; }
    public Transform NearestBed    { get; private set; }

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
        Transform nf = null;
        Transform nw = null;
        Transform nl = null;
        Transform nt = null;
        Transform nb = null;

        float bestFood = float.MaxValue;
        float bestWater = float.MaxValue;
        float bestLitter = float.MaxValue;
        float bestToy = float.MaxValue;
        float bestBed = float.MaxValue;

        var hits = Physics.OverlapSphere(transform.position, senseRadius, senseLayer);

        foreach (var hit in hits)
        {
            TryPickNearestTagged(hit.transform, foodTag,   ref bestFood,   ref nf);
            TryPickNearestTagged(hit.transform, waterTag,  ref bestWater,  ref nw);
            TryPickNearestTagged(hit.transform, litterTag, ref bestLitter, ref nl);
            TryPickNearestTagged(hit.transform, toyTag,    ref bestToy,    ref nt);
            TryPickNearestTagged(hit.transform, bedTag,    ref bestBed,    ref nb);
        }

        NearestFood   = nf;
        NearestWater  = nw;
        NearestLitter = nl;
        NearestToy    = nt;
        NearestBed    = nb;
    }

    private static Transform FindTaggedAncestor(Transform start, string tag)
    {
        if (string.IsNullOrEmpty(tag) || start == null) return null;
        for (Transform t = start; t != null; t = t.parent)
        {
            if (t.CompareTag(tag)) return t;
        }
        return null;
    }

    private void TryPickNearestTagged(Transform colliderTransform, string tag, ref float bestSqrDist, ref Transform nearest)
    {
        Transform tagged = FindTaggedAncestor(colliderTransform, tag);
        if (tagged == null) return;

        float dist = Vector3.SqrMagnitude(tagged.position - transform.position);
        if (dist < bestSqrDist)
        {
            bestSqrDist = dist;
            nearest = tagged;
        }
    }

    public bool IsNearTarget(Transform target, float radius)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.position) <= radius;
    }
}
