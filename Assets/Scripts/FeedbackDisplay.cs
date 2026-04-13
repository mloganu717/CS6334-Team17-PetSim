using System.Collections;
using UnityEngine;
using TMPro;

// displays feedback messages
public class FeedbackDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float displayDuration = 3f;

    private Coroutine hideCoroutine;

    private void Start()
    {
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);

        //petstats feedback event
        if (PetStats.Instance != null)
            PetStats.Instance.OnFeedback.AddListener(ShowMessage);
    }

    private void OnDestroy()
    {
        if (PetStats.Instance != null)
            PetStats.Instance.OnFeedback.RemoveListener(ShowMessage);
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