using PostStressTest.Messages;
using PostStressTest.StateMachine;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PostStressTest
{
    public static class AgentFactory
    {
        public const string UserTokenId = "user-token";
        public const string UserNameId = "user-name";
        public const string MessageCountId = "message-count";
        public const string AckCountId = "ack-count";

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

            var rng = ioc.Resolve<Random>();
            var log = ioc.Resolve<Log>();

            var sendOrder = messageAgent.AddState<HttpMessageState>((s) => ConfigureSendOrderState(s, log, rng, user));
            var login = messageAgent.AddState<HttpMessageState>((s) => ConfigureLoginState(s, log, user, password));
            var logout = messageAgent.AddState<HttpMessageState>((s) => ConfigureLogoutState(s, log, user));

            var loggedOutState = messageAgent.AddState<RandomDelayState>((s) => ConfigureRandomDelayState(s, 10, 3000));
            var delaySendOrder = messageAgent.AddState<RandomDelayState>((s) => ConfigureRandomDelayState(s, 10, 3000));
            
            messageAgent.CurrentState = loggedOutState;

            // done waiting after being logged out, try login again
            messageAgent.AddStateTransition(loggedOutState, () => login);

            messageAgent.AddStateTransition(login, () =>
            {
                // if failed to login, try again later
                if (login.RequestException != null || login.Response == null)
                {
                    // task got canceled - main app is stopping, return null, ending the agent's lifecycle
                    if (login.RequestException is TaskCanceledException)
                    {
                        return null;
                    }
                    else
                    {
                        // exception has been logged - server might not be up, try later
                        return loggedOutState;
                    }
                }
                else if (!login.Response.IsSuccessStatusCode)
                {
                    log?.Put(OutputLevel.Error, login.Name,
                                    "login was not successfull, " + login.Response.ReasonPhrase);
                    return loggedOutState;
                }
                else
                {
                    // generally speaking start sending orders, but add a small chance to log out again
                    if (rng.NextDouble() > 0.2)
                    {
                        return sendOrder;
                    }
                    else
                    {
                        return logout;
                    }
                }
            });


            // sent an order, now wait for a bit
            messageAgent.AddStateTransition(sendOrder, () =>
            {
                if (sendOrder.RequestException != null)
                {
                    // task got canceled - main app is stopping, return null, ending the agent's lifecycle
                    if (sendOrder.RequestException is TaskCanceledException)
                    {
                        return null;
                    }
                }

                // generally speaking start sending orders, but add a small chance to log out again
                if (rng.NextDouble() > 0.2)
                {
                    return delaySendOrder;
                }
                else
                {
                    return logout;
                }                
            });

            // waited for a bit now send it again
            messageAgent.AddStateTransition(delaySendOrder, () => sendOrder);

            // when done logging out, go back to logging in
            messageAgent.AddStateTransition(logout, () => 
            {
                if (login.RequestException != null || login.Response == null)
                {
                    // task got canceled - main app is stopping, return null, ending the agent's lifecycle
                    if (login.RequestException is TaskCanceledException)
                    {
                        return null;
                    }
                }
                return loggedOutState;
            });

            messageAgent.Initialize(ioc);

            messageAgent.AgentContext.Register(UserNameId, user);
            messageAgent.AgentContext.Register(MessageCountId, 0);
            messageAgent.AgentContext.Register(AckCountId, 0);

            return messageAgent;
        }


        private static HttpMessageState ConfigureLoginState(HttpMessageState s, Log log, string user, string password)
        {
            s.Name = user + ", HttpMessageState::login";
            s.Uri = "http://localhost:3000/login";
            s.Message = new LoginMessage()
            {
                user = user,
                password = password,
            };
            s.SetupMessage = (msg) => log?.Put(OutputLevel.Info, s.Name, "logging in user " + ((LoginMessage)s.Message).user);
            s.ProcessResponse = async (response) =>
            {
                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(message))
                    {
                        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(message);

                        log?.Put(OutputLevel.Info, s.Name, " received token " + loginResponse.token);

                        s.Context.Register(UserTokenId, loginResponse.token);
                    }
                }
            };
            s.SendMethod = RestMethod.POST;
            return s;
        }

        private static HttpMessageState ConfigureSendOrderState(HttpMessageState s, Log log, Random rng, string user)
        {
            var orderMessage = OrderMessage.GenerateRandomMessage("-1", rng);

            s.Name = user + ", HttpMessageState::sendOrder";
            s.Uri = "http://localhost:3000/post-order";
            s.Message = orderMessage;
            s.SetupMessage = (msg) =>
            {
                orderMessage.token = s.Context.Resolve<string>(UserTokenId);
                orderMessage.Randomize(s.Context.Resolve<Random>());

                s.Context.Register(MessageCountId, s.Context.Resolve<int>(MessageCountId) + 1);

                log?.Put(OutputLevel.Info, s.Name, "sending order with " + orderMessage.items.Length + " items.");
            };
            s.ProcessResponse = async (response) =>
            {
                if (response.IsSuccessStatusCode)
                {
                    var message = await response.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(message))
                    {
                        var serverResponse = JsonSerializer.Deserialize<ServerResponse>(message);

                        if (serverResponse.timeStamp == orderMessage.timeStamp)
                        {
                            s.Context.Register(AckCountId, s.Context.Resolve<int>(AckCountId) + 1);
                        }
                    }
                }
            };
            s.SendMethod = RestMethod.POST;
            return s;
        }

        private static HttpMessageState ConfigureLogoutState(HttpMessageState s, Log log, string user)
        {
            var logoutMessage = new LogoutMessage();

            s.Name = user + ", HttpMessageState::logout";
            s.Uri = "http://localhost:3000/logout";
            s.Message = logoutMessage;
            s.SetupMessage = (msg) =>
            {
                logoutMessage.token = s.Context.Resolve<string>(UserTokenId);
                log?.Put(OutputLevel.Info, s.Name, "logging out" + user + ".");
            };
            s.ProcessResponse = (response) =>
            {
                if (response.IsSuccessStatusCode)
                {
                    s.Context.Remove(UserTokenId);
                }
            };
            s.SendMethod = RestMethod.POST;
            return s;
        }

        private static RandomDelayState ConfigureRandomDelayState(RandomDelayState s, int min, int max)
        {
            s.MinDelayMs = min;
            s.MaxDelayMs = max;
            return s;
        }
    }
}