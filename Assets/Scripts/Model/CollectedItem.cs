using System;
using UnityEngine;

[Serializable]
public class CollectedItem
{
    public Vector2Int pos;
    public float ts;

    public string ToJson()
    {
        return "{ \"x\":" + pos.x + ", \"y\":" + pos.y + ", \"ts\": " + (ts).ToString() + "}";
    }

    public override string ToString()
    {
        return "x:" + pos.x + ", y:" + pos.y + ", ts: " + (ts).ToString();
    }
}

