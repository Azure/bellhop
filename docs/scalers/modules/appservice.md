# Bellhop for Azure App Service Plans
This README will describe how to use Bellhop to help scale your Azure App service Plan Resources.

## Scaling Considerations
**MORE INFORMATION NEEDED AROUND SCALING THIS SERVICE SPECIFICALLY, THIS IS JUST A PLACE HOLDER**

## Required Tags for Azure App Service Plans
```
setState-WorkerSize = <String>
setState-Tier = <String>
```

For more information on tag values for Azure SQL Databases please see the Microsoft documentaion: [Set-AzAppServicePlan](https://docs.microsoft.com/en-us/powershell/module/az.websites/set-azappserviceplan?view=azps-5.4.0)

## Sample scale message
An example of the message sent to the queue by the engine function 

- appserviceplan.json
```
{
    "direction": "down",
    "graphResults": {
        "id": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Web/serverFarms/autoscale-test-app",
        "name": "autoscale-test-app",
        "type": "microsoft.web/serverfarms",
        "tenantId": "<TENANT-ID>",
        "kind": "linux",
        "location": "westus2",
        "resourceGroup": "<RESOURCE-GROUP-NAME>",
        "subscriptionId": "<SUBSCRIPTION-ID>",
        "managedBy": "",
        "sku": {
            "name": "S3",
            "tier": "Standard",
            "capacity": 1,
            "family": "S",
            "size": "S3"
        },
        "plan": null,
        "properties": {
            "provisioningState": "Succeeded",
            "name": "autoscale-test-app",
            "resourceGroup": "<RESOURCE-GROUP-NAME>",
            "subscription": "<SUBSCRIPTION-ID>",
            "kind": "linux",
            "tags": {
                "setState-WorkerSize": "Small",
                "setState-Tier": "Basic",
                "resize-Enable": "True"
            },
            "hostingEnvironmentProfile": null,
            "status": "Ready",
            "hostingEnvironmentId": null,
            "hostingEnvironment": null,
            "numberOfWorkers": 1,
            "serverFarmId": 13574,
            "computeMode": "Dedicated",
            "webSpace": "<RESOURCE-GROUP-NAME>-<REGION>webspace",
            "reserved": true,
            "siteMode": null,
            "isXenon": false,
            "hyperV": false,
            "freeOfferExpirationTime": null,
            "spotExpirationTime": null,
            "isSpot": false,
            "numberOfSites": 0,
            "maximumElasticWorkerCount": 1,
            "perSiteScaling": false,
            "geoRegion": "West US 2",
            "adminRuntimeSiteName": null,
            "planName": "VirtualDedicatedPlan",
            "maximumNumberOfWorkers": 10,
            "adminSiteName": null,
            "currentNumberOfWorkers": 1,
            "currentWorkerSizeId": 2,
            "currentWorkerSize": "Large",
            "workerTierName": null,
            "workerSizeId": 2,
            "workerSize": "Large",
            "azBalancing": false,
            "existingServerFarmIds": null,
            "webSiteId": null,
            "targetWorkerSizeId": 0,
            "targetWorkerCount": 0,
            "mdmId": ""
        },
        "tags": {
            "setState-WorkerSize": "Small",
            "setState-Tier": "Basic",
            "resize-Enable": "True"
        },
        "identity": null,
        "zones": null,
        "extendedLocation": null,
        "ResourceId": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Web/serverFarms/autoscale-test-app"
    }
}
```