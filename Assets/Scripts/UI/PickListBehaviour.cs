using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class PickListBehaviour : MonoBehaviour
{
    public int headerFontSize = 32;
    public int itemFontSize = 24;

    public string headerColor = "#000000";
    public string selectedItemOrderColor = "#42EDFF";
    public string itemOrderColor = "#A0A0A0";


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
    public void OnShow()
    {
        gameObject.SetActive(true);
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


                builder.Append("<color=");
                builder.Append(i == gameState.CurrentOrderLine ? selectedItemOrderColor : itemOrderColor);
                builder.Append(">");

                builder.Append(orderProperties.ToString(item));

                builder.Append("\n");
            }

            textComponent.text = "<color=" + headerColor + "><size=" + headerFontSize + "><u>Pick list:</u></size>\n"
                + "<size=" + itemFontSize + ">" + builder.ToString() + "</size>";

        }
    }
}
