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

    public bool showAllOrderLines = true;
    public string headerFormat = "Orderline ({0}/{1})";


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

            if (showAllOrderLines)
            {
                for (int i = 0; i < orderList.Count; ++i)
                {
                    var itemColor = i == gameState.CurrentOrderLine ? selectedItemOrderColor : itemOrderColor;
                    BuildOrderLineText(orderList[i], builder, gameState, itemColor);
                }
            }
            else
            {
                BuildOrderLineText(orderList[gameState.CurrentOrderLine], builder, gameState, itemOrderColor);
            }

            var headerText = string.Format(headerFormat, gameState.CurrentOrderLine+1, orderList.Count);

            textComponent.text = 
                "<color=" + headerColor + "><size=" + headerFontSize + ">" 
                    + "<u>" + headerText + "</u></size>\n"
                + "<size=" + itemFontSize + ">" 
                    + builder.ToString() 
                + "</size>";
        }
    }

    private void BuildOrderLineText(OrderProperties orderProperties, StringBuilder builder, GameStateComponent gameState, string itemColor)
    {
        var item = gameState.ResolveItemName(orderProperties);

        builder.Append("<color=");
        builder.Append(itemColor);
        builder.Append(">");

        builder.Append(orderProperties.ToString(item));

        builder.Append("\n");
    }
}
