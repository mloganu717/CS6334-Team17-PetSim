using System.Collections;
using UnityEngine;
using TMPro;

// Displays a floating emoji/symbol above the cat that reflects its current mood
// and stat levels. Attach this to the cat GameObject.
//
// Setup in the Inspector:
//   1. Create a child GameObject on the cat, position it ~1.5 units above the head
//   2. Add a TMP_Text component to it set to World Space
//   3. Assign that TMP_Text to the displayText field below
//
// The display picks the highest priority active mood and shows it.
// Priority order: Critical (dying) > Sick > Angry > Hungry > Thirsty > Tired >
//                 Dirty > Happy > Content > Neutral

public class CatMoodDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private PetStats petStats;
    [SerializeField] private CatMood catMood;

    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.8f, 0f);

    [Header("Update Rate")]
    [SerializeField] private float checkInterval = 0.5f;

    [Header("Animation")]
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float fadeInTime = 0.3f;
    [SerializeField] private float holdTime = 2.5f;
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("Stat Thresholds")]
    [SerializeField] private float criticalThreshold = 10f;
    [SerializeField] private float lowThreshold = 25f;
    [SerializeField] private float happyThreshold = 75f;
    [SerializeField] private float devotedThreshold = 90f;

    private enum MoodIcon
    {
        None,
        Neutral,    // 😐
        Content,    // 😊
        Happy,      // 😄
        Devoted,    // 😻
        Hungry,     // 🍽
        Thirsty,    // 💧
        Tired,      // 💤
        Dirty,      // 🤢  (low hygiene)
        Angry,      // 😾
        Sick,       // 🤮  (multiple stats critical)
        Critical    // ❗
    }

    // Maps each icon to a display string. Use plain text symbols if emoji
    // don't render in your font — swap these out freely.
    private static readonly System.Collections.Generic.Dictionary<MoodIcon, string> IconMap
        = new()
    {
        { MoodIcon.None,     ""    },
        { MoodIcon.Neutral,  "😐"  },
        { MoodIcon.Content,  "😊"  },
        { MoodIcon.Happy,    "😄"  },
        { MoodIcon.Devoted,  "😻"  },
        { MoodIcon.Hungry,   "🍽"  },
        { MoodIcon.Thirsty,  "💧"  },
        { MoodIcon.Tired,    "💤"  },
        { MoodIcon.Dirty,    "🤢"  },
        { MoodIcon.Angry,    "😾"  },
        { MoodIcon.Sick,     "🤮"  },
        { MoodIcon.Critical, "❗"  },
    };

    private MoodIcon _currentIcon = MoodIcon.None;
    private MoodIcon _displayedIcon = MoodIcon.None;
    private float _nextCheckTime;
    private float _bobOffset;
    private Coroutine _animCoroutine;
    private Vector3 _baseLocalPosition;

    private void Awake()
    {
        if (petStats == null)
            petStats = PetStats.Instance != null ? PetStats.Instance : FindAnyObjectByType<PetStats>();
        if (catMood == null)
            catMood = GetComponent<CatMood>();

        if (displayText != null)
        {
            _baseLocalPosition = offset;
            displayText.transform.localPosition = offset;
            SetAlpha(0f);
        }
    }

    private void Update()
    {
        if (Time.time >= _nextCheckTime)
        {
            _nextCheckTime = Time.time + checkInterval;
            EvaluateMood();
        }

        // Bob the icon up and down while visible
        if (displayText != null && displayText.color.a > 0.01f)
        {
            _bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            displayText.transform.localPosition = _baseLocalPosition + Vector3.up * _bobOffset;

            // Always face the camera
            if (Camera.main != null)
            {
                Vector3 dir = displayText.transform.position - Camera.main.transform.position;
                displayText.transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    private void EvaluateMood()
    {
        if (petStats == null) return;

        MoodIcon next = DetermineIcon();

        if (next == _displayedIcon) return;

        _displayedIcon = next;

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        if (next == MoodIcon.None)
            _animCoroutine = StartCoroutine(FadeOut());
        else
            _animCoroutine = StartCoroutine(ShowIcon(IconMap[next], IconColor(next)));
    }

    private MoodIcon DetermineIcon()
    {
        if (petStats == null) return MoodIcon.None;

        int criticalCount = 0;
        if (petStats.Hunger < criticalThreshold) criticalCount++;
        if (petStats.Thirst < criticalThreshold) criticalCount++;
        if (petStats.Energy < criticalThreshold) criticalCount++;
        if (petStats.Hygiene < criticalThreshold) criticalCount++;
        if (petStats.Happiness < criticalThreshold) criticalCount++;

        if (criticalCount >= 3) return MoodIcon.Critical;
        if (criticalCount == 2) return MoodIcon.Sick;

        if (catMood != null && catMood.IsWary) return MoodIcon.Angry;

        if (petStats.Hunger < lowThreshold) return MoodIcon.Hungry;
        if (petStats.Thirst < lowThreshold) return MoodIcon.Thirsty;
        if (petStats.Energy < lowThreshold) return MoodIcon.Tired;
        if (petStats.Hygiene < lowThreshold) return MoodIcon.Dirty;

        if (catMood != null)
        {
            if (catMood.Affinity >= catMood.purringThreshold &&
                petStats.Happiness >= devotedThreshold) return MoodIcon.Devoted;

            if (catMood.IsFriendly &&
                petStats.Happiness >= happyThreshold) return MoodIcon.Happy;

            if (catMood.IsNeutral) return MoodIcon.Content;
        }

        return MoodIcon.Neutral;
    }

    private Color IconColor(MoodIcon icon)
    {
        return icon switch
        {
            MoodIcon.Critical or MoodIcon.Sick => new Color(1f, 0.2f, 0.2f),
            MoodIcon.Angry => new Color(1f, 0.35f, 0.1f),
            MoodIcon.Hungry or MoodIcon.Thirsty => new Color(1f, 0.85f, 0.2f),
            MoodIcon.Tired or MoodIcon.Dirty => new Color(0.6f, 0.8f, 0.6f),
            MoodIcon.Happy or MoodIcon.Devoted => new Color(0.4f, 1f, 0.6f),
            _ => Color.white,
        };
    }

    private IEnumerator ShowIcon(string symbol, Color color)
    {
        if (displayText == null) yield break;

        displayText.text = symbol;
        displayText.color = new Color(color.r, color.g, color.b, 0f);

        // Fade in
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeInTime;
            SetAlpha(Mathf.Clamp01(t));
            yield return null;
        }

        // Hold
        yield return new WaitForSeconds(holdTime);

        // If the mood hasn't changed, fade out and re-evaluate next cycle
        yield return StartCoroutine(FadeOut());
        _displayedIcon = MoodIcon.None;
    }

    private IEnumerator FadeOut()
    {
        if (displayText == null) yield break;

        float startAlpha = displayText.color.a;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeOutTime;
            SetAlpha(Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t)));
            yield return null;
        }
        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        if (displayText == null) return;
        Color c = displayText.color;
        c.a = alpha;
        displayText.color = c;
    }
}