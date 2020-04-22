using System;
using System.Collections.Generic;
using System.Text;

namespace PostStressTest.Messages
{
    public class GetUserOrdersMessage
    {
        public int userId { get; set; }

        public string adminName { get; set; }

        public string password { get; set; }
    }
}
