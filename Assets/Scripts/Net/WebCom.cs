using Assets.Scripts.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public delegate void MessageCallback(string serverReplyText, long responseCode);

public class MessageInfo
{
    public int ackId;
    public string route;
    public object message;
    public MessageCallback callback;
}

public class WebCom : MonoBehaviour
{
    const string NetworkErrorText = "network error";

    public string url = "http://localhost:3000/";
    public string userName = "admin";
    public string password = "foo";
    public int    scene = 0;

    public float timeout = 30.0f;
    public float sendInterval = 0.5f;

    /// <summary>
    /// How often should we send a heartbeat to the server
    /// </summary>
    public float heartbeatInterval = 5.0f;

    public bool loginOnStart = false;

    /// <summary>
    /// No messages will be send to the back-end when the user name equals
    /// the "guest" name.
    /// </summary>
    public string guestUserName = "guest";

    /// <summary>
    /// Token used to identify a logged in user on the server
    /// </summary>
    public string UserToken { get; set; }

    /// <summary>
    /// Time the last known session stopped. Used when restoring previous sessions.
    /// </summary>
    public float SessionTime { get; private set; } = 0;

    public string NetworkError { get; private set; } = null;

    /// <summary>
    /// Last received ping / heartbeat
    /// </summary>
    public float PingTime { get; set; } = -1;

    private List<MessageInfo> messageQueue = new List<MessageInfo>();
    private List<MessageInfo> sendQueue  = new List<MessageInfo>();

    /// <summary>
    /// Ids used to identify what message was acknowledged by the server
    /// </summary>
    private int messageAckId = 0;

    private float lastSendTime = 0;

    /// <summary>
    /// Last time a heartbeat was sent
    /// </summary>
    private float lastHeartbeatTime = -1.0f;

    private bool waitingToHeartbeat = false;

    void Awake()
    {
        var others = GameObject.FindGameObjectsWithTag(Tags.WebComTag);

        for (int i = 0; others != null && i < others.Length; i++)
        {
            if (others[i] != gameObject)
            {
                // if the other does not have a user token its redundant for the duration
                // of this session
                if (!string.IsNullOrEmpty(UserToken))
                {
                    Destroy(others[i]);
                }
                else
                {
                    Destroy(gameObject);
                    break;
                }
            }
        }

        if (others == null || others.Length <= 1)
        {
            if (loginOnStart)
            {
                Debug.Log("logging in using component credentials");
                Login(this.userName, this.password, (serverReplyText, responseCode) => 
                {
                    if (responseCode == 200)
                    {
                        Debug.Log( "(auto) Log in ok...");
                        var response = JsonUtility.FromJson<LoginResponse>(serverReplyText);
                        UserToken = response.token;
                    }
                    else
                    {
                        Debug.LogError("Login failed: " + responseCode);
                    }
                });
                lastSendTime = Time.time;
                FlushQueue(sendQueue);
            }
        }

        DontDestroyOnLoad(gameObject);
    }


    public void Update()
    {
        UpdateHeartbeat();
      
        if (sendQueue.Count > 0 && !string.IsNullOrEmpty(UserToken))
        {
            if (Time.time - lastSendTime > timeout)
            {
                Debug.Log("timeout, trying again");
                lastSendTime = Time.time;
                FlushQueue(sendQueue);
            }
        }
        else if (Time.time - lastSendTime > sendInterval && sendQueue.Count == 0)
        {
            lastSendTime = Time.time;

            if (messageQueue.Count > 0)
            {
                var temp = sendQueue;
                sendQueue = messageQueue;
                messageQueue = temp;

                FlushQueue(sendQueue);
            }
        }
    }

    public void InitializeFromSavedSettings(LoginResponse response, string name, string pwd)
    {
        UserToken = response.token;
        scene = response.scene == -1 ? 1 : response.scene;
        userName = name;
        password = pwd;
        SessionTime = response.timeStamp;

    }

    /// <summary>
    /// Returns the session time and sets it to 0. The session time should only be used once.
    /// </summary>
    /// <returns></returns>
    public float ConsumeSessionTime()
    {
        var result = SessionTime;
        SessionTime = 0;
        return result;
    }

    public void PostOrder(List<CollectedItem> items, float gameTime)
    {
        if (userName != guestUserName)
        {
            var message = new OrderMessage()
            {
                token = UserToken,
                scene = scene,
                timeStamp = gameTime,
                items = items.ToArray(),
            };

            messageQueue.Add(new MessageInfo() { message = message, route = "post-order", ackId = messageAckId });
            messageAckId++;
            Debug.Log("order added to message queue, " + messageQueue.Count + " items in queue.");
        }
        else
        {
            Debug.Log("user is guest ignoring message.");
        }
    }

