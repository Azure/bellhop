# Bellhop Scaler Modules

Scaler modules are built separately to allow the main Scaler function code to remain lightweight and rarely change. This makes the Scaler modules the most complicated part of this solution, but this intentional design affords the solution much more flexibility. Each module is developed to be specific to the service which it is going to scale, so each Azure resource type will have its own scaler module.

Each Azure resource that will be scaled using Bellhop, will require its own Powershell module in the form of a `.psm1`. These modules are always named `function.psm1` and are created under the `./functions/scaler/BellhopScaler/scalers` directory. The subfolders in that directory **must** follow the `Microsoft.<TYPE>` format in order for the Scaler function to import the correct module. 

_You can reference these resource types when creating new Scalers: [Azure Resource Graph Type Reference](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)_

**For example**
When creating the `App Service Plan` Scaler module (discussed in detail below), the folder structure would look like:
```
./functions/scaler/BellhopScaler/scalers/microsoft.web/serverfarms/function.psm1
```


## Creating a New Scaler Module
Bellhop was designed to be extensible from the beginning, with the idea being that each Azure resource type would have unique scaling logic contained in a separate module. The core Engine and Scaler function code should not have to change. There are a few steps to follow to ensure success:

1) Create new folder in the `./functions/scaler/BellhopScaler/scalers` folder that follows the naming pattern based on [Azure Resource Type](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)
    - **The format of these folders is important because the main Scaler Function uses the resource type returned from the Graph API query to determine the path to the correct Scaler Module to import**
    - **See Example Above**

2) Create new .psm1 PowerShell module
    - Name: `function.psm1`
    - This module will contain all of the logic necessary to scale the new resource type, including:
        - Azure PowerShell call to resize the target resource
        - Necessary mapping from Azure Graph API Data to PowerShell update command 
    - Developed to accept the following message format from the Bellhop storage queue:
        - Debug Flag
        - Scale Direction
        - Azure Resource Graph Query results
        - TagMap
    **Example**:
    ```
    {
        "debug": false,
        "direction": "down",
        "tagMap": {
            "enable": "<ENABLE-TAG-VALUE>",
            "start": "<START-TAG-VALUE>",
            "end": "<END-TAG-VALUE>",
            "set": "<SET-PREFIX-VALUE>",
            "save": "<SAVE-PREFIX-VALUE>"
        },
        "graphResults": {
            <...Graph API Results...>
        }
    }
    ```
    - New Scaler modules should all follow a similar format and be designed to accept the same common parameters
        - **Example Scaler function.psm1 can be found in the GitHub repo [/dev/scaler](./dev/scaler/) folder**
        - **Example Scaler logic walkthrough in section [below](#Scaler-Module-Logic)**

3) Build and push new container image versions
    - When creating a new Scaler module, you must update the Scaler function container, at a minimum, so that it includes the new module in the image. We recommend building both a new Scaler function and a new Engine function to keep build versions in sync.

        - **Scaler Example** - To build and push a new Scaler function image, run the following command from the project path `./functions/scaler`:
        ```
        docker build -t azurebellhop/scaler:vX.X .
        ```
        - **Engine Example** - To build and push a new Engine function image, run the following command from the project path `./functions/engine`:
        ```
        docker build -t azurebellhop/engine:vX.X .
        ```
        _**Docker build commands must include valid container image version numbers**_

4) Create new `servicename.md` page to document how to use the new Scaler
    - Create this file in the `./docs/scaler/modules/` folder
    - Update the `./docs/_sidebar.md` with path to new scaler document
    - New page will be displayed on the documentation site


## Scaler Module Logic
When planning to extend Bellhop functionality to include a new Azure resource, it is important to remember the format of the data that the Scaler is expecting from the Engine function. This data will remain consistent across all resource types, and will include 4 distinct data points:
- Debug setting to enable verbose logging (Configured via App Config)
- Direction to scale (up/down)
- Azure Graph Query result
- Tag Map for custom tag support

**IMPORTANT NOTE: The Bellhop team _STRONGLY_ recommends leveraging the data returned by the Graph API query, and not using any `Get-Az<RESOURCETYPE>` commands in your scaler modules. This practice is intended to limit the number of queries to prevent overloading the Azure API, thus improving reliability and performance**

