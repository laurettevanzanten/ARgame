using Assets.Scripts.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WebcomStateUI : MonoBehaviour
{
    public string textFormat = "webcom: user {0}, ping {1} s";

    private WebCom _webCom;
    private TextMeshProUGUI _messageText;

    void Start()
    {
        _webCom = GameObject.FindGameObjectWithTag(Tags.WebComTag).GetComponent<WebCom>();
        _messageText = GetComponent<TextMeshProUGUI>();

        _messageText.text = string.Format(textFormat, ParseUserName(), ParsePingTime());
    }

    void Update()
    {
        _messageText.text =
            !string.IsNullOrEmpty(_webCom.NetworkError)
                ? _webCom.NetworkError
                : _webCom.PingTime == -1 
                    ? "contacting server..."
                    : string.Format(textFormat, ParseUserName(), ParsePingTime());
    }

    private string ParseUserName() => string.IsNullOrEmpty(_webCom.userName) ? "???" : _webCom.userName;

    private string ParsePingTime() =>
        _webCom.PingTime >= 0
            ? Mathf.Round(_webCom.PingTime).ToString()
            : _webCom.PingTime == -1
                ? "???"
                : (-_webCom.PingTime).ToString();
}
