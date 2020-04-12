using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

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

        protected override void OnStarted()
        {
            Response = null;
            RequestException = null;
            SetupMessage?.Invoke(Message);
            SendMessage(Context.Obtain<HttpClient>());
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
            catch (HttpRequestException hte)
            {
                // log exception
                RequestException = hte;
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
