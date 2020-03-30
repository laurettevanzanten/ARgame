using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class PickListBehaviour : MonoBehaviour
{
    private int orderListIndex = -1;
    private int orderLineIndex = -1;
    private TextMeshProUGUI textComponent;

    public void Start()
    {
        orderListIndex = GameStateComponent.Instance.CurrentOrderListIndex;
        textComponent = GetComponent<TextMeshProUGUI>();
        DisplayOrderList(GameStateComponent.Instance.CurrentOrderList);
    }

    public void Update()
    {
        var gameState = GameStateComponent.Instance;

        if (orderListIndex != gameState.CurrentOrderListIndex || orderLineIndex != gameState.CurrentOrderLine)
        {
            orderListIndex = gameState.CurrentOrderListIndex;
            orderLineIndex = gameState.CurrentOrderLine;
            DisplayOrderList(gameState.CurrentOrderList);
        }
    }

    public void DisplayOrderList(List<OrderProperties> orderList)
    {
        if (orderList == null)
        {
            textComponent.text = "No more orders";
        }
        else
        {
            var gameState = GameStateComponent.Instance;
            var builder = new StringBuilder();

            for (int i = 0; i < orderList.Count; ++i) 
            {
                var orderProperties = orderList[i];
                var item = gameState.ResolveItemName(orderProperties);

                if (i == gameState.CurrentOrderLine)
                {
                    builder.Append("<color=#42EDFF><u>");
                }
                else
                {
                    builder.Append("<color=#A0A0A0>");
                }

                builder.Append(orderProperties.ToString(item));

                if (i == gameState.CurrentOrderLine)
                {
                    builder.Append("</u>");
                }

                builder.Append("\n");
            }

            textComponent.text = "<size=32><u>Pick list:</u></size>\n"
                + "<size=24>" + builder.ToString() + "</size>";

        }
    }
}
