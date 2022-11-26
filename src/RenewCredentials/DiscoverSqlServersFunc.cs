using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace RenewCredentials
{
    public class DiscoverSqlServersFunc
    {
        [FunctionName("DiscoverSqlServers")]
        public void Run(
            [TimerTrigger("0 */5 * * * *")]TimerInfo timer, 
            [Queue("sql-servers", Connection = "AzureWebJobsStorage")] ICollector<string> servers, 
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executing at: {DateTime.Now}");
            servers.Add("server1");   
            servers.Add("server2");
            servers.Add("server3");
            servers.Add("server4");         
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