The Engine will always send the same formatted messages to the queue so you'll need to build the new Scaler logic around this information. The Scaler function takes the Storage Queue message and breaks it into 3 parameters that will be passed to each Scaler module. These parameters are:
- $graphData - `graphResults` section from storage queue message
- $tagData - `graphResults.tags` + `tagMap` sections from storage queue message
- $direction - `direction` section from storage queue message

To further illustrate this point, let's use the App Service Plan Scaler module as an example. Observe the process of building the logical map between Azure Graph API data and the required Powershell commands to scale the desired resource. As mentioned earlier, this will likely be the most complicated part of building a new Scaler module because this logic is fairly unique across all Azure resources.

### Powershell Commands
All of the Scaler modules are written in PowerShell due to how effectively it handles management operations across Azure resources. The majority of resources in Azure can be scaled by using the single `Set-Az<RESOURCENAME>` PowerShell Cmdlet. Unfortunately, this is not true for _ALL_ Azure resources, and will require validation via the Azure resource specific documentation.

The PowerShell command Bellhop uses to scale an App Service Plan is:
- `Set-AzAppServicePlan`
- From the [documentation](https://docs.microsoft.com/en-us/powershell/module/az.websites/set-azappserviceplan?view=azps-5.7.0) you can see (Example 2) that the necessary parameters to scale the resource are:
    - Name (App Service Plan Name)
    - ResourceGroupName (RG where the App Service Plan is deployed)
    - Tier (Target App Service Plan Tier)
    - WorkerSize (Target Compute size for App Service Plan)

Example:
```
Set-AzAppServicePlan -Name "bellhop-test-app" -ResourceGroupName "bellhop-test-rg" -Tier "Basic" -WorkerSize "Small"
```  

### Graph Results
The majority of the resource data you will have to work with when designing a new Scaler is returned via the Azure Graph API query and provided to the Scaler via the `graphResults` section of the scale message.

App Service Plan example:

```
"graphResults": {
    "id": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Web/serverFarms/bellhop-test-app",
    "name": "bellhop-test-app",
    "type": "microsoft.web/serverfarms",
    "tenantId": "<TENANT-ID>",
    "kind": "linux",
    "location": "westus2",
    "resourceGroup": "bellhop-test-rg",
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
        "resourceGroup": "bellhop-test-rg",
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
    "ResourceId": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Web/serverFarms/bellhop-test-app"
}
```

From these results, we can gather the information required to identify the resources' current state. These values will be used to formulate the `"<SAVE-STATE-PREFIX>-*"` tags:
- `"graphResults/properties/workerSize": "Large"` 
    - becomes the `"<SAVE-STATE-PREFIX>-WorkerSize": "Large"` tag
- `"graphResults/sku/tier": "Standard"` 
    - becomes the `"<SAVE-STATE-PREFIX>-Tier": "Standard"` tag

To determine the state to which the resource will scale _**DOWN**_, the Scaler will extract the `<SET-STATE-PREFIX>-*` tags from the `graphResults` and map those values to the corresponding PowerShell resource scale command:

**Base Resource Information**:
- `"graphResults/name": "bellhop-test-app"` - App Name
- `"graphResults/resourceGroup": "bellhop-test-rg"` - Resource Group Name
- `"graphResults/type": "microsoft.web/serverfarms"` - Resource Type for Scaler Module import

**App Service Specific Information**:
- `"graphResults/tags/setState-WorkerSize": "Small"` - Scale down to Worker Size "Small"
- `"graphResults/tags/setState-Tier": "Basic"` - Scale down to Tier "Basic"

### Mapping Graph Results to Powershell Command Parameters
Often times the data returned from the Azure Resource Graph API query will require custom mapping to fit the values each `Set-Az<RESOURCENAME>` PowerShell command expects. This is where each Scaler module will vary the most. Each Azure resource requires different combinations of parameters to initiate a scale operation. 

When building the Scaler Module for an App Service Plan, it was required to build a custom map of the WorkerSize values returned from the Azure Resource Graph API to the values that PowerShell expects, as they do not align. 

**Custom Map Built for App Service Plan Worker Size**:
```
   $workerSizeMap = @{
        D1          = "Small"
        D2          = "Medmium"
        D3          = "Large"
        Default     = "Small"
        Medium      = "Medium"
        Large       = "Large"
        SmallV3     = "Small"
        MediumV3    = "Medium"
        LargeV3     = "Large"
    }
```

### Putting it all together
In all Scaler modules, you should begin by building the `$baseData`, `$config`, and `$tag` objects. In the App Service example you'll see that represented as:

```
$baseData = @{
    ResourceGroupName = $graphData.resourceGroup
    Name              = $graphData.name
}

$config = @{ }
$tags = $tagData.tags
```

#### Scaling Up
There is a Switch statement to determine the scale direction sent from the Engine function.  Scaling up is generally a simple operation, as it only requires parsing the values of the `<SAVE-STATE-PREFIX>-*` tags and "splatting" them into the PowerShell scale command.

`$config` is populated with the saved tag data and base data, providing the necessary parameters to the PowerShell scale command:

```
switch ($direction) {
    'up' {
        Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.saveData.Tier)'"

        $config = $baseData + $tagData.saveData
    }
```

#### Scaling Down
Scaling down tends to be a far more challenging operation, and often requires more complex code logic. In the example below for App Service Plans, you'll see the build out the `$config` HashTable, starting with the Tier which is the only _REQUIRED_ additional parameter (in the case of App Service Plans).

```
switch ($direction) {
    'down' {
        Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.setData.Tier)'"

        $config = @{
            Tier = $tagData.setData.Tier
        }
    }
```

Next, optional parameters such as "WorkerSize" and "NumberofWorkers" are evaluated to verify if either are configured on the target resource (via `<SET-STATE-PREFIX>-*` tags). They are then appended to the `$config` HashTable via the following code:

```
    if ( $tagData.setData.WorkerSize ) { $config.Add("WorkerSize", $tagData.setData.WorkerSize) }
    if ( $tagData.setData.NumberofWorkers ) { $config.Add("NumberofWorkers", $tagData.setData.NumberofWorkers) }
```

After that, it's necessary to build the `$saveData` object which is used to "remember" the  original state for the target resource. This is done using both the `$workerSizeMap` discussed above, combined with the values from the Azure Resource Graph API query:

```
$saveData = @{
        WorkerSize      = $workerSizeMap[$graphData.properties.workerSize]
        Tier            = $graphData.sku.tier
        NumberofWorkers = $graphData.sku.capacity
    }
```

Finally, the `$config` and `$tags` objects which will be passed to the PowerShell scale command need to be set. For the `$config` object, `$baseData` is appended to the configuration set in the previous steps. Next, the `$saveData` and `$tagData.map` objects are passed to the _`Set-SaveTags`_ function, which is used to generate the `<SAVE-STATE-PREFIX>-*` tags (returned as the `$tags` variable) to be applied to the resource during the scale down operation:

```
    $config += $baseData
    $tags += Set-SaveTags $saveData $tagData.map
```

#### Saving the Original State
In order to track the original configuration of the resource, every Scaler module uses the _`Set-SaveTags`_ function inside of the `.psm1`. This simple function takes the `$saveData` object which represents the resources' current configuration, and then appends `<SAVE-STATE-PREFIX>-*` to each key, creating the updated tag values. These values are then appended to the `$tags` object which will be passed to the `Set-Az<RESOURCENAME>` command. 

**Set-SaveTags**:
```
function Set-SaveTags {
    param (
        $inTags,
        $tagMap
    )

    $outTags = @{ }
    $inTags.keys | ForEach-Object { $outTags += @{($tagMap.save + $_) = $inTags[$_] } }
    
    return $outTags
}

Export-ModuleMember -Function Update-Resource
```

#### Setting new Resource Config
At this time, whether Bellhop is scaling a resource _UP_ or _DOWN_, the Scaler module has all the data required to scale the target resource. All that needs to happen to complete this process, is issuing the proper PowerShell command and pass in the `$config` and `$tags` objects. 

_Note: the `$config` object is passed to the Set-Resource command as a hash table by using "@" instead of "$" in front of "config" (referred to as 'splatting')._

**App Service Set Resource Example**:

```
Set-AzAppServicePlan @config -Tag $tags
```

_**Sample Scaler module .psm1 can be found in the [dev](./dev/scaler/) folder within the GitHub repo**_
