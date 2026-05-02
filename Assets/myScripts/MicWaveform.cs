using UnityEngine;
using UnityEngine.UI;

public class MicWaveform : MonoBehaviour
{
    public enum WaveformState { Idle, PlayerSpeaking, VetSpeaking }

    [Header("Bar References — assign all 7 in Inspector")]
    public RectTransform[] bars;

    [Header("Settings")]
    public float minHeight = 6f;
    public float maxHeightPlayer = 36f;
    public float maxHeightVet = 20f;
    public float playerSpeed = 0.7f;
    public float vetSpeed = 1.1f;
    public float staggerDelay = 0.1f;

    [Header("Colors")]
    public Color idleColor = new Color(0.27f, 0.27f, 0.27f);
    public Color playerColor = new Color(0.11f, 0.62f, 0.46f);  // #1D9E75
    public Color vetColor = new Color(0.22f, 0.54f, 0.87f);  // #378ADD

    private WaveformState _state = WaveformState.Idle;
    private Image[] _images;

    void Awake()
    {
        _images = new Image[bars.Length];
        for (int i = 0; i < bars.Length; i++)
            _images[i] = bars[i].GetComponent<Image>();
    }

    void Update()
    {
        if (_state == WaveformState.Idle)
        {
            SetAllBarsHeight(minHeight);
            return;
        }

        float speed = _state == WaveformState.PlayerSpeaking ? playerSpeed : vetSpeed;
        float maxH = _state == WaveformState.PlayerSpeaking ? maxHeightPlayer : maxHeightVet;

        for (int i = 0; i < bars.Length; i++)
        {
            float offset = i * staggerDelay;
            float t = (Mathf.Sin((Time.time / speed + offset) * Mathf.PI * 2f) + 1f) / 2f;
            float h = Mathf.Lerp(minHeight, maxH, t);
            bars[i].sizeDelta = new Vector2(bars[i].sizeDelta.x, h);
        }
    }

    public void SetState(WaveformState newState)
    {
        _state = newState;
        Color c = newState switch
        {
            WaveformState.PlayerSpeaking => playerColor,
            WaveformState.VetSpeaking => vetColor,
            _ => idleColor
        };
        foreach (var img in _images) img.color = c;
    }

    private void SetAllBarsHeight(float h)
    {
        foreach (var bar in bars)
            bar.sizeDelta = new Vector2(bar.sizeDelta.x, h);
    }
}