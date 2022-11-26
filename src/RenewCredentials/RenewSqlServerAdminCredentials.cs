using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace RenewCredentials
{
    public class RenewSqlServerAdminCredentialsFunc
    {
        [FunctionName("RenewSqlServerAdminCredentials")]
        [return: Queue("notifications")]
        public string Run(
            [QueueTrigger("sql-servers", Connection = "AzureWebJobsStorage")]string data,
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {data}");
            return $"Processed {data}";
        }
    }
}
