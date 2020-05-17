using UnityEngine;

public class RackComponent : MonoBehaviour
{
    public int width = 1;
    public int height = 1;

    public Vector3 spacing = Vector3.one;
    public Vector3 spriteMinScale = Vector3.one;
    public Vector3 spriteMaxScale = Vector3.one;

    public GameObject boxPrefab;
    public string boxSpriteName = "Sprite-Box";
    public string labelName = "Label";

    public string labelFormat = "A{0}.{1}";
    public bool autoFillRack = false;

    public GameObject[][] BoxObjects { get; private set; }

    void Start()
    {
        if (autoFillRack)
        {
            FillRack(width, height);
        }
    }

    public GameObject[][] FillRack(int rackWidth, int rackHeight)
    { 
        var labelParameter1 = 1;

        BoxObjects = new GameObject[rackWidth][];

        var boxXIndex = 0;

        for (int x = 0; x < rackWidth; x++)
        {
            BoxObjects[boxXIndex] = new GameObject[rackHeight];

            var labelParameter2 = rackHeight;
            var boxYIndex = 0;

            for (int y = 0; y < rackHeight; y++)
            {
                var boxObject = Instantiate(boxPrefab);
                var boxSprite = boxObject.transform.Find(boxSpriteName).gameObject;
                var labelObject = boxObject.transform.Find(labelName).gameObject;
                var labelTextMesh = labelObject.GetComponent<TextMesh>();

                boxObject.transform.position = transform.position + new Vector3(spacing.x * x, spacing.y * (rackHeight - (y+1)), transform.position.z);
                boxObject.transform.parent = transform;
                boxSprite.transform.localScale = spriteMinScale + (spriteMaxScale - spriteMinScale) * Random.value;

                boxObject.name = string.Format(labelFormat, labelParameter2, labelParameter1);
                labelTextMesh.text = boxObject.name;

                BoxObjects[boxXIndex][boxYIndex] = boxObject;

                labelParameter2--;
                boxYIndex++;
            }

            labelParameter1++;
            boxXIndex++;
        }

        return BoxObjects;
    }
}
