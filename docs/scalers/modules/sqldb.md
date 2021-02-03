# Bellhop for Azure SQL Database
This README will describe how to use Bellhop to help scale your Azure SQL Database Resources.

## Scaling Considerations
Azure SQL has a concept of a Service Objective which makes scaling, even between tiers pretty easy. **MORE INFORMATION NEEDED AROUND SCALING THIS SERVICE SPECIFICALLY, THIS IS JUST A PLACE HOLDER**

## Required Tag for Azure SQL Database
```
setState-RequestedServiceObjectiveName = <String>
```

## Optional Tags for Azure SQL Database
```
setState-LicenseType = <String>
setState-MaxSizeBytes = <Int32>
```

For more information on tag values for Azure SQL Databases please see the Microsoft documentaion: [Set-AzSqlDatabase](https://docs.microsoft.com/en-us/powershell/module/az.sql/set-azsqldatabase?view=azps-5.4.0)


## Sample message sent to Queue by Engine function
An example of the message sent to the queue by the engine function 

```
{
  "direction": "up",
  "graphResults": {
    "id": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Sql/servers/elastic-test/databases/sql-db",
    "name": "sql-db",
    "type": "microsoft.sql/servers/databases",
    "tenantId": "<TENANT-ID>",
    "kind": "v12.0,user",
    "location": "westus2",
    "resourceGroup": "<RESOURCE-GROUP-NAME>",
    "subscriptionId": "<SUBSCRIPTION-ID>",
    "managedBy": "",
    "sku": {
      "name": "Basic",
      "tier": "Basic",
      "capacity": 5
    },
    "plan": null,
    "properties": {
      "storageAccountType": "GRS",
      "collation": "SQL_Latin1_General_CP1_CI_AS",
      "maxSizeBytes": 104857600,
      "status": "Online",
      "databaseId": "<DATABASE-ID>",
      "creationDate": "2021-01-07T21:50:15.347Z",
      "currentServiceObjectiveName": "Basic",
      "requestedServiceObjectiveName": "Basic",
      "defaultSecondaryLocation": "westcentralus",
      "catalogCollation": "SQL_Latin1_General_CP1_CI_AS",
      "zoneRedundant": false,
      "earliestRestoreDate": "2021-01-20T00:00:00Z",
      "readScale": "Disabled",
      "readReplicaCount": 0,
      "currentSku": {
        "name": "Basic",
        "tier": "Basic",
        "capacity": 5
      }
    },
    "tags": {
      "saveState-Capacity": "100",
      "saveState-MaxSizeBytes": "53687091200",
      "resize-Enable": "True",
      "setState-Capacity": "5",
      "setState-Edition": "Basic",
      "setState-MaxSizeBytes": "104857600",
      "saveState-Edition": "Standard"
    },
    "identity": null,
    "zones": null,
    "extendedLocation": null,
    "ResourceId": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Sql/servers/elastic-test/databases/sql-db"
  }
}
```