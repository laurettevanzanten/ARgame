using UnityEngine;

public class OrderLineIndicator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private int orderLineIndex = -1;
    private int orderIndex = 0;

    public void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();       
    }

    public void Update()
    {
        var gameState = GameStateComponent.Instance;

        if (orderLineIndex != gameState.CurrentOrderLine || orderIndex != gameState.CurrentOrderListIndex)
        {
            orderLineIndex = gameState.CurrentOrderLine;
            orderIndex = gameState.CurrentOrderListIndex;

            if (orderLineIndex == -1)
            {
                spriteRenderer.enabled = false;
            }
            else
            {
                var order = gameState.CurrentOrderList[orderLineIndex];
                var boxObject = gameState.ResolveBoxObject(order);

                spriteRenderer.enabled = true;
                transform.position = boxObject.transform.position; 
            }
        }
    }
}
