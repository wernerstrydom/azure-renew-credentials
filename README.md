# Azure Renew Credentials

## Why?

Credential disclosure happens and it's often a challenge to manage. This
project consists of a suite of Azure Functions that runs at 2am every day
which would renew the following credentials and store then in Key Vault:

* SQL Server

This has the follwoing consequences:

* It discourages the use of the SQL Server admin credentials for day to day use
* If staff has used the credentials, and then leaves the organization, the credenials will be changed according to company policy
* If credentials are disclosed, there's a short window in which attackers can use it to cause damage.
* Credentials are stored in a location that is governed by access controls
