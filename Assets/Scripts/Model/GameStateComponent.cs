using Assets.Scripts.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public string nextSceneName;

    public SoundList soundList;

    public float TimeRemaining =>
            webComponent == null
                ? Mathf.Max(0, maxTimeSeconds - (Time.time - startTime))
                : Mathf.Max(0, maxTimeSeconds - ((Time.time - startTime) + webComponent.SessionTime));

    public int CompletedOrders => CollectedItems.Count -1;
    public GameObject[] RackObjects { get; private set; }
    public List<List<CollectedItem>> CollectedItems { get; private set; } = new List<List<CollectedItem>>();

    public List<List<OrderProperties>> Orders { get; private set; }
    public int CurrentOrderListIndex { get; private set; } = 0;
    public List<OrderProperties> CurrentOrderList => CurrentOrderListIndex < Orders.Count ? Orders[CurrentOrderListIndex] : null;

    public int CurrentOrderLine { get; private set; } = 0;

    private float startTime;

    private WebCom webComponent;
    private AudioSource audioSource;

    public void Start()
    {
        Instance = this;
        
        Orders = CSVParser.ParseGrid(orders, (value, x, y) => new OrderProperties(value));

        if (rackSetup == null)
        {
            SetupRacks(null, CreateRandomItemVariation);
        }
        else
        {
            SetupRacks(CSVParser.ParseGrid(rackSetup, (value, x, y) => value), CreateItemVariationFromItemGrid);
        }

        CollectedItems.Add(new List<CollectedItem>());

        var webComObject = GameObject.FindGameObjectWithTag(Tags.WebComTag);

        if (webComObject != null)
        {
            webComponent = webComObject.GetComponent<WebCom>();

            if (webComponent == null)
            {
                Debug.LogWarning("Cannot resolve WebCom component");
            }
            else
            {
                Debug.Log("Webcom found using user token " + webComponent.UserToken);
            }
        }
        else
        {
            Debug.LogWarning("Cannot resolve WebCom object");
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void Update()
    {
        if (TimeRemaining <= 0)
        {
            SceneManager.LoadScene(nextSceneName);
        }

    }


    public void StartClock()
    {
        startTime = Time.time;
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

    public void OnItemCollected(ItemBehaviour itemLabel)
    {
        if (CurrentOrderListIndex < CollectedItems.Count)
        {
            CollectedItems[CurrentOrderListIndex].Add(new CollectedItem()
            {
                pos = itemLabel.OriginCoordinate,
                ts = (int) Time.time
            });

            TryToPlaySound(soundList.itemCollected);
        }
        else
        {
            Debug.LogWarning("item " + itemLabel + " went into the void...");
        }
    }

    public void OnOrderCompleted()
    {
        if (CurrentOrderListIndex < Orders.Count)
        {
            if (webComponent != null)
            {
                webComponent.PostOrder(CollectedItems[CollectedItems.Count - 1]);
            }
            else
            {
                Debug.LogWarning("no webcomponent defined, cannot send messages to server.");
            }

            TryToPlaySound(soundList.nextOrder);
            CollectedItems.Add(new List<CollectedItem>());
            CurrentOrderLine = 0;
        }

        CurrentOrderListIndex++;

        if (CurrentOrderListIndex >= Orders.Count)
        {
            CurrentOrderLine = -1;
            Debug.Log("No more orders to collect...");
        }
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

            TryToPlaySound(soundList.nextOrderLine);
        }
    }

    private void TryToPlaySound(AudioClip sound)
    {
        if (audioSource != null && sound != null )
        {
            audioSource.PlayOneShot(sound);
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

    private void CreateItemVariationFromItemGrid(GameObject boxObject, List<List<string>> itemGrid, int x, int y)
    {
        var provider = boxObject.GetComponent<ItemProvider>();
        var itemName = ResolveItemVaration(itemGrid, x, y);

        provider.OriginCoordinate = new Vector2Int(x, y);

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

    private void CreateRandomItemVariation(GameObject boxObject, List<List<string>> itemGrid, int x, int y)
    {
        var provider = boxObject.GetComponent<ItemProvider>();
        provider.itemVariation = itemVariations[UnityEngine.Random.Range(0, itemVariations.Length)];
        provider.itemSource = ItemSourceType.Variation;
    }

    private void SetupRacks(List<List<string>> itemGrid, Action<GameObject, List<List<string>>,  int, int> initializeBoxObject)
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
                    initializeBoxObject(boxObjects[x][y], itemGrid, x + rackComponent.width * i, y);
                }
            }

            RackObjects[i] = rackObj;

            rackLabel++;
        }
    }
}
