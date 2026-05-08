using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// Setup:
//   1. Create a Screen Space Overlay Canvas if you don't have one
//   2. Add a vertical panel anchored to a corner (e.g. top-right)
//   3. Add a Vertical Layout Group to it
//   4. Add a Content Size Fitter set to Preferred Size
//   5. Create a prefab: UI GameObject with an Image component, size ~60x60
//   6. Assign the panel as iconContainer and the prefab as iconPrefab

public class PetMoodHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PetStats petStats;
    [SerializeField] private CatMood catMood;

    [Header("UI")]
    [SerializeField] private RectTransform iconContainer;
    [SerializeField] private GameObject iconPrefab;

    [Header("Update Rate")]
    [SerializeField] private float checkInterval = 0.5f;

    [Header("Thresholds")]
    [SerializeField] private float lowThreshold = 30f;
    [SerializeField] private float highThreshold = 75f;

    [Header("Emoji Sprites")]
    [SerializeField] private Sprite happySprite;
    [SerializeField] private Sprite devotedSprite;
    [SerializeField] private Sprite contentSprite;
    [SerializeField] private Sprite neutralSprite;
    [SerializeField] private Sprite hungrySprite;
    [SerializeField] private Sprite thirstySprite;
    [SerializeField] private Sprite tiredSprite;
    [SerializeField] private Sprite dirtySprite;
    [SerializeField] private Sprite angrySprite;
    [SerializeField] private Sprite sickSprite;

    private enum NeedSlot { Hungry, Thirsty, Tired, Dirty, Mood }

    private readonly Dictionary<NeedSlot, GameObject> _activeSlots = new();
    private float _nextCheckTime;

    private void Awake()
    {
        if (petStats == null)
            petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
        if (catMood == null)
            catMood = FindAnyObjectByType<CatMood>();
    }

    private void Update()
    {
        if (petStats == null)
            petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();

        if (Time.time >= _nextCheckTime)
        {
            _nextCheckTime = Time.time + checkInterval;
            RefreshHUD();
        }
    }

    private void RefreshHUD()
    {
        if (petStats == null) return;

        // Each need slot is shown or hidden based on whether that stat is still low.
        // Icons only disappear once the stat recovers above the threshold.
        UpdateSlot(NeedSlot.Hungry, petStats.Hunger < lowThreshold, hungrySprite);
        UpdateSlot(NeedSlot.Thirsty, petStats.Thirst < lowThreshold, thirstySprite);
        UpdateSlot(NeedSlot.Tired, petStats.Energy < lowThreshold, tiredSprite);
        UpdateSlot(NeedSlot.Dirty, petStats.Hygiene < lowThreshold, dirtySprite);

        // Mood slot — shows the overall emotional state
        Sprite moodSprite = DetermineMoodSprite();
        UpdateSlot(NeedSlot.Mood, true, moodSprite);
    }

    private Sprite DetermineMoodSprite()
    {
        if (catMood != null && catMood.IsWary) return angrySprite;
        if (petStats.Happiness < lowThreshold) return sickSprite;
        if (petStats.Happiness >= highThreshold)
        {
            if (catMood != null && catMood.IsDevoted) return devotedSprite;
            return happySprite;
        }
        if (petStats.Happiness >= lowThreshold * 1.5f) return contentSprite;
        return neutralSprite;
    }

    private void UpdateSlot(NeedSlot slot, bool shouldShow, Sprite sprite)
    {
        if (sprite == null) return;

        if (shouldShow)
        {
            if (!_activeSlots.ContainsKey(slot) || _activeSlots[slot] == null)
            {
                // Spawn new icon
                var go = Instantiate(iconPrefab, iconContainer);
                var img = go.GetComponent<Image>();
                if (img == null) img = go.GetComponentInChildren<Image>();
                if (img != null) img.sprite = sprite;
                _activeSlots[slot] = go;
            }
            else
            {
                // Update sprite in case mood changed
                var img = _activeSlots[slot].GetComponent<Image>();
                if (img == null) img = _activeSlots[slot].GetComponentInChildren<Image>();
                if (img != null) img.sprite = sprite;
            }
        }
        else
        {
            if (_activeSlots.TryGetValue(slot, out var go) && go != null)
            {
                Destroy(go);
                _activeSlots.Remove(slot);
            }
        }
    }
}