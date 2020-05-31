using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public enum LoginState
{
    ContactingServer,
    ReadingUserInput,
    AwaitingServerReply,
    LoadingNextLevel
}

public class LoginLogic : MonoBehaviour
{
    public GameObject nameFieldObject;
    public GameObject passwordFieldObject;
    public GameObject messageObject;
    public GameObject webcomObject;
    public GameObject buttonObject;

    public GameObject loadingImageObject;
    public GameObject fadeObject;

    public string nextScene;

    private TMP_InputField _nameField;
    private TMP_InputField _passwordField;
    private Button _button;

    private TextMeshProUGUI _messageText;
    private WebCom _webCom;
    private LoginState _loginState;

    private FadeImageColor _fadeComponent;

    public void Start()
    {
        _nameField = nameFieldObject.GetComponent<TMP_InputField>();
        _passwordField = passwordFieldObject.GetComponent<TMP_InputField>();
        _messageText = messageObject.GetComponent<TextMeshProUGUI>();
        _webCom = webcomObject.GetComponent<WebCom>();
        _button = buttonObject.GetComponent<Button>();
        _fadeComponent = fadeObject.GetComponent<FadeImageColor>();

        _loginState = LoginState.ContactingServer;
        _messageText.text = "Contacting Server ...";

        loadingImageObject.SetActive( true );
        EnableInput(false);

        _loginState = LoginState.ContactingServer;
    }

    /// <summary>
    /// Callback from a user action (button click)
    /// </summary>
    public void StartLogin()
    {
        if (_loginState == LoginState.ReadingUserInput)
        {
            if (!string.IsNullOrEmpty(_nameField.text) && !string.IsNullOrEmpty(_passwordField.text))
            {
                EnableInput(false);
                loadingImageObject.SetActive(true);

                _messageText.text = "logging in to server ...";
                _loginState = LoginState.AwaitingServerReply;
                _webCom.Login(_nameField.text, _passwordField.text, OnLoginResponse);
            }
            else
            {
                _messageText.text = "please enter name and password.";
            }
        }
    }

    public void Update()
    {
        switch (_loginState)
        {
            case LoginState.ContactingServer:
                UpdateContactingServer();
                break;
            case LoginState.ReadingUserInput:
                UpdateReadUserInput();
                break;
            case LoginState.AwaitingServerReply:
                break;
            case LoginState.LoadingNextLevel:
                break;
        }
    }

    private void UpdateContactingServer()
    {
        if (_webCom.PingTime >= 0 && string.IsNullOrEmpty(_webCom.NetworkError))
        {
            _loginState = LoginState.ReadingUserInput;
            _messageText.text = "";
            _fadeComponent.FadeIn();
            EnableInput(true);
            loadingImageObject.SetActive(false);
        }
    }

    private void UpdateReadUserInput()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            if (_nameField.isFocused)
            {
                _passwordField.ActivateInputField();
            }
            else if (_passwordField.isFocused)
            {
                _passwordField.DeactivateInputField();
            }
            else
            {
                _nameField.ActivateInputField();
            }
        }

        if (Input.GetKeyUp(KeyCode.Space) && !_passwordField.isFocused && !_nameField.isFocused)
        {
            _button.Select();
        }
    }

    private void EnableInput(bool enable)
    {
        _nameField.interactable = enable;
        _passwordField.interactable = enable;
        _button.interactable = enable;
    }

    private void StopLoggingIn(LoginState nextState)
    {
        _loginState = nextState;
        EnableInput(true);
    }

    private void OnLoginResponse(string text, long responseCode)
    {
        if (responseCode == 200)
        {
            var response = JsonUtility.FromJson<LoginResponse>(text);

            _webCom.InitializeFromSavedSettings(response, _nameField.text, _passwordField.text);

            _messageText.text = "Welcome " + _nameField.text + ", starting game";

            _fadeComponent.disableOnFadeComplete = false;
            _fadeComponent.FadeOut(() =>
            {
                _loginState = LoginState.LoadingNextLevel;

                if (_webCom.scene <= -1)
                {
                    Debug.Log("Loading scene " + nextScene);
                    SceneManager.LoadScene(nextScene);
                }
                else
                {
                    Debug.Log("Loading scene " + _webCom.scene);
                    SceneManager.LoadScene(_webCom.scene);
                }
            });
        }
        else if (responseCode >= 400 && responseCode < 500)
        {
            _messageText.text = "Invalid name and/or password. Try again...";
            StopLoggingIn(LoginState.ReadingUserInput);        
        }
        else if (responseCode >= 500 && responseCode < 600)
        {
            _messageText.text = "The server reported an error.";
            StopLoggingIn(LoginState.ReadingUserInput);
        }
        else
        {
            _messageText.text = text;
            StopLoggingIn(LoginState.ReadingUserInput);
        }

        loadingImageObject.SetActive(false);
    }
}
