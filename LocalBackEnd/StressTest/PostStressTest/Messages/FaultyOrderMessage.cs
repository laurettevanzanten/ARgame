using System;
using System.Collections.Generic;
using System.Text;

namespace PostStressTest.Messages
{
    public class FaultyOrderMessage
    {
        public string token { get; set; }
        public string sessionId { get; set; }
        public string timeStamp { get; set; }

        public FaultyOrderMessage Randomize(Random rng)
        {
            sessionId = rng.NextDouble() > 0.5 ? "session" : "42";
            timeStamp = rng.NextDouble() > 0.5 ? "timeStamp" : "42";
            return this;
        }
    }
}
