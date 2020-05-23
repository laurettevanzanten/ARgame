using System;

[Serializable]
public class OrderMessage
{
    public string token;
    public int scene;
    public float timeStamp;
    public CollectedItem[] items;

    public override string ToString()
    {
        return token + ", scene " + scene + "( " + timeStamp + "), items: {" + string.Join<CollectedItem>(";", items) + "}.";
    }
}

