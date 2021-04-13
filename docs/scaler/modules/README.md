# Bellhop Scaler Modules

Scaler modules are built separately to allow the main Scaler Function code to remain lightweight and rarely change. This makes the scaler modules most complicated part of this solution, but this intentional design affords the solution much more flexibility. Each module is developed to be specific to the service they are going to scale, so each Azure resource type will have its own scaler module.

Each Azure resource that you wish to scale using Bellhop, will require its own Powershell Module in the form of a `.psm1`. These modules will be named `function.psm1` and are created under the `./functions/scaler/BellhopScaler/scalers` directory. The sub folders in that directory **must** follow the Microsoft.TYPE format in order for the Scaler Function to import the correct module. 

_You can reference these resource types when creating new scalers: [Azure Resource Graph Type Reference](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)_

**For example**
When creating the `App Service Plan` scaler module (discussed in detail below), the folder structure looks like this:
```
./functions/scaler/BellhopScaler/scalers/microsoft.web/serverfarms/function.psm1
```


## Creating a New Scaler Module
This solution was designed to be extensible from the beginning, with the idea being that to scale a new resource you only need to write a new module. The core Engine and Scaler Function code should not have to change.  There are a few steps to follow to ensure success:

1) Create new folder in the `./functions/scaler/BellhopScaler/scalers` folder that follows the pattern based on [Azure Resource Type](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)
    - **The format of these folders is important because the main Scaler Function uses the resource type returned from the Graph API query to determine the path to the correct Scaler Module to import**
    - **SEE EXAMPLE ABOVE**

2) Create new .psm1 PowerShell Module
    - Named: `function.psm1`
    - This module will contain all of the logic necessary to scale the new resource type, including:
        - Azure PowerShell call to resize the rarget resource
        - Necessary mapping of Azure Graph API Data to PowerShell scale command 
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
        "graphResults": {
            <..Graph API Results..>
        },
        "tagMap": {
            <..Map of Custom Tags..>
        }
    }
    ```
    - The scaler modules should all follow a similar format and be designed to accept the same common parameters
        - **Example Scaler function.psm1 can be found in the GitHub repo [/dev](./dev/scaler/) folder**
        - **Example scaler logic walkthrough in section [below](#Scaler-Module-Logic)**

3) Build and Push New Container Image Versions
    - When creating a new Scaler Module you must update the Scaler Function container, at a minimum, so that it includes the new module in the image. We recommend building both a new Scaler Function and a new Engine Function to keep build versions in sync.

        - **Scaler Example** - To build and push a new Scaler Function Image just run the following command from the project path `./functions/scaler`:
        ```
        docker build -t azurebellhop/scaler:vX.X .
        ```
        - **Engine Example** - To build and push a new Engine Function Image just run the following command from the project path `./functions/engine`:
        ```
        docker build -t azurebellhop/engine:vX.X .
        ```
        _**Docker build commands must include valid container image version numbers**_

4) Create new `servicename.md` page to document how to use the new scaler
    - Create this file in the `./docs/scaler/modules/` folder
    - Update the `./docs/_sidebar.md` with path to new scaler document
    - New page will be displayed on the documentation site


## Scaler Module Logic
When beginning to think about extending Bellhop functionality to include a new Azure resource, it is important to remember the format of the data that we are expecting from the Engine function. This data will remain consistent across all resource types, and will include 4 distinct pieces:
- Debug setting to enable verbose logging (Configured in App Config)
- Direction to Scale
- Azure Graph Query result
- Tag Map for Custom Tag Support

**IMPORTANT NOTE: The Bellhop team _STRONGLY_ recommends leveraging the data returned by the Graph API query, and not using any `Get-Az<RESOURCETYPE>` commands in your scaler modules. This practice is intended to limit the number of queries to prevent overloading the Azure API, thus improving reliability and performance**

The Engine will always send the same formatted messages to the queue so we need to build our scaler logic around this information. The Scaler Function takes the Storage Queue message and breaks it into 3 parameters that will be passed to each Scaler Module. These parameters are:
- $graphData - `graphResults` section from storage queue message
- $tagData - `graphResults.tags` section from storage queue message
- $direction - `direction` section from storage queue message

To further illustrate this point, we can use the App Service Plan Scaler Module as an example. We can look at the process of building the logical map between Azure Graph API data and the required Powershell commands to scale the desired resource. As mentioned earlier, this will be the most complicated part of building a new Scaler Module because this logic is not consistent across all Azure resources.

### Powershell Commands
All of the Scaler Modules are written in PowerShell due to how effectively it handles mangement operations on Azure resources. The majority of resources in Azure can be scaled by using the single `Set-Az<RESOURCENAME>` PowerShell Cmdlet. Unfortunately, this is not true for _ALL_ Azure resources, and will need validation via Azure resource specific documentation.

The PowerShell command Bellhop uses to scale an App Service Plan is:
- `Set-AzAppServicePlan`
- From the [documentation](https://docs.microsoft.com/en-us/powershell/module/az.websites/set-azappserviceplan?view=azps-5.7.0) you can see (Example 2) that the necessary parameters to scale the resource are:
    - Name (ASP Name)
    - ResourceGroupName (RG where ASP is deployed)
    - Tier (Desired ASP Tier)
    - WorkerSize (Desired Compute size for App Service Plan)

Example:
```
Set-AzAppServicePlan -Name "bellhop-test-app" -ResourceGroupName "bellhop-test-rg" -Tier "Basic" -WorkerSize "Small"
```  

### Graph Results
The majority of the resource data we will have to work with is returned via the Azure Graph API query and provided to the scaler via the `graphResults` section of the scale message.

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

From these results, we can gather the information required to capture the resources current state. These vales will be used to create the `"saveState-"` tags:
- `"graphResults/properties/workerSize": "Large"` 
    - becomes the `"saveState-WorkerSize": "Large"` tag
- `"graphResults/sku/tier": "Standard"` 
    - becomes the `"saveState-Tier": "Standard"` tag

We can also determine the desired target state to scale _**DOWN**_ to. From the `graphResults` above we would use the following tag and query values to map to our PowerShell Command:

**Base Resource Information**:
- `"graphResults/name": "bellhop-test-app"` - App Name
- `"graphResults/ResourceGroup": "bellhop-test-rg"` - Resource Group Name
- `"graphResults/type": "microsoft.web/serverfarms"` - Resource type for Scaler Module import

**App Service Specific Information**:
- `"graphResults/tags/setState-WorkerSize": "Small"` - Scale down Worker Size "Small"
- `"graphResults/tags/setState-Tier": "Basic"` - Scale down tier "Basic"

### Mapping Graph Results to Powershell Command Parameters
Often times the data returned from the Graph API query will require custom mapping to fit the values the `"Set-Az<RESOURCENAME>"` PowerShell command expects. This is where each Scaler Module will differ the most. Each service requires different combinations of parameters to issue a scale operation. 

When building the Scaler Module for an App Service Plan it was required to build a custom map of the WorkerSize values returned via the Graph API to the values that PowerShell expects, because they do not match. 

**Custom Map built for App Service Plan Worker Size**:
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
In all Scaler Modules we begin by building our `$baseData`, `$config`, and `$tag` objects. In the App Service Example we see that represented as:

```
$baseData = @{
    ResourceGroupName = $graphData.resourceGroup
    Name              = $graphData.name
}

