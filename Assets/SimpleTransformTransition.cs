using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TransformKey
{
    public float lerpDuration = 1.0f;
    public Vector3 translation = Vector3.zero;
    public Vector3 scale = Vector3.one;
    public Vector3 rotation = Vector3.zero;
    
}

public class SimpleTransformTransition : MonoBehaviour
{
    public TransformKey[] keys;

    private RectTransform _targetTransform;

    private int _currentKeyIndex;
    private int _nextKeyIndex;
    private float _startTime;

    void Start()
    {
        _targetTransform = GetComponent<RectTransform>();

        _currentKeyIndex = 0;
        _nextKeyIndex = 1;
        _startTime = Time.time;
    }

    void Update()
    {
        var currentKey = keys[_currentKeyIndex];
        var nextKey = keys[_nextKeyIndex];

        var lerpTime = Mathf.Min(1.0f, (Time.time - _startTime) / Mathf.Max(0.001f, nextKey.lerpDuration));

        _targetTransform.position = Vector3.Lerp(currentKey.translation, nextKey.translation, Sigmoid(lerpTime * nextKey.lerpDuration, nextKey.lerpDuration));
        _targetTransform.localScale= Vector3.Lerp(currentKey.scale, nextKey.scale, Sigmoid(lerpTime * nextKey.lerpDuration, nextKey.lerpDuration));

        if (lerpTime >= 1.0f)
        {
            _currentKeyIndex = _nextKeyIndex;
            _nextKeyIndex = (_nextKeyIndex + 1) % keys.Length;
            _startTime = Time.time;
        }
    }

    float Sigmoid(float x, float duration)
    {
        var alpha = 16.0f / duration;
        var exponent = -alpha * x + 8;
        var result = 1.0f / (1 + Mathf.Exp(exponent));

        return result;
    }
}
