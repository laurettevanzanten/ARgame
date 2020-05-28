using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Script used for the basket, which will collect the items put in the basket
/// </summary>
public class ItemConsumer : MonoBehaviour
{
    public string targetTag = "Item";
    private GameStateComponent gameState;

    public void Start()
    {
        gameState = GameStateComponent.Instance;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        TryToConsume(collision.gameObject);
    }

    public void OnCollisionStay2D(Collision2D collision)
    {
        TryToConsume(collision.gameObject);
    }

    private void TryToConsume(GameObject colliderObject)
    {
        if ((gameState == null || gameState.IsGameActive) && colliderObject.tag == targetTag)
        {
            var jointControl = colliderObject.GetComponent<TargetJointControl>();
            var itemBehaviour = colliderObject.GetComponent<ItemBehaviour>();

            if (itemBehaviour != null && jointControl != null && !jointControl.IsTrackingMouse)
            {
                gameState?.OnItemCollected(itemBehaviour);
            }

            if (jointControl != null && !jointControl.IsTrackingMouse)
            {
                GameObject.Destroy(colliderObject);
            }
        }
    }
}
