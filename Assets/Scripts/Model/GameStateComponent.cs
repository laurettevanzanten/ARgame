using Assets.Scripts.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameStateComponentState
{
    CountDown,
    InGame,
    Paused,
    LostServerConnection,
    GameCompleted
}

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

    public GameObject loadSpinnerObject;

    public float TimeRemaining { get; private set; }

    public GameStateComponentState State { get; private set; } = GameStateComponentState.CountDown;

    public int CompletedOrders => CollectedItems.Count -1;
    public GameObject[] RackObjects { get; private set; }
    public List<List<CollectedItem>> CollectedItems { get; private set; } = new List<List<CollectedItem>>();

    public List<List<OrderProperties>> Orders { get; private set; }
    public int CurrentOrderListIndex { get; private set; } = 0;
    public List<OrderProperties> CurrentOrderList => CurrentOrderListIndex < Orders.Count ? Orders[CurrentOrderListIndex] : null;

    public int CurrentOrderLine { get; private set; } = 0;

    private WebCom webComponent;
    private AudioSource audioSource;

    private Dictionary<GameObject, CollectedItem> _pickedupItems = new Dictionary<GameObject, CollectedItem>();

    public bool IsGameActive { get; private set; } = true;

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
                // consume any time remaining from the last time a session was run
                // ie if the user aborts the game, we keep use of the last order sent to estimate
                // when the game should start. This time is kept in the web component and consumed 
                // one time by the game state. 
                TimeRemaining = Math.Max(0, maxTimeSeconds - webComponent.ConsumeSessionTime());
                Debug.Log("Webcom found using user token " + webComponent.UserToken);
            }
        }
        else
        {
            Debug.LogWarning("Cannot resolve WebCom object");
            TimeRemaining = maxTimeSeconds;
        }

        audioSource = GetComponent<AudioSource>();

        State = GameStateComponentState.CountDown;
    }

    public void Update()
    {
        switch (State)
        {
            case GameStateComponentState.CountDown:
                break;

            case GameStateComponentState.InGame:

                // no more time or the user hit the debug command to go to the next level ?
                if (TimeRemaining <= 0 || CheckDebugKey(KeyCode.Slash))
                {
                    // fade the screen to black and load the next scene
                    FadeUtility.FadeToNextScene(loadSpinnerObject, webComponent,
                            () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1));

                    State = GameStateComponentState.GameCompleted;
                }
                else
                {
                    TimeRemaining = Mathf.Max(0, TimeRemaining - Time.deltaTime);

                    // check if the user wants to pause the game
                    if (Input.GetKeyUp(KeyCode.P))
                    {
                        Time.timeScale = 0.0f;
                        State = GameStateComponentState.Paused;
                    }
                    // debug command to decrease the time by 3 minutes
                    else if (CheckDebugKey(KeyCode.Z))
                    {
                        TimeRemaining = Mathf.Max(0, TimeRemaining - 180);
                    }
                }
                break;

            case GameStateComponentState.Paused:
                if (Input.GetKeyUp(KeyCode.P))
                {
                    Time.timeScale = 1.0f;
                    State = GameStateComponentState.InGame;
                }
                break;

            case GameStateComponentState.LostServerConnection:
                break;

            case GameStateComponentState.GameCompleted:

                // just (wait for the server to ack and) do nothing 
                break;
        }
    }

    private bool CheckDebugKey(KeyCode key) => Debug.isDebugBuild && Input.GetKeyUp(key);
    

    /// <summary>
    /// Start the game clock, will happen after for instance the countdown completes
    /// </summary>
    public void StartClock()
    {
        State = GameStateComponentState.InGame;
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

    public void OnItemTakenFromBox(ItemBehaviour itemLabel)
    {
        Debug.Log("Item taken at " + TimeRemaining);

        _pickedupItems[itemLabel.gameObject] = new CollectedItem()
        {
            pos = itemLabel.OriginCoordinate,
            ts = maxTimeSeconds - TimeRemaining
        };
    }

    public void OnItemCollected(ItemBehaviour itemLabel)
    {
        if (CurrentOrderListIndex < CollectedItems.Count)
        {
            CollectedItems[CurrentOrderListIndex].Add(_pickedupItems[itemLabel.gameObject]);

            _pickedupItems.Remove(itemLabel.gameObject);

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
                foreach (var collectedItem in _pickedupItems.Values)
                {
                    // missed items have a negative position + (-1,-1) 
                    // this way we can still figure out where the item comes from 
                    collectedItem.pos = -(collectedItem.pos)  + Vector2Int.one * -1;
                    CollectedItems[CurrentOrderListIndex].Add(collectedItem);
                }

                webComponent.PostOrder(CollectedItems[CollectedItems.Count - 1], maxTimeSeconds - TimeRemaining);
            }
            else
            {
                Debug.LogWarning("no webcomponent defined, cannot send messages to server.");
            }

            foreach ( var pickedUpItem in _pickedupItems.Keys)
            {
                GameObject.Destroy(pickedUpItem);
            }

            _pickedupItems.Clear();

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
