using System;
using System.Collections.Generic;
using System.Text;

namespace PostStressTest.Messages
{
    public class HeartbeatMessage
    {
        public string token { get; set; }
        public int timeStamp { get; set; }
    }
}
