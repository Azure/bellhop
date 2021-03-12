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
        - Scale Direction + Azure Resource Graph Query.

***template.psm1 example:***
```
###################################################################################
##
## All functions will accept the same parameters, parsed from the storage Queue message
##
###################################################################################

function Update-Resource {
    param (
        [Parameter(Mandatory = $true)]
        [Object]
        $graphData,

        [Parameter(Mandatory = $true)]
        [Hashtable]
        $tagData,

        [Parameter(Mandatory = $true)]
        [String]
        $direction
    )

    # Set preference variables
    $ErrorActionPreference = "Stop"

###################################################################################
##
## Here you will need to gather the resource specific data you need to perform the scale operation
##
###################################################################################

    $workerSizeMap = @{
        D1          = "Small"
        D2          = "Medmium"
        D3          = "Large"
        Default     = "Small"
        Medium      = "Medium"
        Large       = "Lerge"
        SmallV3     = "Small"
        MediumV3    = "Medium"
        LargeV3     = "Large"
    }

###################################################################################
##
## All resources will have $baseData. These values are needed as flags for the scaling operation
##
###################################################################################

    $baseData = @{
        ResourceGroupName = $graphData.resourceGroup
        Name              = $graphData.name
    }

    $config = @{ }
    $tags = $tagData.tags

###################################################################################
##  
## You will need to switch off of the direction sent from the Engine function.
## Scaling UP is typically easier, and involves parsing the values of "saveState-" tags.
##  
###################################################################################

    switch ($direction) {
        'up' {
            Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.saveData.Tier)'"

            $config = $baseData + $tagData.saveData
        }

###################################################################################
##
## Scaling DOWN is typically the harder operation and requires the most logic. This also involves populating the 
## $saveData variable which is used to remember what to scale the resource back to
##
###################################################################################

        'down' {
            Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.setData.Tier)'"

            $config = @{
                Tier = $tagData.setData.Tier
            }

            if ( $tagData.setData.WorkerSize ) { $config.Add("WorkerSize", $tagData.setData.WorkerSize) }
            if ( $tagData.setData.NumberofWorkers ) { $config.Add("NumberofWorkers", $tagData.setData.NumberofWorkers) }

            $saveData = @{
                WorkerSize      = $workerSizeMap[$graphData.properties.workerSize]
                Tier            = $graphData.sku.tier
                NumberofWorkers = $graphData.sku.capacity
            }

            $config += $baseData
            $tags += Set-SaveTags $saveData
        }
    }

###################################################################################
##
## The scale command for most resources is "Set-AzRESOURCENAME", but not always.  These functions are written to 
## pass @config (hashtable) and the required tags.  
##
###################################################################################

    # Scale the App Service Plan
    try {
        Set-AzAppServicePlan @config -Tag $tags
    }
    catch {
        Write-Host "Error scaling App Service Plan: $($graphData.name)"
        Write-Host "($($Error.exception.GetType().fullname)) - $($PSItem.ToString())"
        # throw $PSItem
        Exit
    }
    
    Write-Host "Scaler function has completed successfully!"
}

###################################################################################
##
## All functions will use the Set-SaveTags function to parse Tag data and ensure proper scaling operations
##
###################################################################################

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

3) Run `updateScaler.ps1` script from project root to Zip-Deploy new Scaler code.

Example:
```
PS /User/github/Azure/bellhop> ./updateScaler.ps1
Enter resource group name where function is deployed: bellhop-rg 
```

4) Create new `servicename.md` page to document how to use the new scaler.
    - Create this file in the `./docs/scaler/modules/` folder
    - Update the `./docs/_sidebar.md` with path to new scaler document
    - New page will be displayed on the documentation site
