using UnityEngine;

/// <summary>
/// Controls the physics of the dragged item
/// </summary>
public class TargetJointControl : MonoBehaviour
{
    private TargetJoint2D targetJoint;

    private bool isMouseOverTargetJoint = false;

    public bool IsTrackingMouse = true;

    // Start is called before the first frame update
    void Start()
    {
        targetJoint = GetComponent<TargetJoint2D>();   
    }

    // Update is called once per frame
    void Update()
    {
        var worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        targetJoint.target = new Vector2(worldPosition.x, worldPosition.y);

        if (Input.GetMouseButtonUp(0) && IsTrackingMouse)
        {
            targetJoint.enabled = false;
            IsTrackingMouse = false;
        }

        if (!IsTrackingMouse && isMouseOverTargetJoint && Input.GetMouseButtonDown(0))
        {
            IsTrackingMouse = true;
            targetJoint.enabled = true;
        }
    }

    public void OnMouseEnter()
    {
        isMouseOverTargetJoint = true;
    }

    public void OnMouseExit()
    {
        isMouseOverTargetJoint = false;
    }
}
