using System;
using System.Collections.Generic;
using System.Text;

namespace PostStressTest.Messages
{
    public class LoginResponse
    {
        public string token { get; set; }

		public int session { get; set; }

        public int timeStamp { get; set; }
    }
}
