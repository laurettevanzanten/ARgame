using Assets.Scripts.Model;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public GameObject loadScreenSpinner;
    
    private WebCom _webCom;

    public void Start()
    {
        loadScreenSpinner.SetActive(false);

        var webComObject = GameObject.FindGameObjectWithTag(Tags.WebComTag);

        _webCom = webComObject?.GetComponent<WebCom>();
       
        if (_webCom == null)
        {
            Debug.LogWarning("No webcom object resolved");
        }
    }

    public void OnClick()
    {
        FadeUtility.FadeToNextScene(loadScreenSpinner, _webCom, () => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1));

    }
}