$config = @{ }
$tags = $tagData.tags
```

#### Scaling Up
You will need to switch off of the direction sent from the Engine function.  Scaling up is the easier operation as it only requires parsing the values of the `"saveState-"` tags.

`$config` is populated with the saved tag data and base data, providing the necessary parameters to the scaling command:
```
switch ($direction) {
    'up' {
        Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.saveData.Tier)'"

        $config = $baseData + $tagData.saveData
    }
```

#### Scaling Down
Scaling down is the more challenging operation and often requires the most complex code logic. In the below example for App Service Plans we begin to build out the `$config` Hash Table, starting with the Tier which is the only _REQUIRED_ additional parameter.
```
switch ($direction) {
    'down' {
        Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.setData.Tier)'"

        $config = @{
            Tier = $tagData.setData.Tier
        }
    }
```

We then evaluate whether "WorkerSize" or "NumberofWorkers" was configured on the target resource. If one or both has then they are then added to the `$config` hash table via the below commands:
```
    if ( $tagData.setData.WorkerSize ) { $config.Add("WorkerSize", $tagData.setData.WorkerSize) }
    if ( $tagData.setData.NumberofWorkers ) { $config.Add("NumberofWorkers", $tagData.setData.NumberofWorkers) }
```

Next, we build the `$saveData` object which is used to remember the resources original state. This is done using both the `$WorkerSizeMap` we discussed above and the values from the Graph API query:
```
$saveData = @{
        WorkerSize      = $workerSizeMap[$graphData.properties.workerSize]
        Tier            = $graphData.sku.tier
        NumberofWorkers = $graphData.sku.capacity
    }
```

We can now finalize the `$config` and `$tags` objects that we will use to pass to the PowerShell Command. For the `$config` object we just combine what we have built so far with the initial `$baseData` we gathered. We then pass the `$saveData` object to the _`Set-SaveTags`_ Function which is used to generate the `"saveState-"` tags (saved as `$tags`) to be applied to the resource during scale down:
```
    $config += $baseData
    $tags += Set-SaveTags $saveData
```

#### Saving the Original State
In order to track the original configuration of the resource, every Scaler Module uses the `Set-SaveTags` function inside of their `psm1`. This simple function takes the `$saveData` object which reprents the resources current configuration and then appends `"saveState-"` to each key creating the new tag values. These values are then appended to the `$tags` object which will be passed to the `"Set-Az<RESOURCENAME>"` command. 

**Set-SaveTags**:
```
function Set-SaveTags {
    param (
        $inTags
    )

    $outTags = @{ }
    $inTags.keys | ForEach-Object { $outTags += @{("saveState-" + $_) = $inTags[$_] } }
    
    return $outTags
}

Export-ModuleMember -Function Update-Resource
```

#### Setting new Resource Config
At this time, whether we are scaling _UP_ or _DOWN_ we have all the data required to scale the target resource. All that we need to do is issue the proper PowerShell command and pass in our updated `$config` and `$tags` objects. 

_Note: the config object is passed to the Set-Resource command as a hash table by using "@" instead of "$" in front of "config"_.

**App Service Set Resource Example**:
```
Set-AzAppServicePlan @config -Tag $tags
```
_**Sample-scaler module psm1 can be found in the [dev](./dev/scaler/) folder in the GitHub repo**_




