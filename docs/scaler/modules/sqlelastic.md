# Bellhop for Azure SQL Elastic Pools
This README will describe how to use Bellhop to help scale your Azure SQL ELastic Pool Resources.

## Scaling Considerations

- Please refer to [this](https://docs.microsoft.com/en-us/azure/azure-sql/database/elastic-pool-scale) document for considerations when scaling an Azure SQL Elastic Pool resource between service tiers.

## Required Tags for DTU Type
```
setState-Edition = <String>
setState-Dtu = <Int32>
```

Supported DTU Editions:
- Basic
- Standard
- Premium

## Optional Tags for DTU Type
```
setState-DatabaseDtuMin = <Int32>
setState-DatabaseDtuMax = <Int32>
setState-StorageMB = <Int32>
```

## Required Tags for VCore Type
```
setState-ComputeGeneration = <String>
setState-Edition = <String>
setState-VCore = <Int32>
```

Supported vCore Editions:
- GeneralPurpose
- BusinessCritical

vCore Compute Generations can be found in the `Compute Generation` tables: [HERE](https://docs.microsoft.com/en-us/azure/azure-sql/database/resource-limits-vcore-single-databases)

## Optional Tags for VCore Type
```
setState-DatabaseCapacityMin = <Double>
setState-DatabaseCapacityMax = <Double>
setState-StorageMB = <Int32>
setState-LicenseType = <String>
```

For more information on tag values for Azure SQL Elastic pools please see the Microsoft documentaion: [Set-AzSqlElasticPool](https://docs.microsoft.com/en-us/powershell/module/az.sql/set-azsqlelasticpool?view=azps-5.4.0)


## Sample Message Sent to Queue by Engine Function
An example of the message sent to the queue by the engine function.

**SQL ELastic pool Message**
```
{
    "debug": false,
    "direction": "down",
    "graphResults": {
        "id": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Sql/servers/elastic-test/elasticpools/sql-test",
        "name": "sqlelastic",
        "type": "microsoft.sql/servers/elasticpools",
        "tenantId": "<TENANT-ID>",
        "kind": "vcore,pool",
        "location": "westus2",
        "resourceGroup": "<RESOURCE-GROUP-NAME>",
        "subscriptionId": "<SUBSCRIPTION-ID>",
        "managedBy": "",
        "sku": {
            "name": "GP_Gen5",
            "tier": "GeneralPurpose",
            "capacity": 4,
            "family": "Gen5"
        },
        "plan": null,
        "properties": {
            "state": "Ready",
            "zoneRedundant": false,
            "creationDate": "2020-11-16T23:36:35.687Z",
            "maxSizeBytes": 68719476736,
            "licenseType": "BasePrice",
            "perDatabaseSettings": {
                "minCapacity": 2.0,
                "maxCapacity": 4.0
            }
        },
        "tags": {
            "resize-Enable": "True",
            "resize-StartTime": "Friday 7PM",
            "resize-EndTime": "Monday 6AM",
            "setState-Edition": "Standard",
            "setState-Dtu": "100"
        },
        "identity": null,
        "zones": null,
        "extendedLocation": null,
        "ResourceId": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Sql/servers/elastic-test/elasticpools/sql-test"
    }
}
```
