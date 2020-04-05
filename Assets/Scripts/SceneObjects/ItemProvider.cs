using UnityEngine;

public enum ItemSourceType
{
    Prefab,
    None,
    Variation
}

/// <summary>
/// Creates an item for the player to drag around
/// </summary>
public class ItemProvider : MonoBehaviour
{
    // not used atm
    public int maxItems = -1;
    public ItemSourceType itemSource = ItemSourceType.Prefab;
    public GameObject itemPrefab;
    public ItemVariation itemVariation;
    public float itemSpawnTime = 0.1f;

    private bool isMouseDown;
    private float mouseDownTime = 0;
    private bool isMouseOnProvider = false;

    public Vector2Int OriginCoordinate { get; set; }

    void Start()
    {
        isMouseDown = false;
    }

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
                var spawnedItem = InstantiateItem();

                if (spawnedItem != null)
                {
                    var itemProperties = spawnedItem.GetComponent<ItemBehaviour>();
                    var worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                    worldPosition.z = gameObject.transform.position.z;

                    spawnedItem.transform.position = worldPosition;
                    itemProperties.OriginCoordinate = OriginCoordinate;
                }

                isMouseDown = false;
            }
        }
    }

    public GameObject InstantiateItem()
    {
        switch (itemSource)
        {
            case ItemSourceType.Variation:
                return itemVariation.Instantiate();
            case ItemSourceType.Prefab:
                return GameObject.Instantiate(itemPrefab);
        }

        return null;
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

    public string ItemName()
    {
        switch (itemSource)
        {
            case ItemSourceType.Variation:
                return itemVariation.fullName;
            case ItemSourceType.Prefab:
                return itemPrefab.name;
            case ItemSourceType.None:
                return "No item specified";
        }

        return "unknown item";
    }
}
