using System;
using UnityEngine;
using UnityEngine.UI;

public class FadeImageColor : MonoBehaviour
{
    public static FadeImageColor Instance { get; private set; }

    public Color startColor = new Color(0, 0, 0.2f, 1.0f);
    public Color destinationColor = new Color(0, 0, 0, 0);

    public float fadeTime = 1.0f;

    public bool onStartFadeToDestinationColor = true;

    public bool disableOnFadeComplete = true;

    public Action Callback { get; set; }

    private Color _from;
    private Color _to;

    private Image _overlayImage;
    private float _startTime;

    void Start()
    {
        Instance = this;

        _overlayImage = GetComponent<Image>();
        _overlayImage.color = startColor;
        _startTime = onStartFadeToDestinationColor ? Time.time : -1.0f;
        _from = startColor;
        _to = destinationColor;
    }

    void Update()
    {
        if (_startTime >= 0)
        {
            var lerpTime = (Time.time - _startTime) / fadeTime;

            _overlayImage.color = Color.Lerp(_from, _to, Mathf.Min(lerpTime, 1.0f));
            
            if (lerpTime >= 1.0f)
            {
                _startTime = -1;

                if (Callback != null)
                {
                    Callback();
                    Callback = null;
                }

                if (disableOnFadeComplete)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }

    public void Fade( Color from, Color to, float time)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        _from = from;
        _to = to;
        fadeTime = time;
        _startTime = Time.time;
    }

    public void FadeIn(Action callback = null)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        _from = startColor;
        _to = destinationColor;
        disableOnFadeComplete = true; 
        _startTime = Time.time;
        Callback = callback;
    }

    public void FadeOut(Action callback = null)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        _from = destinationColor;
        _to = startColor;
        _startTime = Time.time;
        disableOnFadeComplete = false;
        Callback = callback;
    }
}
