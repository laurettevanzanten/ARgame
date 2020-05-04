using TMPro;
using UnityEngine;

public class LoginLogic : MonoBehaviour
{
    public GameObject nameFieldObject;
    public GameObject passwordFieldObject;
    public GameObject messageObject;
    public GameObject webcomObject;

    private TMP_InputField nameField;
    private TMP_InputField passwordField;
    private TextMeshProUGUI messageText;
    private WebCom webCom;
    private bool isLoggingIn;

    public void Start()
    {
        nameField = nameFieldObject.GetComponent<TMP_InputField>();
        passwordField = passwordFieldObject.GetComponent<TMP_InputField>();
        messageText = messageObject.GetComponent<TextMeshProUGUI>();
        webCom = webcomObject.GetComponent<WebCom>();

        isLoggingIn = false;
        messageText.text = "";
    }

    public void StartLogin()
    {
        if (!isLoggingIn)
        {
            if (!string.IsNullOrEmpty(nameField.text) && !string.IsNullOrEmpty(passwordField.text))
            {
                messageText.text = "logging in to server ...";
                isLoggingIn = true;
                webCom.Login(nameField.text, passwordField.text, OnLoginResponse);
            }
            else
            {
                messageText.text = "please enter name and password.";
            }
        }
    }

    private void OnLoginResponse(string text, long responseCode)
    {
        if (responseCode == 200)
        {
            messageText.text = "Log in ok...";
            var response = JsonUtility.FromJson<LoginResponse>(text);
            webCom.UserToken = response.token;
            webCom.sessionId = response.session;

        }
        else if (responseCode >= 400 && responseCode < 500)
        {
            messageText.text = "Invalid name and/or password. Try again...";
            isLoggingIn = false;
        }
        else if (responseCode >= 500 && responseCode < 600)
        {
            messageText.text = "Hmm ... something is wrong, the server reported an error.";
            isLoggingIn = false;
        }
    }
}
