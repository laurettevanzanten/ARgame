using PostStressTest.Messages;
using PostStressTest.StateMachine;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace PostStressTest
{
    public static class AgentFactory 
    {
        public const string UserTokenId = "user-token";
        public const string UserNameId = "user-name";

        /// <summary>
        /// Creates an agent with a simple statemachine to login in (and retry if it fails), and send
        /// messages on a random interval.
        /// </summary>
        /// <param name="ioc"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static HttpMessageAgent CreateHttpMessageAgent(IoC ioc, string user, string password)
        {
            var messageAgent = new HttpMessageAgent();
            var rng = ioc.Obtain<Random>();

            var sendOrder = messageAgent.AddState<HttpMessageState>((s) =>
            {
                s.Name = "send order";
                s.Uri = "http://localhost:3000/post-session-db";
                s.Message = OrderMessage.GenerateRandomMessage(-1, rng);
                s.SetupMessage = (msg) =>
                {
                    var orderMessage = (OrderMessage)msg;

                    orderMessage.token = s.Context.Obtain<int>(UserTokenId);
                    orderMessage.Randomize(s.Context.Obtain<Random>());
                    Debug.WriteLine(s.Context.Obtain<string>(UserNameId) + "-" +
                        orderMessage.token + ": sending order with " + orderMessage.items.Length + " items.");
                };
                s.SendMethod = RestMethod.POST;
            });

            var login = messageAgent.AddState<HttpMessageState>((s) =>
            {
                s.Name = "login";
                s.Uri = "http://localhost:3000/login";
                s.Message = new LoginMessage()
                {
                    user = user,
                    password = password,
                };
                s.SetupMessage = (msg) => Debug.WriteLine("logging in user " + ((LoginMessage)s.Message).user);
                s.ProcessResponse = async (response) =>
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var message = await response.Content.ReadAsStringAsync();

                        if (!string.IsNullOrEmpty(message))
                        {
                            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(message);
                            var token = int.Parse(loginResponse.token);

                            Debug.WriteLine("user " + ((LoginMessage)s.Message).user + " received token " + token);

                            s.Context.Register(UserTokenId, token);
                        }
                    }
                };
                s.SendMethod = RestMethod.POST;
            });

            var delaySendOrder = messageAgent.AddState<RandomDelayState>((s) =>
            {
                s.MinDelayMs = 10;
                s.MaxDelayMs = 3000;
            });

            var delayTryRepeatLogin = messageAgent.AddState<RandomDelayState>((s) =>
            {
                s.MinDelayMs = 3000;
                s.MaxDelayMs = 6000;
            });

            messageAgent.CurrentState = login;

            messageAgent.AddStateTransition(login, () =>
            {
                // if failed to login, try again later
                return (login.RequestException != null || !login.Response.IsSuccessStatusCode)
                            ? (IState) delayTryRepeatLogin 
                            : (IState) sendOrder;
            });

            // done waiting after a failed login, try login again
            messageAgent.AddStateTransition(delayTryRepeatLogin, () => login);

            // sent an order, now wait for a bit
            messageAgent.AddStateTransition(sendOrder, () => delaySendOrder);

            // waited for a bit now send it again
            messageAgent.AddStateTransition(delaySendOrder, () => sendOrder);

            messageAgent.Initialize(ioc);

            messageAgent.AgentContext.Register(UserNameId, user);

            return messageAgent;
        }
    }
}
