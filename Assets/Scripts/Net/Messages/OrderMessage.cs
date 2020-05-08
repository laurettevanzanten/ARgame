using System;

[Serializable]
public class OrderMessage
{
    public string token;
    public int sessionId;
    public float timeStamp;
    public CollectedItem[] items;

    public override string ToString()
    {
        return token + ", session " + sessionId + "( " + timeStamp + "), items: {" + string.Join<CollectedItem>(";", items) + "}.";
    }
}

