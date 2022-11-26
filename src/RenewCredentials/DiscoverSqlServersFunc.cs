using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace RenewCredentials;

public class DiscoverSqlServersFunc
{
    [FunctionName("DiscoverSqlServers")]
    public void Run(
        [TimerTrigger("0 0 19 * * *")] TimerInfo timer,
        [Queue("sql-servers")] ICollector<string> servers,
        ILogger log)
    {
        var client = new ArmClient(new DefaultAzureCredential());
        var subscriptions = client.GetSubscriptions();
        foreach (var subscription in subscriptions)
        {
            var subscriptionName = subscription.Data?.DisplayName;
            var subscriptionId = subscription.Data?.SubscriptionId;

            log.LogInformation("Scanning subscription `{SubscriptionName}` ({SubscriptionId}) for SQL Servers",
                subscriptionName, subscriptionId);

            var sqlServers = subscription.GetSqlServers();
            foreach (var sqlServer in sqlServers)
            {
                var sqlServerName = sqlServer.Data?.Name;
                var sqlServerId = sqlServer.Data?.Id;
                log.LogInformation("Found SQL Server subscription `{SqlServerName}` ({SqlServerId}) for SQL Servers",
                    sqlServerName, sqlServerId);
                servers.Add(sqlServerId);
            }

            log.LogInformation("Scanned subscription `{SubscriptionName}` ({SubscriptionId}) for SQL Servers",
                subscriptionName, subscriptionId);
        }
    }
}