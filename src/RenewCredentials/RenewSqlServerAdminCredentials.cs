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

public class RenewSqlServerAdminCredentialsFunc
{
    [FunctionName("RenewSqlServerAdminCredentials")]
    public void Run(
        [QueueTrigger("sql-servers")] string id,
        [Queue("notifications")] ICollector<string> notifications,
        ILogger log)
    {
        var eventSource = new EventSource(log);

        var client = new ArmClient(new DefaultAzureCredential());
        var rid = ResourceIdentifier.Parse(id);
        var sqlServer = client.GetSqlServerResource(rid)?.Get()?.Value;
        if (sqlServer == null)
        {
            eventSource.SqlServerNotFound(id);
            return;
        }
        
        var password = Password.GeneratePassword();
        UpdateSqlServerAdminPassword(sqlServer, password, eventSource);
        UpdateSecrets(sqlServer, password, eventSource);
        notifications.Add($"Successfully renewed administrator password for SQL Server `{sqlServer.Data?.Name}` ({sqlServer.Data?.Id})");
    }

    private static void UpdateSqlServerAdminPassword(SqlServerResource sqlServer, string password,
        EventSource eventSource)
    {
        // Update the SQL Server admin password
        eventSource.RenewingSqlServerAdminLoginPassword(sqlServer);
        var patch = new SqlServerPatch
        {
            AdministratorLoginPassword = password
        };

        sqlServer.Update(WaitUntil.Completed, patch);
        eventSource.RenewedSqlServerAdminPassword(sqlServer);
    }

    private static void UpdateSecrets(SqlServerResource sqlServer, string password, EventSource eventSource)
    {
        var secretClient = CreateSecretClient();
        UpdateSqlServerAdminPasswordSecret(sqlServer, password, secretClient, eventSource);
        UpdateSqlServerAdminLoginSecret(sqlServer, secretClient, eventSource);
    }

    private static void UpdateSqlServerAdminPasswordSecret(SqlServerResource sqlServer,
        string password, SecretClient secretClient,
        EventSource eventSource)
    {
        var secretName = sqlServer.Data?.Name + "-administrator-login-password";

        if (!ShouldSetSecret(secretClient, secretName, password)) return;
        
        var secret = new KeyVaultSecret(secretName, password);
        secretClient.SetSecret(secret);
        eventSource.UpdatedSqlServerAdminPasswordSecret(secret);
    }

    private static void UpdateSqlServerAdminLoginSecret(SqlServerResource sqlServer, 
        SecretClient secretClient,
        EventSource eventSource)
    {
        var secretName = sqlServer.Data?.Name + "-administrator-login";
        var secretValue = sqlServer.Data?.AdministratorLogin;

        if (!ShouldSetSecret(secretClient, secretName, secretValue)) return;
        
        var secret = new KeyVaultSecret(secretName, secretValue);
        secretClient.SetSecret(secret);
        
        eventSource.UpdatedSqlServerAdminLoginSecret(secret);
    }

    private static bool ShouldSetSecret(SecretClient secretClient, string secretName, string newValue)
    {
        if (secretClient.TryGetSecretValue(secretName, out var value) == false) 
            return true;
        
        return value != newValue;
    }

    private static SecretClient CreateSecretClient()
    {
        // Get the KeyVault uri where secrets will be stored 
        var environmentValue = Environment.GetEnvironmentVariable("KEYVAULT_URI", EnvironmentVariableTarget.Process);

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