using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public Button[] menuButtons; // start and quit buttons
    public string gameSceneName = "FinalScene";
    private int index = 0;
    private float timer = 0;

    void Start() {
        UpdateUI();
    }

    void Update() {
        timer += Time.deltaTime;
        float v = Input.GetAxis("Vertical");

        //move selection
        if (Mathf.Abs(v) > 0.5f && timer > 0.25f) {
            index = (v > 0) ? 0 : 1; // 0 for start, 1 for quit
            UpdateUI();
            timer = 0;
        }

        //click selection (Using JS5 / Button B)
        if (Input.GetButtonDown("js5") || Input.GetButtonDown("Submit")) {
            if (index == 0) StartGame();
            else QuitGame();
        }
    }

    void UpdateUI() {
        for (int i = 0; i < menuButtons.Length; i++) {
            menuButtons[i].image.color = (i == index) ? Color.yellow : Color.white;
            menuButtons[i].transform.localScale = (i == index) ? Vector3.one * 1.2f : Vector3.one;
        }
    }

    public void StartGame() { SceneManager.LoadScene(gameSceneName); }
    public void QuitGame() { Application.Quit(); }
}