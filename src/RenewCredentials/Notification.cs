using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace RenewCredentials;

public class Notification
{
    [FunctionName("Notification")]
    public void Run([QueueTrigger("notifications", Connection = "AzureWebJobsStorage")] string notification,
        ILogger log)
    {
        log.LogInformation($"C# Queue trigger function processed: {notification}");
    }
}