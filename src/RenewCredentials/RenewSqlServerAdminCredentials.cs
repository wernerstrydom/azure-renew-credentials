using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace RenewCredentials
{
    public class RenewSqlServerAdminCredentials
    {
        [FunctionName("RenewSqlServerAdminCredentials")]
        public void Run([TimerTrigger("0 0 11 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
