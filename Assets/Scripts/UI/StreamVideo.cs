using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StreamVideo : MonoBehaviour
{
    public GameObject imageObject;
    public GameObject audioObject;
    public GameObject videoObject;

    private RawImage _rawImage;

    private VideoPlayer _videoPlayer;
    private AudioSource _audioSource;

    // Start is called before the first frame update
    void Start()
    {
        _videoPlayer = videoObject != null ? videoObject.GetComponent<VideoPlayer>() : GetComponent<VideoPlayer>();
        _audioSource = audioObject != null ? audioObject.GetComponent<AudioSource>() : GetComponent<AudioSource>();
        _rawImage = imageObject != null ? imageObject.GetComponent<RawImage>() : GetComponent<RawImage>();

        StartCoroutine(PlayVideo());
    }

    IEnumerator PlayVideo()
    {
        WaitForSeconds aMoreDistinctiveNameThan_waitForSeconds = new WaitForSeconds(3);

        _videoPlayer.Prepare();

        while (!_videoPlayer.isPrepared)
        {
            yield return aMoreDistinctiveNameThan_waitForSeconds;
            break;
        }

        _videoPlayer.Play();
        _audioSource.Play();

        _rawImage.texture = _videoPlayer.texture;

    }

}
