using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Hovers a single emoji above the cat when a mood event fires.
// Shows once per trigger then disappears — does not loop.
//
// Setup:
//   1. Create a child GameObject on the cat named "MoodPopup"
//   2. Add a Canvas component set to World Space
//   3. Add a child Image to that canvas
//   4. Assign that Image to the displayImage field below
//   5. Set the canvas width/height to something like 0.5 x 0.5
//   6. Position it ~1.8 units above the cat's head

public class CatMoodDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image displayImage;
    [SerializeField] private PetStats petStats;
    [SerializeField] private CatMood catMood;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.8f, 0f);

    [Header("Animation")]
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float holdTime = 2f;
    [SerializeField] private float fadeOutTime = 0.5f;
    [SerializeField] private float floatHeight = 0.3f;

    [Header("Trigger Thresholds")]
    [SerializeField] private float lowThreshold = 30f;
    [SerializeField] private float highThreshold = 75f;

    [Header("Emoji Sprites")]
    [SerializeField] private Sprite happySprite;
    [SerializeField] private Sprite devotedSprite;
    [SerializeField] private Sprite contentSprite;
    [SerializeField] private Sprite hungrySprite;
    [SerializeField] private Sprite thirstySprite;
    [SerializeField] private Sprite tiredSprite;
    [SerializeField] private Sprite dirtySprite;
    [SerializeField] private Sprite angrySprite;
    [SerializeField] private Sprite sickSprite;
    [SerializeField] private Sprite needySprite;

    private Coroutine _showCoroutine;

    private float _lastHunger;
    private float _lastThirst;
    private float _lastEnergy;
    private float _lastHygiene;
    private float _lastHappiness;

    private void Awake()
    {
        if (petStats == null)
            petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
        if (catMood == null)
            catMood = GetComponent<CatMood>();

        if (displayImage != null)
        {
            displayImage.transform.localPosition = offset;
            SetAlpha(0f);
        }
    }

    private void Start()
    {
        if (petStats != null)
        {
            _lastHunger = petStats.Hunger;
            _lastThirst = petStats.Thirst;
            _lastEnergy = petStats.Energy;
            _lastHygiene = petStats.Hygiene;
            _lastHappiness = petStats.Happiness;
        }
    }

    private void Update()
    {
        if (petStats == null)
        {
            petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
            return;
        }

        CheckThresholdCrossings();

        if (displayImage != null && Camera.main != null)
        {
            displayImage.transform.localPosition = offset;
            Vector3 dir = displayImage.transform.position - Camera.main.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                displayImage.transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void CheckThresholdCrossings()
    {
        // Happiness went up noticeably — show positive reaction
        if (petStats.Happiness > _lastHappiness + 5f)
        {
            if (petStats.Happiness >= highThreshold)
                TriggerPopup(catMood != null && catMood.IsDevoted ? devotedSprite : happySprite);
            else
                TriggerPopup(contentSprite);
        }

        // Stats crossing low threshold going downward — fire once on crossing
        if (petStats.Hunger < lowThreshold && _lastHunger >= lowThreshold) TriggerPopup(hungrySprite);
        if (petStats.Thirst < lowThreshold && _lastThirst >= lowThreshold) TriggerPopup(thirstySprite);
        if (petStats.Energy < lowThreshold && _lastEnergy >= lowThreshold) TriggerPopup(tiredSprite);
        if (petStats.Hygiene < lowThreshold && _lastHygiene >= lowThreshold) TriggerPopup(dirtySprite);

        if (petStats.Happiness < lowThreshold && _lastHappiness >= lowThreshold)
            TriggerPopup(catMood != null && catMood.IsWary ? angrySprite : sickSprite);

        _lastHunger = petStats.Hunger;
        _lastThirst = petStats.Thirst;
        _lastEnergy = petStats.Energy;
        _lastHygiene = petStats.Hygiene;
        _lastHappiness = petStats.Happiness;
    }

    public void TriggerPopup(Sprite sprite)
    {
        if (sprite == null) return;

        if (_showCoroutine != null)
            StopCoroutine(_showCoroutine);

        _showCoroutine = StartCoroutine(ShowPopup(sprite));
    }

    public void TriggerNeedy() => TriggerPopup(needySprite);
    public void TriggerHappy() => TriggerPopup(happySprite);

    private IEnumerator ShowPopup(Sprite sprite)
    {
        if (displayImage == null) yield break;

        displayImage.sprite = sprite;

        Vector3 startPos = offset;
        Vector3 endPos = offset + Vector3.up * floatHeight;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeInTime;
            float c = Mathf.Clamp01(t);
            SetAlpha(c);
            displayImage.transform.localPosition = Vector3.Lerp(startPos, endPos, c);
            yield return null;
        }

        yield return new WaitForSeconds(holdTime);

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeOutTime;
            SetAlpha(Mathf.Lerp(1f, 0f, Mathf.Clamp01(t)));
            yield return null;
        }

        SetAlpha(0f);
        displayImage.transform.localPosition = offset;
    }

    private void SetAlpha(float alpha)
    {
        if (displayImage == null) return;
        Color c = displayImage.color;
        c.a = alpha;
        displayImage.color = c;
    }
}