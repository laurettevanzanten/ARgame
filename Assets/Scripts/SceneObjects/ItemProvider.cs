using UnityEngine;

/// <summary>
/// Creates an item for the player to drag around
/// </summary>
public class ItemProvider : MonoBehaviour
{
    // not used atm
    public int maxItems = -1;
    public GameObject itemPrefab;
    public float itemSpawnTime = 0.1f;

    private bool isMouseDown;
    private float mouseDownTime = 0;
    private bool isMouseOnProvider = false;

    // Start is called before the first frame update
    void Start()
    {
        isMouseDown = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isMouseDown && isMouseOnProvider)
        {
            isMouseDown = true;
            mouseDownTime = Time.time;
        }

        if (Input.GetMouseButtonUp(0) && isMouseDown)
        {
            isMouseDown = false;
            isMouseOnProvider = false;
        }

        if (!Input.GetMouseButtonUp(0) && isMouseDown && isMouseOnProvider)
        {
            if (Time.time - mouseDownTime > itemSpawnTime)
            {
                var spawnedItem = GameObject.Instantiate(itemPrefab);
                var worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                worldPosition.z = gameObject.transform.position.z;

                spawnedItem.transform.position = worldPosition;
                

                isMouseDown = false;
            }
        }
    }

    public void OnMouseExit()
    {
        isMouseDown = false;
        mouseDownTime = 0;
        isMouseOnProvider = false;
    }

    public void OnMouseEnter()
    {
        isMouseOnProvider = true;
    }
}
