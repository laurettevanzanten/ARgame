using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginButtonBehavior : MonoBehaviour
{
    public GameObject nameFieldObject;
    public GameObject passwordFieldObject;

    private TMP_InputField nameField;
    private TMP_InputField passwordField;
    private TextMeshProUGUI messageText;

    private Button loginButton;

    public void Start()
    {
        nameField = nameFieldObject.GetComponent<TMP_InputField>();
        passwordField = passwordFieldObject.GetComponent<TMP_InputField>();

        loginButton = GetComponent<Button>();
    }

    void Update()
    {
        loginButton.interactable = !string.IsNullOrEmpty(nameField.text) && !string.IsNullOrEmpty(passwordField.text);
    }
}
