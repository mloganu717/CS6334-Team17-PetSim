using UnityEngine;

public class spawnobject : MonoBehaviour
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
            if (Physics.Raycast(ray, out RaycastHit intersect))
            {
                if (Input.GetButtonDown("js0") && destroyobject.deleted != null)
                {
                    GameObject spawnobject = destroyobject.deleted;
                    spawnobject.SetActive(true);
                    spawnobject.transform.position = intersect.point + new Vector3(0, 0.5f, 0);
                    destroyobject.deleted = null;
                }

                if (Input.GetButtonDown("js8"))
                {
                    character.position = new Vector3(intersect.point.x, character.position.y, intersect.point.z);
                    Physics.SyncTransforms(); // could not get teleport to work without this for some reason
                }
            }
        }
    }
}