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
            log.LogInformation("Password: " + GeneratePassword());

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
                        // var password = GeneratePassword();
                        // var patch = new SqlServerPatch
                        // {
                        //     AdministratorLoginPassword = password
                        // };
                        //
                        // sqlServer.Update(WaitUntil.Completed, patch);
                        log.LogInformation("Updated password for SQL Server `{0}` ({1})", sqlServer.Data?.Name,
                            sqlServer.Data?.Id);
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

        private static string GeneratePassword(
            string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+{}|[]\\<>?/.,",
            int length = 32)
        {
            var bytes = new byte[length * 4];
            using var generator = RandomNumberGenerator.Create();
            generator.GetBytes(bytes);

            var result = Convert(bytes, alphabet);
            return result.Substring(0, length);
        }

        private static string Convert(byte[] bytes, string alphabet)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (alphabet == null) throw new ArgumentNullException(nameof(alphabet));
            if (bytes.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(bytes));
            if (string.IsNullOrWhiteSpace(alphabet))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(alphabet));

            var builder = new StringBuilder();

            var l = new BigInteger(alphabet.Length);
            var zero = new BigInteger(0);
            var n = new BigInteger(bytes);
            while (n != zero)
            {
                n = BigInteger.DivRem(n, l, out var remainder);
                builder.Insert(0, alphabet[(int)remainder]);
            }
            return builder.ToString();
        }
}