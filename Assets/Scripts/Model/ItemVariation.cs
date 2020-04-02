using System;
using UnityEngine;

[Serializable]
public class ItemVariation
{
    public Color color;
    public string shortName;
    public string fullName;
    public GameObject prefab;
    public string prefabChildOverlay = "Overlay";

    public GameObject Instantiate()
    {
        var result = GameObject.Instantiate(prefab);
        var colorOverlay = result.transform.Find(prefabChildOverlay);
        var overlayRenderer = colorOverlay.GetComponent<SpriteRenderer>();
        overlayRenderer.color = color;
        result.name = fullName;
        return result;
    }
}

