﻿using System;

namespace PostStressTest.Messages
{
    public class OrderMessageResponse
    {
        public int userid { get; set; }
        public int scene { get; set; }
        public int timestamp { get; set; }
        public string jsontext { get; set; }
    }
}
