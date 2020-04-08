using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WebCom : MonoBehaviour
{
    public string url = "http://localhost:3000/post-session-db";
    public string userName = "admin";
    public string password = "foo";
    public int    sessionId = 0;

    public float timeout = 30.0f;
    public float sendInterval = 1.0f;

    private List<OrderMessage> messageQueue = new List<OrderMessage>();
    private List<OrderMessage> sendQueue  = new List<OrderMessage>();

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

        messageQueue.Add(message);
        Debug.Log("order added to message queue, " + messageQueue.Count + " items in queue");

    }

    private void FlushQueue(List<OrderMessage> queue)
    {
        Debug.Log("sending " + queue.Count + "items");
        for (int i = 0; i < queue.Count; i++)
        {
            PostASync(url, JsonUtility.ToJson(queue[i]));
        }
    }

    public void PostASync(string uri, string json)
    {
        StartCoroutine(PostRequest(uri, json));
    }

    private int FindMessageIndex(int timeStamp, List<OrderMessage> queue)
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

    private IEnumerator PostRequest(string url, string json)
    {
        var uwr = new UnityWebRequest(url, "POST");
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
        uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
        uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        uwr.SetRequestHeader("Content-Type", "application/json");

        yield return uwr.SendWebRequest();

        if (uwr.isNetworkError)
        {
            Debug.Log("Error While Sending: " + uwr.error);
        }
        else
        {
            var response = JsonUtility.FromJson<ServerReply>(uwr.downloadHandler.text);
            var index = FindMessageIndex(response.timeStamp, sendQueue);

            if (index == -1)
            {
                Debug.LogError("Client error, cannot resolve message with index " + response.timeStamp);
            }
            else if (response.errorCode == 0)
            {
                sendQueue.RemoveAt(index);
                Debug.Log("ack " + response.timeStamp + ", " + sendQueue.Count + " messages in send queue remaining");
            }
            else
            {
                Debug.LogError("Server error (" + response.errorCode + "(" + response.timeStamp + "), '" + response.message + "') when sending " + sendQueue[index]);
            }
        }
    }
}
