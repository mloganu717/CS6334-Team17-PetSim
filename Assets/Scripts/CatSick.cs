using System.Collections.Generic;
using UnityEngine;

public class CatSick : MonoBehaviour
{
    [SerializeField] private PetStats petStats;
    [SerializeField] private Renderer[] meshRenderers;

    [SerializeField] private float healthyOverallMin = 48f;
    [SerializeField] private float sickOverallMax = 14f;

    [SerializeField] private Color sickTintMultiplier = new Color(0.32f, 1f, 0.44f);

    private MaterialPropertyBlock _block;
    private List<MatSnap> _snaps;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private struct MatSnap
    {
        public Renderer Renderer;
        public int MaterialIndex;
        public int PropertyId;
        public Color BaseColor;
    }

    private void Awake()
    {
        _block = new MaterialPropertyBlock();

        if (petStats == null)
        {
            petStats = GetComponent<PetStats>()
                ?? PetStats.Instance
                ?? FindAnyObjectByType<PetStats>();
        }

        if (meshRenderers == null || meshRenderers.Length == 0)
        {
            var found = GetComponentsInChildren<Renderer>(true);
            var list = new List<Renderer>();
            foreach (var r in found)
            {
                if (r == null) continue;
                if (r is ParticleSystemRenderer) continue;
                list.Add(r);
            }
            meshRenderers = list.ToArray();
        }
    }

    private void Start()
    {
        CacheMaterialBaseColors();
    }

    private void CacheMaterialBaseColors()
    {
        _snaps = new List<MatSnap>();
        foreach (var r in meshRenderers)
        {
            if (r == null) continue;
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;
                int pid = BaseColorId;
                if (!m.HasProperty(pid))
                    pid = ColorId;
                if (!m.HasProperty(pid))
                    continue;
                _snaps.Add(new MatSnap
                {
                    Renderer = r,
                    MaterialIndex = i,
                    PropertyId = pid,
                    BaseColor = m.GetColor(pid),
                });
            }
        }
    }

    private void LateUpdate()
    {
        if (_snaps == null || _snaps.Count == 0)
            return;

        if (petStats == null)
            petStats = PetStats.Instance ?? FindAnyObjectByType<PetStats>();
        if (petStats == null)
            return;

        float overall = petStats.OverallHealth;
        float sickBlend = ComputeSickBlend(overall);

        foreach (var s in _snaps)
        {
            Color mul = Color.Lerp(Color.white, sickTintMultiplier, sickBlend);
            Color target = s.BaseColor * mul;

            _block.Clear();
            s.Renderer.GetPropertyBlock(_block, s.MaterialIndex);
            _block.SetColor(s.PropertyId, target);
            s.Renderer.SetPropertyBlock(_block, s.MaterialIndex);
        }
    }

    private float ComputeSickBlend(float overall)
    {
        if (overall >= healthyOverallMin)
            return 0f;
        if (overall <= sickOverallMax)
            return 1f;
        return 1f - Mathf.InverseLerp(sickOverallMax, healthyOverallMin, overall);
    }

    private void OnDisable()
    {
        if (_snaps == null) return;
        foreach (var s in _snaps)
        {
            if (s.Renderer == null) continue;
            _block.Clear();
            _block.SetColor(s.PropertyId, s.BaseColor);
            s.Renderer.SetPropertyBlock(_block, s.MaterialIndex);
        }
    }
}
