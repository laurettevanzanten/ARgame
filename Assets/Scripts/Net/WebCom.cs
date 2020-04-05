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

        PostASync(url, JsonUtility.ToJson(message));
    }

    public void PostASync(string uri, string json)
    {
        StartCoroutine(PostRequest(uri, json));
    }

    IEnumerator PostRequest(string url, string json)
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
            Debug.Log("Received: " + uwr.downloadHandler.text);
        }
    }
}
