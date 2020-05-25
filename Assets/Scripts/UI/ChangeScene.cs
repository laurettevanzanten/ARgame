using Assets.Scripts.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public string nextScene;
    private WebCom webCom;

    public void Awake()
    {
        webCom = GameObject.FindGameObjectWithTag(Tags.WebComTag)?.GetComponent<WebCom>();
    }

    public void OnClick()
    {
        if (webCom != null)
        {
            var nextScene = SceneManager.GetActiveScene().buildIndex + 1;
            webCom.SaveProgress(nextScene, (message, response) =>
            {
                if (response == 200)
                {
                    SceneManager.LoadScene(nextScene);
                    webCom.scene = nextScene;
                }
                else
                {
                    Debug.Log("Problem reaching the server " + response);
                }
            });
        }
        else
        {
            SceneManager.LoadScene(nextScene);
        }
    }
}
