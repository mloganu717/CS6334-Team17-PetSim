using System.Collections;
using UnityEngine;
using TMPro;

public class FeedbackDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float displayDuration = 3f;

    private Coroutine hideCoroutine;
    private PetStats _subscribedStats;

    private void OnEnable()
    {
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
        TrySubscribeFeedback();
    }

    private void LateUpdate()
    {
        TrySubscribeFeedback();
    }

    private void TrySubscribeFeedback()
    {
        if (_subscribedStats != null)
            return;

        PetStats p = PetStats.Instance ?? FindAnyObjectByType<PetStats>();
        if (p == null)
            return;

        _subscribedStats = p;
        _subscribedStats.OnFeedback.AddListener(ShowMessage);
    }

    private void OnDisable()
    {
        if (_subscribedStats != null)
        {
            _subscribedStats.OnFeedback.RemoveListener(ShowMessage);
            _subscribedStats = null;
        }
    }

    public void ShowMessage(string message)
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
    }
}
