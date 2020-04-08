using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using System.Threading;
using System.Collections.Generic;


/// <summary>
/// Simple stress test program for the back-end server
/// </summary>
namespace PostStressTest
{
    public class Coordinate
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    public class JsonMessage
    {
        public string user { get; set; }
        public string password { get; set; }
        public int sessionId { get; set; }
        public int timeStamp { get; set; } 
        public Coordinate[] items { get; set; }

        public static JsonMessage GenerateRandomMessage(Random rng)
        {
            var id = rng.Next(1, 10);
            return new JsonMessage()
            {
                user = "user" + id,
                password = "pwd" + id,
                items = GenerateRandomItems(rng),
                sessionId = rng.Next(0, 2),
                timeStamp = rng.Next(0, 900)
            };
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

    class Program
    {
        static void Main(string[] args)
        {
            var startTime = DateTime.Now;
            var rng = new Random();
            var taskList = new List<Task>();

            using (var client = new HttpClient())
            {
                while ((DateTime.Now - startTime).TotalSeconds < 15.0f)
                {
                    var json = JsonSerializer.Serialize(JsonMessage.GenerateRandomMessage(rng));
                    var task = Post(client, "http://localhost:3000/post-session-db", json);

                    taskList.Add(task);
                    Thread.Sleep(rng.Next(1, 10));
                }

                for (int i = 0; i < taskList.Count; i++ )
                {
                    if (taskList[i].Status != TaskStatus.RanToCompletion)
                    {
                        taskList[i].Wait();
                    }
                }   
            }
        }

        private static async Task<HttpResponseMessage> Post(HttpClient client, string uri, string json)
        {
            return await client.PostAsync( uri,
                    new StringContent(json, Encoding.UTF8, "application/json"));
        
        }
    }
}