    public void SaveProgress(int nextScene, MessageCallback callback)
    {
        if (userName != guestUserName)
        {
            var message = new OrderMessage()
            {
                token = UserToken,
                scene = nextScene,
                timeStamp = 0,
                items = new CollectedItem[0],                
            };

            messageQueue.Add(new MessageInfo()
            {
                message = message,
                route = "post-order",
                ackId = messageAckId,
                callback = callback
            });

            messageAckId++;
            Debug.Log("save progress to message queue, " + messageQueue.Count + " items in queue.");
        }
        else
        {
            Debug.Log("user is guest ignoring message.");
        }
    }

    public void Login(string userName, string password, MessageCallback callback)
    {
        if (userName != guestUserName)
        {
            messageQueue.Add(new MessageInfo()
            {
                message = new LoginMessage()
                {
                    password = password,
                    user = userName
                },
                route = "login",
                ackId = messageAckId,
                callback = callback
            });

            messageAckId++;

            Debug.Log("login added to message queue, " + messageQueue.Count + " items in queue.");
        }
        else
        {
            Debug.Log("User is guest, ignoring message.");
        }
    }

    private void UpdateHeartbeat()
    {
        if (lastHeartbeatTime < 0 || ((Time.time - lastHeartbeatTime) > heartbeatInterval && !waitingToHeartbeat))
        {
            lastHeartbeatTime = Time.time;
            waitingToHeartbeat = true;

            messageQueue.Add(new MessageInfo()
            {
                message = new HeartbeatMessage()
                {
                    // timestamp is unused at the time of writing
                    timeStamp = 0,

                    // token may or may not be defined
                    token = UserToken
                },
                route = "heartbeat",
                ackId = messageAckId,
                callback = (serverReplyText, responseCode) =>
                {
                    if (serverReplyText != NetworkErrorText)
                    {
                        if (responseCode == 200)
                        {
                            PingTime = Time.time - lastHeartbeatTime;
                        }
                        else
                        {
                            PingTime = -responseCode;
                        }
                    }

                    // update the time so we won't send heartbeats too quickly
                    waitingToHeartbeat = false;
                    lastHeartbeatTime = Time.time;
                }
            });

            messageAckId++;
        }
    }

    private void FlushQueue(List<MessageInfo> queue)
    {
        Debug.Log("sending " + queue.Count + "items");
        for (int i = 0; i < queue.Count; i++)
        {
            var messageInfo = queue[i];
            PostASync(url + "/" + messageInfo.route, 
                        JsonUtility.ToJson(messageInfo.message),
                        messageInfo.ackId,
                        messageInfo.callback);
        }
    }

    public void PostASync(string uri, string json, int timeStamp, MessageCallback callback)
    {
        StartCoroutine(PostRequest(uri, json, timeStamp, callback));
    }

    private int FindMessageIndex(int timeStamp, List<MessageInfo> queue)
    {
        for (int i = 0; i < queue.Count; i++)
        {
            if (timeStamp == queue[i].ackId)
            {
                return i;
            }
        }

        return -1;
    }

    private IEnumerator PostRequest(string url, string json, int ackId, MessageCallback callback)
    {
        Debug.Log("[" + ackId + "] sending " + json + " to " + url);

        var uwr = new UnityWebRequest(url, "POST");

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);

        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();


        if (uwr.isNetworkError)
        {
            callback.Invoke(NetworkErrorText, uwr.responseCode);

            Debug.Log("Error while sending message, error = " + uwr.error);
            callback(uwr.error, uwr.responseCode);

            NetworkError = NetworkErrorText + (uwr.responseCode != 0 ? " " + uwr.responseCode : ""); 
        }
        else
        {
            NetworkError = string.Empty;

            callback?.Invoke(uwr.downloadHandler.text, uwr.responseCode);

            var index = FindMessageIndex(ackId, sendQueue);
            sendQueue.RemoveAt(index);


            if (index == -1)
            {
                Debug.LogError("Client error, cannot resolve message with ack-id " + ackId + ".");
            }
            else 
           {
                
                Debug.Log("Server-Ack " + ackId + ", " + sendQueue.Count + " messages in send queue remaining.");
            }
        }
    }
}
