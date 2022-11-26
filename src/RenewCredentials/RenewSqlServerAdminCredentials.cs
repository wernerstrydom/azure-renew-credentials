using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace RenewCredentials
{
    public class RenewSqlServerAdminCredentialsFunc
    {
        [FunctionName("RenewSqlServerAdminCredentials")]
        public void Run(
            [QueueTrigger("sql-servers", Connection = "AzureWebJobsStorage")]string data, 
            [Queue("sql-servers", Connection = "AzureWebJobsStorage")] out string notification, 
            ILogger log)
        {
            notification = $"Processed {data}";
            log.LogInformation($"C# Queue trigger function processed: {data}");
        }
    }
}
