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
    public string url = "http://localhost:3000/";
    public string userName = "admin";
    public string password = "foo";
    public int    sessionId = 0;

    public float timeout = 30.0f;
    public float sendInterval = 1.0f;

    /// <summary>
    /// No messages will be send to the back-end when the user name equals
    /// the "guest" name.
    /// </summary>
    public string guestUserName = "guest";

    public string UserToken { get; set; }
    public float SessionTime { get; set; }

    private List<MessageInfo> messageQueue = new List<MessageInfo>();
    private List<MessageInfo> sendQueue  = new List<MessageInfo>();

    private int messageAckId = 0;

    private float lastSendTime = 0;

    void Awake()
    {
        var others = GameObject.FindGameObjectsWithTag(Tags.WebComTag);

        for (int i = 0; others != null && i < others.Length; i++)
        {
            if (others[i] != gameObject)
            {
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

        DontDestroyOnLoad(gameObject);
    }

    public void Update()
    {
        if (sendQueue.Count > 0)
        {
            if (Time.time - lastSendTime > timeout)
            {
                Debug.Log("timeout, trying again");
                lastSendTime = Time.time;
                FlushQueue(sendQueue);
            }
        }
        else if (Time.time - lastSendTime > sendInterval)
        {
            lastSendTime = Time.time;

            if (messageQueue.Count > 0 )
            {
                var temp = sendQueue;
                sendQueue = messageQueue;
                messageQueue = temp;

                FlushQueue(sendQueue);
            }
        }
    }

    public void PostOrder(List<CollectedItem> items)
    {
        if (userName != guestUserName)
        {
            var message = new OrderMessage()
            {
                token = UserToken,
                sessionId = sessionId,
                timeStamp = Time.time,
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
            callback.Invoke("error", uwr.responseCode);

            Debug.Log("Error while sending message, error = " + uwr.error);
            callback(uwr.error, uwr.responseCode);
        }
        else
        {
            callback?.Invoke(uwr.downloadHandler.text, uwr.responseCode);

            var index = FindMessageIndex(ackId, sendQueue);

            if (index == -1)
            {
                Debug.LogError("Client error, cannot resolve message with ack-id " + ackId + ".");
            }
            else 
            {
                sendQueue.RemoveAt(index);
                Debug.Log("Server-Ack " + ackId + ", " + sendQueue.Count + " messages in send queue remaining.");
            }
        }
    }
}
