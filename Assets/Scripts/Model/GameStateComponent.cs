using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class GameStateComponent : MonoBehaviour
{
    public static GameStateComponent Instance { get; set; }

    public float maxTimeSeconds = 15 * 60;

    public TextAsset orders;
    public TextAsset rackSetup;

    public ItemVariation[] itemVariations;

    public GameObject[] rackVariations;

    public int rackCount = 1;
    public Vector3 rackSpacing = Vector3.right* 5;
    public char rackLabelPrefix = 'A';

    public float TimeRemaining => Mathf.Max(0, maxTimeSeconds - (Time.time - startTime));

    public int CompletedOrders => CollectedItems.Count -1;
    public GameObject[] RackObjects { get; private set; }
    public List<List<string>> CollectedItems { get; private set; } = new List<List<string>>();

    public List<List<OrderProperties>> Orders { get; private set; }
    public int CurrentOrderListIndex { get; private set; } = 0;
    public List<OrderProperties> CurrentOrderList => CurrentOrderListIndex < Orders.Count ? Orders[CurrentOrderListIndex] : null;

    public int CurrentOrderLine { get; private set; } = 0;

    private float startTime;

    public void Start()
    {
        Instance = this;
        startTime = Time.time;

        Orders = CSVParser.ParseGrid(orders, (value, x, y) => new OrderProperties(value));

        if (rackSetup == null)
        {
            CreateRandomRacks();
        }
        else
        {
            SetupRacks(CSVParser.ParseGrid(rackSetup, (value, x, y) => value));
        }

        CollectedItems.Add(new List<string>());
    }
    

    public GameObject ResolveBoxObject(OrderProperties properties)
    {
        return ResolveBoxObject(properties.rackLetter - rackLabelPrefix, properties.x, properties.y);
    }

    public GameObject ResolveBoxObject(int rackId, int x, int y)
    {
        var rack = RackObjects[rackId];
        var rackComponent = rack.GetComponent<RackComponent>();
        return  rackComponent.BoxObjects[x][y];
    }

    public string ResolveItemName(int rackId, int x, int y)
    {
        var boxObject = ResolveBoxObject(rackId, x, y);
        var itemProvider = boxObject.GetComponent<ItemProvider>();

        return itemProvider.ItemName();
    }

    public string ResolveItemName(OrderProperties properties)
    {
        return ResolveItemName(properties.rackLetter - rackLabelPrefix, properties.x, properties.y);
    }

    public void OnItemCollected(string itemLabel)
    {
        if (CurrentOrderListIndex < CollectedItems.Count)
        {
            CollectedItems[CurrentOrderListIndex].Add(itemLabel);
        }
        else
        {
            Debug.Log("item " + itemLabel + " went into the void...");
        }
    }

    public void OnOrderCompleted()
    {
        if (CurrentOrderListIndex < Orders.Count - 1)
        {
            CollectedItems.Add(new List<string>());
            CurrentOrderLine = 0;
        }
        else
        {
            CurrentOrderLine = -1;
            Debug.Log("No more orders to collect...");
        }

        CurrentOrderListIndex++;
    }

    public void OnNextOrderLine()
    {
        if (CurrentOrderList == null)
        {
            CurrentOrderLine = -1;
        }
        else
        {
            if (CurrentOrderLine < CurrentOrderList.Count - 1)
            {
                CurrentOrderLine++;
            }
            else
            {
                CurrentOrderLine = 0;
            }
        }
    }

    private string ResolveItemVaration(List<List<string>> grid, int x, int y)
    {
        if (grid.Count > y )
        {
            var row = grid[y];

            if (row.Count > x)
            {
                return row[x];
            }
        }

        return "";
    }

    private void SetupRacks(List<List<string>> grid)
    {
        RackObjects = new GameObject[rackCount];

        var basePosition = transform.position - (rackCount / 2) * rackSpacing;
        var rackLabel = char.ToUpper(rackLabelPrefix);

        for (int i = 0; i < rackCount; i++)
        {
            var rackObj = Instantiate(rackVariations[UnityEngine.Random.Range(0, rackVariations.Length)]);
            var rackComponent = rackObj.GetComponent<RackComponent>();

            rackObj.transform.parent = transform;
            rackObj.transform.position = basePosition + i * rackSpacing;

            rackObj.name = "Rack-" + rackLabel;

            rackComponent.labelFormat = rackLabel + "{0}.{1}";

            var boxObjects = rackComponent.FillRack(rackComponent.width, rackComponent.height);

            for (int x = 0; x < rackComponent.width; x++)
            {
                for (int y = 0; y < rackComponent.height; y++)
                {
                    var provider = boxObjects[x][y].GetComponent<ItemProvider>();
                    var itemName = ResolveItemVaration(grid, x + rackComponent.width * i, y);

                    if (string.IsNullOrEmpty(itemName))
                    {
                        provider.itemSource = ItemSourceType.None;
                    }
                    else
                    {
                        var itemVaration = itemVariations.FirstOrDefault(variation => variation.shortName == itemName);

                        if (itemVaration == null)
                        {
                            provider.itemSource = ItemSourceType.None;
                        }
                        else
                        {
                            provider.itemVariation = itemVaration;
                            provider.itemSource = ItemSourceType.Variation;
                        }
                    }
                }
            }

            RackObjects[i] = rackObj;

            rackLabel++;
        }
    }

    private void CreateRandomRacks()
    {
        RackObjects = new GameObject[rackCount];

        var basePosition = transform.position - (rackCount / 2) * rackSpacing;
        var rackLabel = char.ToUpper(rackLabelPrefix);

        for (int i = 0; i < rackCount; i++)
        {
            var rackObj = Instantiate(rackVariations[UnityEngine.Random.Range(0, rackVariations.Length)]);
            var rackComponent = rackObj.GetComponent<RackComponent>();

            rackObj.transform.parent = transform;
            rackObj.transform.position = basePosition + i * rackSpacing;

            rackObj.name = "Rack-" + rackLabel;

            rackComponent.labelFormat = rackLabel + "{0}.{1}";

            var boxObjects = rackComponent.FillRack(rackComponent.width, rackComponent.height);

            for (int x = 0; x < rackComponent.width; x++)
            {
                for (int y = 0; y < rackComponent.height; y++)
                {
                    var provider = boxObjects[x][y].GetComponent<ItemProvider>();
                    provider.itemVariation = itemVariations[UnityEngine.Random.Range(0, itemVariations.Length)];
                    provider.itemSource = ItemSourceType.Variation;
                }
            }

            RackObjects[i] = rackObj;

            rackLabel++;
        }
    }
}
