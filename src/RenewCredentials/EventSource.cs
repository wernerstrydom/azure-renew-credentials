using System;
using Azure.ResourceManager.Sql;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;

namespace RenewCredentials;

public class EventSource
{
    private readonly ILogger _logger;

    public EventSource(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void UpdatedSqlServerAdminLoginSecret(KeyVaultSecret secret)
    {
        _logger.LogInformation("Successfully set secret `{SecretName}`", secret.Name);
    }
    
    public void UpdatedSqlServerAdminPasswordSecret(KeyVaultSecret secret)
    {
        _logger.LogInformation("Successfully set secret `{SecretName}`", secret.Name);
    }


    public void RenewingSqlServerAdminLoginPassword(SqlServerResource sqlServerResource)
    {
        var name = sqlServerResource.Data?.Name;
        var id = sqlServerResource.Data?.Id;
        _logger.LogTrace("Renewing password for SQL Server `{SqlServerName}` ({SqlServerId})", name, id);
    }

    public void RenewSqlServerAdminPasswordFailed(SqlServerResource sqlServerResource, Exception e)
    {
        var name = sqlServerResource.Data?.Name;
        var id = sqlServerResource.Data?.Id;
        _logger.LogError(e,
            "Error renewing administrator password for SQL Server `{SqlServerName}` ({SqlServerId})\n\n{Exception}",
            name, id, e);
    }

    public void RenewedSqlServerAdminPassword(SqlServerResource sqlServerResource)
    {
        var name = sqlServerResource.Data?.Name;
        var id = sqlServerResource.Data?.Id;
        _logger.LogInformation("Renewed administrator password for SQL Server `{SqlServerName}` ({SqlServerId})", name,
            id);
    }

    public void SqlServerNotFound(string id)
    {
        _logger.LogWarning("SQL Server with ID `{SqlServerId}` not found", id);
    }
}