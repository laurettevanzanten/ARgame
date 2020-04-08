using System;

[Serializable]
public class OrderMessage
{
    public string user;
    public string password;
    public int sessionId;
    public int timeStamp;
    public CollectedItem[] items;

    public override string ToString()
    {
        return user + "/" + password + ", session " + sessionId + "( " + timeStamp + "), items: {" + string.Join<CollectedItem>(";", items) + "}.";
    }
}

