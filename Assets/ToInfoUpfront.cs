using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToInfoUpfront : MonoBehaviour
{
    public void OnClick()
    {
        Debug.Log("ToInfoUpfront");
        SceneManager.LoadScene("SampleScene");
    }
}
