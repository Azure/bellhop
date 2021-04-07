# Bellhop Scaler Modules

Scaler modules are built separately to allow the main Scaler Function code to remain lightweight and rarely change. This makes the scaler modules most complicated part of this solution, but this intentional design affords the solution much more flexibility. Each module is developed to be specific to the service they are going to scale, so each Azure resource type will have its own scaler module.

Each Azure resource that you wish to scale using Bellhop, will require its own Powershell Module in the form of a `.psm1`. These modules will be named `function.psm1` and are created under the `./functions/scaler/BellhopScaler/scalers` directory. The sub folders in that directory **must** follow the Microsoft.TYPE format in order for the Scaler Function to import the correct module. 

_You can reference these resource types when creating new scalers: [Azure Resource Graph Type Reference](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)_

**For example**
When creating the `App Service Plan` scaler module, the folder structure looks like this:
```
./functions/scaler/BellhopScaler/scalers/microsoft.web/serverfarms/function.psm1
```


## Creating a New Scaler Module
How can I extend this myself for a new Azure Resource Type?

This solution was designed to be extensible from the beginning, with the idea being that to scale a new resource you only need to write a new module. There are a few steps to this process:

1) Create new folder in the `/functions/scaler/BellhopScaler/scalers` folder that follows the pattern based on [Azure Resource Type](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)
    - **The format of these folders is important because the main Scaler-Trigger function uses the resource type returned from the Graph API query to determine the path to the correct Powershell Module to import**
    - **SEE EXAMPLE ABOVE**

2) Create new .psm1 PowerShell Module
    - Named: `function.psm1`
    - This module will contain all of the logic to scale the new resource type, including the Azure PowerShell call to resize the rarget resource.
    - Developed to accept the message format sent to the `autoscale` storage queue.
        - Scale Direction + Azure Resource Graph Query results
    - The scaler modules should all follow a similar format and be designed to accept the same common parameters.
        - **Sample-scaler module psm1 can be found in the [development](./development/sample-scaler/) folder in the GitHub repo**
        - **Example scaler logic walkthrough in section below**

3) **TODO: DOCUMENT PROCESS TO UPDATE SCALER FUNCTION CONTAINER IMAGE**
    - _Current process is being updated and documented_

4) Create new `servicename.md` page to document how to use the new scaler.
    - Create this file in the `./docs/scaler/modules/` folder
    - Update the `./docs/_sidebar.md` with path to new scaler document
    - New page will be displayed on the documentation site


## Scaler Module Logic
When beginning to think about extending Bellhop functionality to include a new Azure service, it is important to remember the format of the data that we are expecting from the Engine function. This data will remain consistent across all resource types, and will include 3 pieces:
- Debug setting to enable verbose logging (Configured in App Config)
- Direction to Scale
- Azure Graph Query result

**It is important to note that we _STRONGLY_ recommend leveraging the data returned by the Graph API query. This is intended to limit the amount of queries to the API improving reliability and performance**

The Engine will always send the same formatted messages to the queue so we need to build our scaler logic around this information. The Scaler Function takes the Storage Queue message and breaks it into 3 parameters that will be passed to each Scaler Module. These parameters are:
- $graphData - `graphResults` section from storage queue message
- $tagData - `graphResults.tags` section from storage queue message
- $direction - `direction` section from storage queue message

To further illustrate this point, we can use the App Service Plan Scaler Module as an example. We can look at the process of building the map between Azure Graph API data and the necessary Powershell commands to scale the resource. As mentioned earlier, this will be the most complicated part of building a new Scaler Module because this logic/necessary information is not the same between all resources.  

### Graph Results
The base of the resource information we will have to work with is what is returned via the Azure Graph API query. Using an ASP, this is a sample Storage Queue message:

```
{
    "debug": false,
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
                "resize-Enable": "True"
                "resize-StartTime": "Friday 7PM",
                "resize-EndTime": "Monday 6AM",
                "setState-WorkerSize": "Small",
                "setState-Tier": "Basic",
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
        "ResourceId": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Web/serverFarms/autoscale-test-app"
    }
}
```

From these results, we see that the resource is going to scale is down as noted here:
```
{
    "debug": false,
    "direction": "down",
    "graphResults": {
    ...
```



### Powershell Commands to Scale

### Mapping Graph Results to Powershell Scale Commands

### Putting it all together