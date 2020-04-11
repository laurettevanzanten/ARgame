using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 

public class CountdownTimer : MonoBehaviour
{
    public int countdownTime;
    public Text countdownDisplay; //this will refer to the countdown text object in Unity. I still have to create this object, I didn't manage to connect the script to the UI 

    private void Start()
    {
        StartCoroutine(CountdownToStart());
    }
    IEnumerator CountdownToStart ()
    {
        while(countdownTime > 0)
        {
            countdownDisplay.text = countdownTime.ToString();

            yield return new WaitForSeconds(if) ;

            countdownTime--;
        }

        countdownDisplay.text = "Go!";

        //now we have to let the game know it can start, so counting down from 900 seconds. Did not manage to connect this yet.

        //to disable the countdown timer afterwards

        countdownDisplay.gameObject.SetActive(false);
    }
    

}
