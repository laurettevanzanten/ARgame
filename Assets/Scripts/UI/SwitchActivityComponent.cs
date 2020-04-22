using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchActivityComponent : MonoBehaviour
{
    public void ToggleActivation()
    {
        gameObject.SetActive(!gameObject.active);
    }
 
}
