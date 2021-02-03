# Bellhop for Azure SQL Elastic Pools
This README will describe how to use Bellhop to help scale your Azure SQL ELastic Pool Resources.

## Scaling Considerations
Scaling Azure SQL Elastic Pools effectively is a bit more challenging, as you really have to make sure to account for scaling between Vcore editions and DTU's. Each type will have different tag options and requirements.

## Required Tags for DTU Type
```
setState-Edition = <String>
setState-Dtu = <Int32>
```

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

## Optional Tags for VCore Type
```
setState-DatabaseCapacityMin = <Double>
setState-DatabaseCapacityMax = <Double>
setState-StorageMB = <Int32>
setState-LicenseType = <String>
```

For more information on tag values for Azure SQL Elastic pools please see the Microsoft documentaion: [Set-AzSqlElasticPool](https://docs.microsoft.com/en-us/powershell/module/az.sql/set-azsqlelasticpool?view=azps-5.4.0)


## Sample message sent to Queue by Engine function
An example of the message sent to the queue by the engine function.

**SQL ELastic pool Meaasge**
```
{
    "direction": "down",
    "graphResults": {
        "id": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Sql/servers/elastic-test/elasticpools/sql-test",
        "name": "sql-test",
        "type": "microsoft.sql/servers/elasticpools",
        "tenantId": "<TEANANT-ID>",
        "kind": "pool",
        "location": "westus2",
        "resourceGroup": "<RESOURCE-GROUP-NAME>",
        "subscriptionId": "<SUBSCRIPTION-ID>",
        "managedBy": "",
        "sku": {
            "name": "StandardPool",
            "tier": "Standard",
            "capacity": 100
        },
        "plan": null,
        "properties": {
            "state": "Ready",
            "zoneRedundant": false,
            "creationDate": "2021-01-06T18:27:55.947Z",
            "maxSizeBytes": 53687091200,
            "perDatabaseSettings": {
                "minCapacity": 0.0,
                "maxCapacity": 10.0
            }
        },
        "tags": {
            "setState-Edition": "Basic",
            "resize-Enable": "True"
        },
        "identity": null,
        "zones": null,
        "extendedLocation": null,
        "ResourceId": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Sql/servers/elastic-test/elasticpools/sql-test"
    }
}
```