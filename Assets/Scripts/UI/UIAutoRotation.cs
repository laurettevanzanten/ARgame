using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAutoRotation : MonoBehaviour
{
    public float rotationPerSecond;

    public Vector3 axis = Vector3.forward;

    private RectTransform _uiTransform;


    void Start()
    {
        _uiTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        _uiTransform.rotation *= Quaternion.AngleAxis(rotationPerSecond * Time.deltaTime, axis);
    }
}
