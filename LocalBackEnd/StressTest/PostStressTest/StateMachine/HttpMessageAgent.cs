using System;
using System.Net.Http;

namespace PostStressTest.StateMachine
{
    public class HttpMessageAgent : BasicAgent, IDisposable
    {
        private HttpClient _client;

        protected override void InitializeAgentContext()
        {
            _client = new HttpClient();
            AgentContext.Register<HttpClient>(_client);
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
        }
    }
}
