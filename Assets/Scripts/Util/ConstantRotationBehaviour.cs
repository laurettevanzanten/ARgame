using UnityEngine;

public class ConstantRotationBehaviour : MonoBehaviour
{
    public float rotationPerSecond = 45;
    public Vector3 axis = Vector3.forward;

    void Update()
    {
        transform.rotation *= Quaternion.AngleAxis(rotationPerSecond * Time.deltaTime, axis);
    }
}
