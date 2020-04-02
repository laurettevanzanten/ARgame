
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Behaviour driving the Clock UI Element
/// </summary>
public class ClockBehaviour : MonoBehaviour
{
    public string textFormat = "Time remaining {0}:{1}";
    private TextMeshProUGUI textComponent;

    public void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    public void Update()
    {
        if (GameStateComponent.Instance != null && textComponent != null)
        {
            var seconds = (int) (GameStateComponent.Instance.TimeRemaining % 60);
            var minutes = (int) (GameStateComponent.Instance.TimeRemaining / 60);
            textComponent.text = string.Format(textFormat, minutes, seconds);
        }
        else
        {
            Debug.Log((GameStateComponent.Instance == null ? "null" : "gamestate") + "/" + (textComponent == null ? "null" : "textcomponent"));
        }
    }
}
