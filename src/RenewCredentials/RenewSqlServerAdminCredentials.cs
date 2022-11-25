using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Azure.ResourceManager.Sql.Models;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace RenewCredentials;

public class RenewSqlServerAdminCredentials
{
    [FunctionName("RenewSqlServerAdminCredentials")]
    public static void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer, ILogger log)
    {
        try
        {
            var secretClient = CreateSecretClient();
            var client = new ArmClient(new DefaultAzureCredential());
            var subscriptions = client.GetSubscriptions();
            foreach (var subscription in subscriptions)
            {
                log.LogInformation("Scanning subscription `{0}` ({1}) for SQL Servers", subscription.Data?.DisplayName,
                    subscription.Data?.SubscriptionId);
                var sqlServers = subscription.GetSqlServers();
                foreach (var sqlServer in sqlServers)
                    try
                    {
                        var password = Password.GeneratePassword();
                        var patch = new SqlServerPatch
                        {
                            AdministratorLoginPassword = password
                        };
                        
                        log.LogInformation("Updating password for SQL Server `{0}` ({1})", sqlServer.Data?.Name,
                            sqlServer.Data?.Id);
                        
                        sqlServer.Update(WaitUntil.Completed, patch);
                        log.LogInformation("Updated password for SQL Server `{0}` ({1})", sqlServer.Data?.Name,
                            sqlServer.Data?.Id);
                        
                        var secretName = sqlServer.Data?.Name + "-admin-password";
                        log.LogInformation("Updating secret `{0}` in Key Vault `{1}`", secretName, secretClient.VaultUri);
                        secretClient.SetSecret(secretName, password);
                        log.LogInformation("Updated secret `{0}` in Key Vault `{1}`", secretName, secretClient.VaultUri);
                    }
                    catch (Exception e)
                    {
                        log.LogError(e, "Error settings password for SQL Server `{0}` ({1})", sqlServer.Data?.Name,
                            sqlServer.Data?.Id);
                    }

                log.LogInformation("Scanned subscription `{0}` ({1}) for SQL Servers", subscription.Data?.DisplayName,
                    subscription.Data?.SubscriptionId);
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "Error renewing SQL Server admin credentials");
            throw;
        }
    }

    private static SecretClient CreateSecretClient()
        {
            // Get the KeyVault uri where secrets will be stored 
            var environmentValue =
                Environment.GetEnvironmentVariable("KEYVAULT_URI", EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(environmentValue))
                throw new InvalidOperationException(
                    "The application setting `KEYVAULT_URI` is missing, empty or only consists of spaces");

            var options = new SecretClientOptions
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                }
            };
            var secretClient = new SecretClient(new Uri(environmentValue), new DefaultAzureCredential(), options);
            return secretClient;
        }
}