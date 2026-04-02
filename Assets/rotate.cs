using UnityEngine;

public class rotatecube2 : MonoBehaviour
{
    private bool highlighted = false;
    public float speed = 90.0f;

    public void OnHighlight()
    {
        highlighted = true;
    }

    public void NotHighlighted()
    {
        highlighted = false;
    }

    void Update()
    {
        if (highlighted && Input.GetButton("js1"))
            transform.Rotate(Vector3.up * speed * Time.deltaTime);
    }
}