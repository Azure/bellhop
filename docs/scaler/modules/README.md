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

1) Create new folder in the `/functions/scaler/BellhopScaler/scalers` folder that follows the pattern based on [Azure Resource Type](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)
    - **The format of these folders is important because the main Scaler-Trigger function uses the resource type returned from the Graph API query to determine the path to the correct Powershell Module to import**
    - **SEE EXAMPLE ABOVE**

2) Create new .psm1 PowerShell Module
    - Named: `function.psm1`
    - This module will contain all of the logic to scale the new resource type, including the Azure PowerShell call to resize the rarget resource
    - Developed to accept the message format sent to the `autoscale` storage queue
        - Scale Direction + Azure Resource Graph Query results
    - The scaler modules should all follow a similar format and be designed to accept the same common parameters
        - **Sample-scaler module psm1 can be found in the [development](./development/sample-scaler/) folder in the GitHub repo**
        - **Example scaler logic walkthrough in section below**

3) Build and Push new Scaler Function container version.
    - Build new version via Dockerfile in `./functions/scaler/Dockerfile`
    - Push new version to Docker Hub Repo
    - _**NEED STEP BY STEP INSTRUCTIONS FOR HOW TO ACCOMPLISH THIS**_
    - _**Current process is being updated and documented**_

4) Create new `servicename.md` page to document how to use the new scaler.
    - Create this file in the `./docs/scaler/modules/` folder
    - Update the `./docs/_sidebar.md` with path to new scaler document
    - New page will be displayed on the documentation site


## Scaler Module Logic
When beginning to think about extending Bellhop functionality to include a new Azure service, it is important to remember the format of the data that we are expecting from the Engine function. This data will remain consistent across all resource types, and will include 3 pieces:
- Debug setting to enable verbose logging (Configured in App Config)
- Direction to Scale
- Azure Graph Query result

**It is important to note that we _STRONGLY_ recommend leveraging the data returned by the Graph API query, and not using `"Get-"` commands in your scaler modules. This practice is intended to limit the number of queries to the API, thus improving reliability and performance**

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

### Saving the Original State
Every Scaler Module should make use of the `Set-SaveTags` Function. This simple function takes the `$saveData` object which reprents the resources current configuration and then appends `"saveState-"` to each key creating the new tag values.

Function:
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

### Mapping Graph Results to Powershell Command Parameters
Often times the data returned from Graph will require custom mapping to fit what values the PowerShell expects. When sizing the App Service Plan it is required to custom map the Worker Sizes returned via the Graph API query with the specific  "Small", "Medium", and "Large" that PowerShell expects.

For Example ([sample-scaler/function.psm1](./development/sample-scaler/function.psm1)):

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

#### Scaling Up:

You will need to switch off of the direction sent from the Engine function.  Scaling up is the easier operation as it only requires parsing the values of the `"saveState-"` tags.

`$config` is populated with the saved tag data and base data, providing the necessary parameters to the scaling command:
```
switch ($direction) {
    'up' {
        Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.saveData.Tier)'"

        $config = $baseData + $tagData.saveData
    }
```

#### Scaling Down:

Scaling down is the more challenging operation and often requires the most complex code logic. In the below example for App Service Plans we begin to build out the `$config` Hash Table, starting with the Tier which is the only _REQUIRED_ additional parameter.
```
 'down' {
            Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.setData.Tier)'"

            $config = @{
                Tier = $tagData.setData.Tier
            }
```

We then evaluate whether "WorkerSize" or "NumberofWorkers" has been set and if one, or both, has they are then added to the "$config" hash table:
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

#### Setting new Resource Config:
At this time, whether we are scaling _UP_ or _DOWN_ we have all the data required to scale the target resource. All that we need to do is issue the proper PowerShell command and pass in our updated `$config` and `$tags` objects. 

_Note: the config object is passed to the Set-Resource command as a hash table by using "@" instead of "$" in front of "config"_.

**App Service Set Resource Example**:
```
Set-AzAppServicePlan @config -Tag $tags
```
_**Sample-scaler module psm1 can be found in the [development](./development/sample-scaler/) folder in the GitHub repo**_




