using UnityEngine;

public class MovementSettings : MonoBehaviour
{
    [SerializeField] private float currentSpeed = 10f; //default

    public float CurrentSpeed => currentSpeed;

    public void SetSpeed(float newSpeed)
    {
        currentSpeed = newSpeed;
    }
}
