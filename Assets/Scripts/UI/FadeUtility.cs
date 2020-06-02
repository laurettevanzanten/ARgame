using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeUtility
{
    public static void FadeToNextScene(GameObject loadScreenSpinner, WebCom webCom, Action fadeCompletedCallback)
    {
        var fadeoutCompleted = false;
        var serverResponseReceived = false;
        var fadeScreen = FadeImageColor.Instance;
        var nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        loadScreenSpinner?.SetActive(true);

        if (webCom != null)
        {
            serverResponseReceived = false;
            fadeoutCompleted = fadeScreen != null ? false : true;

            fadeScreen?.FadeOut(() =>
            {
                fadeoutCompleted = true;

                if (serverResponseReceived)
                {
                    fadeCompletedCallback();
                }
            });

            webCom.SaveProgress(nextScene, (message, response) =>
            {
                if (response == 200)
                {
                    serverResponseReceived = true;
                    webCom.scene = nextScene;

                    if (fadeoutCompleted)
                    {
                        fadeCompletedCallback();
                    }
                }
                else
                {
                    Debug.Log("Problem reaching the server " + response);
                }
            });
        }
        else
        {
            fadeCompletedCallback();
        }
    }
}

