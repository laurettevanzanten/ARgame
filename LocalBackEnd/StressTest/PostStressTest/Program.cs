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
   


    class Program
    {
        static void Main(string[] args)
        {
            const int maxUsers = 10;
            var ioc = new IoC().Register<Random>(new Random());
            var taskList = new List<(CancellationToken cancelToken, Task task)>();

            // create tasks for each user
            for (int i = 0; i < maxUsers; i++)
            {
                var token = new CancellationToken();
                var userName = "user" + (i + 1);
                var password = "pwd" + (i + 1);
                taskList.Add((token,
                    Task.Run(async () =>
                    {
                        using (var agent = AgentFactory.CreateHttpMessageAgent(ioc, userName, password))
                        {
                            agent.Start();

                            while (agent.Phase == StateMachine.StatePhase.Started)
                            {
                                agent.Update();
                                await Task.Delay(100, token);
                            }
                        }
                    })));
            }

            while (taskList.Count > 0)
            {
                for (int i = 0; i < taskList.Count;)
                {
                    var task = taskList[i].task;

                    if (task.Status == TaskStatus.Running || task.Status == TaskStatus.WaitingForActivation)
                    {
                        i++;
                    }
                    else
                    {
                        taskList.RemoveAt(i);
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}
