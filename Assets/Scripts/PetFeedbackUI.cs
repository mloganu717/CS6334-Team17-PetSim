using System.Collections;
using TMPro;
using UnityEngine;

// pet feedback ui
public class PetFeedbackUI : MonoBehaviour
{
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private PetStats petStats;
    [SerializeField] private float displayDuration = 2.5f;

    private Coroutine hideCoroutine; // timer

    private void Awake()
    {
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (petStats != null)
            petStats.OnFeedback.AddListener(ShowFeedback);
    }

    private void OnDisable()
    {
        if (petStats != null)
            petStats.OnFeedback.RemoveListener(ShowFeedback);
    }

    public void ShowFeedback(string message)
    {
        if (feedbackText == null) return;

        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
        hideCoroutine = null;
    }
}
