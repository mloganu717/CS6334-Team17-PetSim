using UnityEngine;

public class PhoneInteractable : MonoBehaviour
{
    [Header("Drag your Phone UI panel here")]
    public GameObject phoneUI;          

    private bool _isOpen = false;

    void Start()
    {
        phoneUI.SetActive(false);       // hidden by default
    }

    // Interact button pressed
    public void OnInteract()
    {
        _isOpen = !_isOpen;
        phoneUI.SetActive(_isOpen);
    }

    // Close with Escape key
    void Update()
    {
        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            _isOpen = false;
            phoneUI.SetActive(false);
        }
    }
}