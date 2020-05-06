using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LoginLogic : MonoBehaviour
{
    public GameObject nameFieldObject;
    public GameObject passwordFieldObject;
    public GameObject messageObject;
    public GameObject webcomObject;
    public GameObject buttonObject;

    public string nextScene;

    private TMP_InputField nameField;
    private TMP_InputField passwordField;
    private Button button;

    private TextMeshProUGUI messageText;
    private WebCom webCom;
    private bool isLoggingIn;

    public void Start()
    {
        nameField = nameFieldObject.GetComponent<TMP_InputField>();
        passwordField = passwordFieldObject.GetComponent<TMP_InputField>();
        messageText = messageObject.GetComponent<TextMeshProUGUI>();
        webCom = webcomObject.GetComponent<WebCom>();
        button = buttonObject.GetComponent<Button>();

        isLoggingIn = false;
        messageText.text = "";

        nameField.ActivateInputField();
    }

    public void StartLogin()
    {
        if (!isLoggingIn)
        {
            if (!string.IsNullOrEmpty(nameField.text) && !string.IsNullOrEmpty(passwordField.text))
            {
                nameField.interactable = false;
                passwordField.interactable = false;
                button.interactable = false;

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

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            if (nameField.isFocused)
            {
                passwordField.ActivateInputField();
            }
            else if (passwordField.isFocused)
            {
                passwordField.DeactivateInputField();
            }
            else 
            {
                nameField.ActivateInputField();
            }
        }

        if (Input.GetKeyUp(KeyCode.Space) && !passwordField.isFocused && !nameField.isFocused)
        {
            button.Select();
        }
    }

    private void StopLoggingIn()
    {
        isLoggingIn = false;
        nameField.interactable = true;
        passwordField.interactable = true;
        button.interactable = true;
    }

    private void OnLoginResponse(string text, long responseCode)
    {
        if (responseCode == 200)
        {
            messageText.text = "Log in ok...";
            var response = JsonUtility.FromJson<LoginResponse>(text);
            webCom.UserToken = response.token;
            webCom.sessionId = response.session;
            webCom.userName = nameField.text;
            webCom.password = passwordField.text;
            Debug.Log("Loading scene " + nextScene);
            SceneManager.LoadScene(nextScene);
        }
        else if (responseCode >= 400 && responseCode < 500)
        {
            messageText.text = "Invalid name and/or password. Try again...";
            StopLoggingIn();        
        }
        else if (responseCode >= 500 && responseCode < 600)
        {
            messageText.text = "Hmm ... something is wrong, the server reported an error.";
            StopLoggingIn();
        }
        else
        {
            messageText.text = text;
            StopLoggingIn();
        }
    }
}
