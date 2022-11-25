using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Sql;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace RenewCredentials;

public class RenewSqlServerAdminCredentials
{
    [FunctionName("RenewSqlServerAdminCredentials")]
    public static void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timer, ILogger log)
    {
        //var secretClient = CreateSecretClient();
        //KeyVaultSecret secret = secretClient.SetSecret("sqladmin", "P@ssw0rd1234");

        log.LogInformation("Password: " + GeneratePassword());
        
        var client = new ArmClient(new DefaultAzureCredential());
        var subscriptions = client.GetSubscriptions();
        foreach (var subscription in subscriptions)
        {
            log.LogInformation("Scanning subscription `{0}` ({1}) for SQL Servers", subscription.Data?.DisplayName,
                subscription.Data?.SubscriptionId);
            var sqlServers = subscription.GetSqlServers();
            foreach (var sqlServer in sqlServers)
                log.LogInformation($"Server: {sqlServer.Data.Name} - Admin: {sqlServer.Data.AdministratorLogin}");
            log.LogInformation("Scanned subscription `{0}` ({1}) for SQL Servers", subscription.Data?.DisplayName,
                subscription.Data?.SubscriptionId);
        }
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

    private static string GeneratePassword(string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+{}|[]\\<>?/.,", int length = 64)
    {
        var bytes = new byte[length*2];
        using var generator = RandomNumberGenerator.Create();
        generator.GetBytes(bytes);

        var builder = new StringBuilder();

        var l = new BigInteger(alphabet.Length);
        var zero = new BigInteger(0);
        var n = new BigInteger(bytes);
        while (n != zero)
        {
            var index = n % l;
            builder.Append(alphabet[(int) index]);
            n = n / l;
        }

        return builder.ToString();
    }
}