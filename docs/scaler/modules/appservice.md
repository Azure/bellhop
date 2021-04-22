# Bellhop for Azure App Service Plans
This README will describe how to use Bellhop to help scale your Azure App service Plan Resources.

## Scaling Considerations

- Please refer to [this](https://docs.microsoft.com/en-us/azure/app-service/manage-scale-up) document for considerations when scaling an Azure App Service resource between service tiers.

## Required Tags for Azure App Service Plans
```
setState-WorkerSize = <String>
setState-Tier = <String>
```

WorkerSize Options:
- Small
- Medium
- Large

Tier Options:
- Basic
- Standard
- Premium
- PremiumV2
- PremiumV3

For more information on tag values for Azure App Service please see the Microsoft documentaion: [Set-AzAppServicePlan](https://docs.microsoft.com/en-us/powershell/module/az.websites/set-azappserviceplan?view=azps-5.4.0)


## Sample Message Sent to Queue by Engine Function
An example of the message sent to the queue by the engine function 

**App Service Plan Message**
```
{
    "debug": false,
    "direction": "down",
    "tagMap": {
        "enable": "resize-Enable",
        "start": "resize-StartTime",
        "end": "resize-EndTime",
        "set": "setState-",
        "save": "saveState-"
    },
    "graphResults": {
        "id": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Web/serverFarms/bellhop-test-app",
        "name": "bellhop-test-app",
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
            "name": "bellhop-test-app",
            "resourceGroup": "<RESOURCE-GROUP-NAME>",
            "subscription": "<SUBSCRIPTION-ID>",
            "kind": "linux",
            "tags": {
                "resize-Enable": "True",
                "resize-StartTime": "Friday 7PM",
                "resize-EndTime": "Monday 6AM",
                "setState-WorkerSize": "Small",
                "setState-Tier": "Basic"
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
            "resize-Enable": "True",
            "resize-StartTime": "Friday 7PM",
            "resize-EndTime": "Monday 6AM",
            "setState-WorkerSize": "Small",
            "setState-Tier": "Basic"
        },
        "identity": null,
        "zones": null,
        "extendedLocation": null,
        "ResourceId": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Web/serverFarms/bellhop-test-app"
    }
}
```
