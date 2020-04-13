using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PostStressTest.StateMachine
{
    public enum RestMethod
    {
        GET,
        POST
    };

    public class HttpMessageState : BasicState
    {
        public string Name { get; set; }

        public string Uri { get; set; }

        public object Message { get; set; }

        public RestMethod SendMethod { get; set; } 

        public Exception RequestException { get; private set; }

        public Action<HttpResponseMessage> ProcessResponse { get; set; }

        public Action<object> SetupMessage { get; set; }

        public HttpResponseMessage Response { get; set; }

        private Log _log;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _log = Context.Resolve<Log>();
        }

        protected override void OnStarted()
        {
            _log?.Put(OutputLevel.Info, Name, "Start send message.");

            Response = null;
            RequestException = null;
            SetupMessage?.Invoke(Message);
            SendMessage(Context.Resolve<HttpClient>());
        }

        public async void SendMessage(HttpClient client)
        {
            var json = JsonSerializer.Serialize(Message);

            try
            {
                switch (SendMethod)
                {
                    case RestMethod.GET:
                        Response = await client.GetAsync(Uri);
                        break;
                    case RestMethod.POST:
                        Response = await client.PostAsync(Uri, new StringContent(json, Encoding.UTF8, "application/json"));
                        break;
                }
            }
            catch (Exception e)
            {
                var message = e.ToString().Replace(",", " ").Replace("\n", " ").Replace("\r", " ");
                _log?.Put(OutputLevel.Error, Name, message);
                RequestException = e;
                Response = null;
            }
        }

        public override void Update()
        {
            if (Response != null)
            {
                ProcessResponse?.Invoke(Response);
                Stop();
            }
            else if (RequestException != null)
            {
                Stop();
            }
        }
    }
}
