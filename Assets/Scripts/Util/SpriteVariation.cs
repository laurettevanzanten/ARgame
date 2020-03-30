using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteVariation : MonoBehaviour
{
    public Sprite[] spriteOptions;
    public Color[] colorOptions;

    private SpriteRenderer spriteRenderer;

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            if (spriteOptions.Length > 0)
            {
                spriteRenderer.sprite = spriteOptions[Random.Range(0, spriteOptions.Length)];
            }

            if (colorOptions.Length > 0)
            {
                spriteRenderer.color = colorOptions[Random.Range(0, spriteOptions.Length)];
            }
        }
    }
}
