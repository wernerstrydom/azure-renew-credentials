using System;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace RenewCredentials;

public static class KeyVaultExtensions
{
    public static bool TryGetSecretValue(this SecretClient client, string name, out string value)
    {
        value = string.Empty;
        try
        {
            var secret = client.GetSecret(name);
            if (!secret.HasValue)
                return false;

            value = secret.Value.Value;
            return true;
        }
        catch (RequestFailedException ex)
        {
            if (ex.ErrorCode != "SecretNotFound")
                throw;

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