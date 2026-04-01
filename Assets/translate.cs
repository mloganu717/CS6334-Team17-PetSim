using UnityEngine;

public class translatecube1 : MonoBehaviour
{
    private bool highlighted = false;
    public float speed = 1.0f;

    public void OnHighlight()
    {
        highlighted = true;
    }

    public void NotHighlighted()
    {
        highlighted=false;
    }

    void Update()
    {
        if (highlighted && Input.GetButton("js1"))
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}