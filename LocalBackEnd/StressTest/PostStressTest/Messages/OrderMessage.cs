using System;

namespace PostStressTest.Messages
{
    public class OrderMessage
    {
        public string token { get; set; }
        public int sessionId { get; set; }
        public int timeStamp { get; set; }
        public Coordinate[] items { get; set; }

        public OrderMessage Randomize(Random rng)
        {
            items = GenerateRandomItems(rng);
            sessionId = rng.Next(0, 2);
            timeStamp = rng.Next(0, 900);
            return this;
        }

        public static OrderMessage GenerateRandomMessage(string userToken, Random rng)
        {
            return new OrderMessage()
            {
                token = userToken,
            }.Randomize(rng);
        }

        public static Coordinate[] GenerateRandomItems(Random rng)
        {
            var result = new Coordinate[rng.Next(1, 10)];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Coordinate()
                {
                    x = rng.Next(0, 20),
                    y = rng.Next(0, 5)
                };
            }

            return result;
        }
    }
}
