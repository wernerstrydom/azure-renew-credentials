using System;
using System.Threading.Tasks;
using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.Sql.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace RenewCredentials
{
    public class RenewSqlServerAdminCredentials
    {
        [FunctionName("RenewSqlServerAdminCredentials")]
        public static void Run([TimerTrigger("0 0 11 * * *")] TimerInfo timer, ILogger log)
        {
            var armClient = new ArmClient(new DefaultAzureCredential());
            var subscriptions = armClient.GetSubscriptions();
            foreach (var subscription in subscriptions)
            {
                var sqlServers = subscription.GetSqlServers();
                foreach (var sqlServer in sqlServers)
                {
                    log.LogInformation($"Server: {sqlServer.Data.Name} - Admin: {sqlServer.Data.AdministratorLogin}");
                }
            }
        }
    }
}
