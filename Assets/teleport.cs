using UnityEngine;

public class teleport : MonoBehaviour
{
    private bool looking = false;
    public Transform character;

    public void OnLook()
    {
        looking = true;
    }

    public void OnLookaway()
    {
        looking = false;
    }

    void Update()
    {
        if (looking)
        {
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (Input.GetButtonDown("js8"))
                {
                    character.position = new Vector3(hit.point.x, character.position.y, hit.point.z);
                }
            }
        }
    }
}