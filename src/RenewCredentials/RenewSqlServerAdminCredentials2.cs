using System;
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

/// <summary>
///     Represents a function that renews SQL Server Administrator credentials.
/// </summary>
/// <remarks>
///     One of the challenges of using Azure SQL Database is that the administrator credentials are not automatically
///     renewed.
///     This function will renew the administrator credentials for all SQL Server instances in a subscription, and save the
///     new credentials to a Key Vault.
/// </remarks>
public class RenewSqlServerAdminCredentials2
{
    [FunctionName("RenewSqlServerAdminCredentials2")]
    public static void Run([TimerTrigger("0 0 11 * * *")] TimerInfo timer, ILogger log)
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
                    var sqlServerName = sqlServer.Data?.Name;
                    var sqlServerId = sqlServer.Data?.Id;

                    log.LogInformation($"Renewing password for SQL Server `{sqlServerName}` ({sqlServerId})");

                    var password = Password.GeneratePassword();
                    if (!TrySetAdministratorLoginPassword(sqlServer, password, log))
                        continue;

                    var secretName = sqlServerName + "-administrator-login-password";
                    SetSecretValue(secretClient, secretName, password, log);

                    secretName = sqlServerName + "-administrator-login";
                    SetSecretValue(secretClient, secretName, sqlServer.Data?.AdministratorLogin, log);
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

    private static void SetSecretValue(SecretClient secretClient, string secretName, string password, ILogger log)
    {
        bool ret;
        try
        {
            var shouldSet = secretClient.TryGetSecretValue(secretName, out var value) == false || value != password;
            if (!shouldSet)
            {
                ret = true;
            }
            else
            {
                var secret = new KeyVaultSecret(secretName, password);
                secretClient.SetSecret(secret);
                ret = true;
            }
        }
        catch (RequestFailedException ex)
        {
            ret = false;
        }

        if (ret)
            log.LogInformation("Successfully set secret `{0}`", secretName);
        else
            log.LogError("Failed to set secret `{0}`", secretName);
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