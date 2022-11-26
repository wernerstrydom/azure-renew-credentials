# Azure Renew Credentials

## About the Project

Information disclosure is a serious problem that can happen in many ways. Information disclosure occurs when someone accidentally or deliberately releases information about an entity, like a person, business, or organization, to the public without permission. It could be as innocent as taking note of someone's address and posting it online. Or it could be something more sinister, like hacking into SQL Server Admin accounts and releasing confidential data.

There’s also been a few incidents where advisories obtained privileged credentials and then used them to undermine the business. There’s a short window between when secrets are disclosed and when it’s used. This typically takes a few days. You can reduce the risk of that event by refreshing credentials frequently. 

This project aims to refresh credentials automatically within that window. 

## Process

This project is a suite of Azure Functions that will renew, refresh or rotate credentials, and store them in Azure Key Vault.  The process is something like this:

1. Every day at 19:00 UTC, the `DiscoverSqlServers` function scans subscriptions for SQL Servers and adds them to a `sql-servers` queue. 
2. The `RenewSqlServerAdminCredentials` function gets triggered when items are added to the `sql-servers` queue. It generates a new password, updates the SQL Server administrator password, and stores it in a vault of your choice. If the server is named `myserver`, two secrets will be created/updated, namely `myserver-administrator-login-password` and `myserver-administrator-login`. A notification is added to a queue.
3. The `Notification` function reads the notification queue and sends a slack message, informing folks that the SQL Server admin credentials were refreshed. 

## Benefits

One of the primary benefits of refreshing credentials is that it discourages using them. For example, an employee could share the credentials with another over Slack, Teams, or other means. Later, that password is refreshed, and folks must get the credentials again. Do this frequently enough, and folks will abandon using these credentials. Credentials checked into source control will be of no consequence.

Another benefit is when folks who had access to the credentials move on, the credentials will be refreshed, thus ensuring compliance and reducing risks that the employee may use it to do damage. Just remember to do this after revoking their access to the vault.

## Usage

Probably the best way to use these functions is to fork the repo and then deploy it from your repository. A simple way is to use the portal to create a function app with source control integration. You’ll need to create two application settings:

1. `KEYVAULT_URI` — the URI of the key vault where secrets should be stored
2. `SlackEndpoint` (optional) — the URI of the Slack channel where notifications should be sent. A warning will be logged if this is missing, but the function will succeed.

You’ll also need to attach an identity to the function app with access to subscriptions and SQL Servers. 

## Contribution

Want credentials to be refreshed for other Azure resources? Create an issue, and let’s see if we can make it happen.

