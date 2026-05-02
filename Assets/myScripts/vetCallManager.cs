using System.Collections;
using UnityEngine;
using TMPro;

public class VetCallManager : MonoBehaviour
{
    [Header("Screens — drag from hierarchy")]
    public GameObject screenCall;         // the "Call" child panel (home screen)
    public GameObject screenConnecting;   // "Connecting"
    public GameObject screenConnected;    // "Connected"
    public GameObject screenEnded;        // "ended"

    [Header("Connected Panel")]
    public TMP_Text vetResponseText;      // AI TEXT BOX
    public TMP_Text timerText;            // time
    public MicWaveform waveform;          // WaveformContainer

    [Header("Ended Panel")]
    public TMP_Text durationText;
    public TMP_Text summaryText;

    private float _callTime = 0f;
    private bool _isMuted = false;
    private MicrophoneRecorder _recorder;
    private VetAPIClient _apiClient;

    void Awake()
    {
        _recorder = GetComponent<MicrophoneRecorder>();
        _apiClient = GetComponent<VetAPIClient>();
    }

    void Start()
    {
        // Show only the Call home screen by default
        ShowOnly(screenCall);
    }

    void Update()
    {
        if (screenConnected.activeSelf)
        {
            _callTime += Time.deltaTime;
            int m = Mathf.FloorToInt(_callTime / 60f);
            int s = Mathf.FloorToInt(_callTime % 60f);
            timerText.text = $"{m}:{s:00}";
        }
    }

    //  called by your HomeScreen "Call" button 
    public void OnCallButtonPressed()
    {
        _callTime = 0f;
        ShowOnly(screenConnecting);
        StartCoroutine(ConnectToVet());
    }

    private IEnumerator ConnectToVet()
    {
        yield return new WaitForSeconds(1.5f);
        ShowOnly(screenConnected);
        vetResponseText.text = "Hello! How can I help your pet today?";
        waveform.SetState(MicWaveform.WaveformState.Idle);
        _recorder.StartListening(OnPlayerSpeechReady);
    }

    private void OnPlayerSpeechReady(string transcribedText)
    {
        if (!screenConnected.activeSelf || _isMuted) return;
        waveform.SetState(MicWaveform.WaveformState.VetSpeaking);
        StartCoroutine(_apiClient.SendToVet(transcribedText, OnVetResponse));
    }

    private void OnVetResponse(string response)
    {
        vetResponseText.text = response;
        waveform.SetState(MicWaveform.WaveformState.Idle);
        _recorder.StartListening(OnPlayerSpeechReady);
    }

    //  button callbacks 
    public void OnMutePressed()
    {
        _isMuted = !_isMuted;
        if (_isMuted)
        {
            _recorder.StopListening();
            waveform.SetState(MicWaveform.WaveformState.Idle);
        }
        else
        {
            _recorder.StartListening(OnPlayerSpeechReady);
            waveform.SetState(MicWaveform.WaveformState.PlayerSpeaking);
        }
    }

    public void OnEndCallPressed()
    {
        _recorder.StopListening();
        waveform.SetState(MicWaveform.WaveformState.Idle);

        int m = Mathf.FloorToInt(_callTime / 60f);
        int s = Mathf.FloorToInt(_callTime % 60f);
        durationText.text = $"Duration: {m}:{s:00}";
        summaryText.text = "Tap 'Call again' to reconnect.";

        ShowOnly(screenEnded);
    }

    public void OnCallAgainPressed() => OnCallButtonPressed();
    public void OnClosePressed() => ShowOnly(screenCall);

    //  helper 
    private void ShowOnly(GameObject target)
    {
        screenCall.SetActive(target == screenCall);
        screenConnecting.SetActive(target == screenConnecting);
        screenConnected.SetActive(target == screenConnected);
        screenEnded.SetActive(target == screenEnded);
    }
}