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
        if (colliderObject.tag == targetTag)
        {
            var jointControl = colliderObject.GetComponent<TargetJointControl>();

            if (jointControl != null && !jointControl.IsTrackingMouse)
            {
                GameObject.Destroy(colliderObject);
            }

            var itemBehaviour = colliderObject.GetComponent<ItemBehaviour>();

            if (itemBehaviour != null)
            {
                gameState.OnItemCollected(itemBehaviour.Label);
            }
        }
    }
}
