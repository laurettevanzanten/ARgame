using UnityEngine;

public class SwitchActivityComponent : MonoBehaviour
{
    public void ToggleActivation()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
 
}
