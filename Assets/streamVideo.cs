using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class streamVideo : MonoBehaviour
{
    public RawImage rawImage;
    public VideoPlayer videoPlayer;
    public AudioSource audioSource; 

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(PlayVideo());
    }
    IEnumerator PlayVideo()
    {
        videoPlayer.Prepare();
        WaitForSeconds waitForSeconds = new WaitForSeconds(1);
        while (videoPlayer.isPrepared)
        {
            yield return WaitForSeconds;
            break;
        }
        rawImage.texture = videoPlayer.texture;
        videoPlayer.Play();
        audioSource.Play();
    }

}
