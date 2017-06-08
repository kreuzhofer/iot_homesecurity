using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;


namespace FunctionApp
{
    public static class LogFunction
    {
        [FunctionName("EventHubTriggerCSharp")]
        public static void Run([EventHubTrigger("logeventhub", Connection = "logeventhub")]string myEventHubMessage, TraceWriter log)
        {
            log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");
        }
    }
}