using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public delegate void MessageCallback(string serverReplyText, long responseCode);

public class MessageInfo
{
    public int timeStamp;
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

    public string UserToken { get; set; }

    private List<MessageInfo> messageQueue = new List<MessageInfo>();
    private List<MessageInfo> sendQueue  = new List<MessageInfo>();

    private int timeStamp = 0;

    private float lastSendTime = 0;

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
        var message = new OrderMessage()
        {
            user = userName,
            password = password,
            sessionId = sessionId,
            timeStamp = (int)Time.time,
            items = items.ToArray(),
        };

        messageQueue.Add(new MessageInfo() { message = message, route = "post-order", timeStamp = timeStamp });
        timeStamp++;
        Debug.Log("order added to message queue, " + messageQueue.Count + " items in queue");

    }

    public void Login(string userName, string password, MessageCallback callback)
    {
        messageQueue.Add(new MessageInfo()
        {
            message = new LoginMessage()
            {
                password = password,
                user = userName
            },
            route = "login",
            timeStamp = timeStamp,
            callback = callback
        });

        timeStamp++;

        Debug.Log("login added to message queue, " + messageQueue.Count + " items in queue");
    }

    private void FlushQueue(List<MessageInfo> queue)
    {
        Debug.Log("sending " + queue.Count + "items");
        for (int i = 0; i < queue.Count; i++)
        {
            var messageInfo = queue[i];
            PostASync(url + "/" + messageInfo.route, 
                        JsonUtility.ToJson(messageInfo.message),
                        messageInfo.timeStamp,
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
            if (timeStamp == queue[i].timeStamp)
            {
                return i;
            }
        }

        return -1;
    }

    private IEnumerator PostRequest(string url, string json, int timeStamp, MessageCallback callback)
    {
        Debug.Log("[" + timeStamp + "] sending " + json + " to " + url);

        var uwr = new UnityWebRequest(url, "POST");

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);

        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            callback.Invoke("error", uwr.responseCode);
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            callback(uwr.downloadHandler.text,  uwr.responseCode);

            var index = FindMessageIndex(timeStamp, sendQueue);

            if (index == -1)
            {
                Debug.LogError("Client error, cannot resolve message with index " + timeStamp);
            }
            else 
            {
                sendQueue.RemoveAt(index);
                Debug.Log("ack " + timeStamp + ", " + sendQueue.Count + " messages in send queue remaining");
            }
        }
    }
}
