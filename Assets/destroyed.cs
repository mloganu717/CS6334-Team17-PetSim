using UnityEngine;

public class destroyobject : MonoBehaviour
{
    private bool looking = false;

    public static GameObject deleted = null;

    public void OnHighlight()
    {
        looking = true;
    }

    public void NotHighlighted()
    {
        looking = false;
    }

    void Update()
    {
        if (looking && Input.GetButtonDown("js0"))
        {
            gameObject.SetActive(false);
            deleted = gameObject;
        }
    }
}