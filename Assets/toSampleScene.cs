using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class toSampleScene : MonoBehaviour
{
    public void OnClick()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
