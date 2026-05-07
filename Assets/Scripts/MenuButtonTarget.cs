using UnityEngine;
using UnityEngine.UI;

public class MenuButtonTarget : MonoBehaviour
{
    public enum MenuAction
    {
        Destroy,
        Store,
        Exit,
        Use,
        CallVet,
        OrderFood,
        Pet,
        ShowStats,
        Play,
        Drink,
        Eat,
        Sleep,

        PhoneHome,
        StartVetCall,
        EndVetCall,
        ClosePhone,

        VetOption0,
        VetOption1,
        VetOption2,

        StartVoiceRecording,
        StopVoiceRecording
    }

    [SerializeField] private MenuAction action;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private BoxCollider boxCollider;

    public MenuAction Action => action;

    private void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider>();

        if (boxCollider != null)
            boxCollider.isTrigger = true;

        UpdateBoxCollider();
        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        Color targetColor = hovered ? hoverColor : normalColor;
        if (buttonImage != null)
            buttonImage.color = targetColor;

        // var text = GetComponentInChildren<TMPro.TMP_Text>(); //bugs with text color
        // if (text != null)
        //     text.color = targetColor;
    }

    public void UpdateBoxCollider()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null || boxCollider == null)
            return;

        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(rt.rect.width, rt.rect.height, 10f);
    }

    private void OnDisable()
    {
        SetHovered(false);
    }
}
