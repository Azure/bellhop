# Bellhop for Azure SQL Database
This README will describe how to use Bellhop to help scale your Azure SQL Database Resources.

## Scaling Considerations

- Please refer to [this](https://docs.microsoft.com/en-us/azure/azure-sql/database/single-database-scale) document for considerations when scaling an Azure SQL Database resource between service tiers.

## Required Tag for Azure SQL Database
```
setState-RequestedServiceObjectiveName = <String>
```

vCore Service Objectives can be found in the `Compute Size` tables: [HERE](https://docs.microsoft.com/en-us/azure/azure-sql/database/resource-limits-vcore-single-databases)


DTU Service Objectives can be found in the `Compute Size` tables: [HERE](https://docs.microsoft.com/en-us/azure/azure-sql/database/resource-limits-dtu-single-databases)

## Optional Tags for Azure SQL Database
```
setState-LicenseType = <String>
setState-MaxSizeBytes = <Int32>
```

For more information on tag values for Azure SQL Databases please see the Microsoft documentaion: [Set-AzSqlDatabase](https://docs.microsoft.com/en-us/powershell/module/az.sql/set-azsqldatabase?view=azps-5.4.0)


## Sample Message Sent to Queue by Engine Function
An example of the message sent to the queue by the engine function 

**SQL Database Message**
```
{
    "debug": false,
    "direction": "up",
    "tagMap": {
        "enable": "resize-Enable",
        "start": "resize-StartTime",
        "end": "resize-EndTime",
        "set": "setState-",
        "save": "saveState-"
    },
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
            "resize-Enable": "True",
            "resize-StartTime": "Friday 7PM",
            "resize-EndTime": "Monday 6AM",
            "setState-RequestedServiceObjectiveName": "S4",
            "setState-LicenseType": "LicenseIncluded",
            "setState-MaxSizeBytes": "104857600"
        },
        "identity": null,
        "zones": null,
        "extendedLocation": null,
        "ResourceId": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Sql/servers/elastic-test/databases/sql-db"
    }
}
```
