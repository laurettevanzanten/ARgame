using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class OrderMessage
{
    public string user;
    public string password;
    public int sessionId;
    public int timeStamp;
    public CollectedItem[] items;
}

