using UnityEngine;

/// <summary>
/// Displays a highlight when moving the mouse over the collision object associated with the game object
/// </summary>
public class HighlightControl : MonoBehaviour
{
    public GameObject hightLightObject;

    // Start is called before the first frame update
    void Start()
    {
        if (hightLightObject != null)
        {
            hightLightObject.SetActive(false);
        }
    }
    
    public void OnMouseEnter()
    {
        if (this.enabled && hightLightObject != null)
        {
            hightLightObject.SetActive(true);
        }
    }

    public void OnMouseExit()
    {
        if (this.enabled && hightLightObject != null)
        {
            hightLightObject.SetActive(false);
        }
    }
}
