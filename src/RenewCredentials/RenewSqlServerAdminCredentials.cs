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
using Humanizer;
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
                {
                    var password = Password.GeneratePassword();
                    if (TrySetAdministratorLoginPassword(sqlServer, password, log))
                    {
                        var secretName = nameof(sqlServer.Data.AdministratorLoginPassword).Dasherize().ToLower();
                        SaveSecret(secretClient, secretName, password, log);
                        secretName = nameof(sqlServer.Data.AdministratorLogin).Dasherize().ToLower();
                        SaveSecret(secretClient, secretName, sqlServer.Data?.AdministratorLogin, log);
                    }
                }

                log.LogInformation("Scanned subscription `{0}` ({1}) for SQL Servers", subscription.Data?.DisplayName,
                    subscription.Data?.SubscriptionId);
            }
        }
        catch (Exception e)
        {
            log.LogError(e, "Error refreshing SQL Server admin credentials\n\n{0}", e);
            throw;
        }
    }

    private static void SaveSecret(SecretClient client, string name, string value, ILogger log)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (log == null) throw new ArgumentNullException(nameof(log));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        try
        {
            log.LogInformation("Updating secret `{0}` in Key Vault `{1}`", name, client.VaultUri);
            client.SetSecret(name, value);
            log.LogInformation("Updated secret `{0}` in Key Vault `{1}`", name, client.VaultUri);
        }
        catch (Exception e)
        {
            log.LogError(e, "Error updating secret `{0}` in Key Vault `{1}`\n\n{2}", name, client.VaultUri, e);
        }
    }

    private static bool TrySetAdministratorLoginPassword(SqlServerResource sqlServer, string password, ILogger log)
    {
        var data = sqlServer.Data;
        var name = data?.Name;
        var id = data?.Id;
        try
        {
            log.LogInformation("Updating password for SQL Server `{0}` ({1})", name, id);

            var patch = new SqlServerPatch
            {
                AdministratorLoginPassword = password
            };
            
            sqlServer.Update(WaitUntil.Completed, patch);
            log.LogInformation("Updated password for SQL Server `{0}` ({1})", name, id);
            return true;
        }
        catch (Exception e)
        {
            log.LogError(e, "Error updating password for SQL Server `{0}` ({1})\n\n{2}", name, id, e);
            return false;
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