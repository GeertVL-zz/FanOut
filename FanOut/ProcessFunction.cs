using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FanOut
{ 
    public static class ProcessFunction
    {
        public static string[] Names = new string[] { "Donald", "Mickey", "Goofy", "Pluto", "Brutus" };

        [FunctionName("ProcessFunction")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
        {
            log.LogWarning("Entering orchestrator");
            var parallelTasks = new List<Task<string>>();

            for (int i = 0; i < Names.Length; i++)
            {
                Task<string> task = context.CallActivityAsync<string>("ProcessFunction_Hello", Names[i]);
                parallelTasks.Add(task);
            }

            await Task.WhenAll(parallelTasks);
            log.LogWarning("Leaving orchestrator");
        }

        [FunctionName("ProcessFunction_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogError($"Saying hello to {name}.");
            Thread.Sleep(2000);
            return $"Hello {name}!";
        }

        [FunctionName("ProcessFunction_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("ProcessFunction", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            log.LogWarning("I AM DONE");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}