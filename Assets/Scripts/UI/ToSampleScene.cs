﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToSampleScene : MonoBehaviour
{
    public void OnClick()
    {
        SceneManager.LoadScene("TutorialScene");
    }
}
