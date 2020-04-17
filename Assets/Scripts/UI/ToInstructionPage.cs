using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToInstructionPage : MonoBehaviour
{
    public void OnClick ()
    {
        SceneManager.LoadScene("InstructionPage");
    }
}
