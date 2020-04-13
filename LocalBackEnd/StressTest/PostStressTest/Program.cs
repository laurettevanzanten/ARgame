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
   
    public class AgentTaskList : List<(CancellationTokenSource cancelSource, Task task)>
    {
    }

    class Program
    {
        static void Main(string[] args)
        {
            const int maxUsers = 10;
            const int maxTimeSeconds = 10;

            var log = new Log()
            {
                OutputSet = OutputChannel.Debug | OutputChannel.Memory
            };

            var ioc = new IoC()
                        .Register<Random>()
                        .Register<Log>(log);

            var agentTaskList = CreateTaskList(ioc, maxUsers);

            RunAgentTasks(agentTaskList, maxTimeSeconds);
            StopAgentTasks(agentTaskList);

            log.FlushCSV("stress-test.csv");
        }

        private static AgentTaskList CreateTaskList(IoC ioc, int agentCount)
        {
            var agentTaskList = new AgentTaskList();

            // create tasks for each user
            for (int i = 0; i < agentCount; i++)
            {
                var cancelSource = new CancellationTokenSource();
                var token = cancelSource.Token;
                var userName = "user" + (i + 1);
                var password = "pwd" + (i + 1);
                agentTaskList.Add((cancelSource,
                    Task.Run(async () =>
                    {
                        var agent = AgentFactory.CreateHttpMessageAgent(ioc, userName, password);
                        using (agent)
                        {
                            agent.Start();

                            while (agent.Phase == StateMachine.StatePhase.Started)
                            {
                                agent.Update();
                                await Task.Delay(100, token);
                            }
                        }
                        agent.Stop();
                    })));
            }

            return agentTaskList;
        }

        static void RunAgentTasks(
            AgentTaskList agentTaskList, 
            int maxTimeSeconds,
            int intervalMs = 10)
        {
            var startTime = DateTime.Now;

            while (agentTaskList.Count > 0 && (DateTime.Now - startTime).TotalSeconds < maxTimeSeconds)
            {
                for (int i = 0; i < agentTaskList.Count;)
                {
                    var agentTask = agentTaskList[i].task;

                    if (agentTask.Status == TaskStatus.Running || agentTask.Status == TaskStatus.WaitingForActivation)
                    {
                        i++;
                    }
                    else
                    {
                        agentTaskList.RemoveAt(i);
                    }
                }

                Thread.Sleep(intervalMs);
            }
        }

        static void StopAgentTasks(AgentTaskList agentTaskList)
        {
            while (agentTaskList.Count > 0)
            {
                for (int i = 0; i < agentTaskList.Count;)
                {
                    var agentTask = agentTaskList[i].task;
                    
                    if (agentTask.Status == TaskStatus.Running || agentTask.Status == TaskStatus.WaitingForActivation)
                    {
                        agentTaskList[i].cancelSource.Cancel();

                        i++;
                    }
                    else
                    {
                        agentTaskList.RemoveAt(i);
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}
