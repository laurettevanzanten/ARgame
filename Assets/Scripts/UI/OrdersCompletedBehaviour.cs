using UnityEngine;
using TMPro;

public class OrdersCompletedBehaviour : MonoBehaviour
{
    public string textFormat = "Completed orders: {0}";

    private int completedOrders = 0;
    private TextMeshProUGUI textComponent;

    public void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        textComponent.text = string.Format(textFormat, completedOrders);
    }

    public void Update()
    {
        if (GameStateComponent.Instance.CompletedOrders != completedOrders)
        {
            completedOrders = GameStateComponent.Instance.CompletedOrders;
            textComponent.text = string.Format(textFormat, completedOrders);
        }
    }
}
