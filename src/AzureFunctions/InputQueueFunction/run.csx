#r "Newtonsoft.Json"
using System;
using Newtonsoft.Json;

public static void Run(string myEventHubMessage, TraceWriter log)
{
    log.Info($"C# Event Hub trigger function processed a message: {myEventHubMessage}");

    dynamic messageObj = JsonConvert.DeserializeObject(myEventHubMessage);

    if(messageObj.MessageType == "Log") // this should be the case, we have set up a route for it
    {
        log.Error("Log message received.");
        if(messageObj.Severity == "Critial")
        {
            log.Info("Critial message! Forwarding to critial queue");
        }
    }

}