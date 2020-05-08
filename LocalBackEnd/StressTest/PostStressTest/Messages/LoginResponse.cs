namespace PostStressTest.Messages
{
    public class LoginResponse
    {
        public string token { get; set; }

		public int session { get; set; }

        public float timeStamp { get; set; }
    }
}
