#######################################################################################################################
##
## EXAMPLE TEMPLATE FOR NEW SCALERS
## 
## All scaler modules will accept the same parameters. The following parameters will be 
## parsed from the storage Queue message sent by the Engine Function:
##
## - graphData: the result of the Azure Graph API query
## - tagData: tag data, with set and saveState details
## - direction: the direction to scale the resource (Up or Down)
##
#######################################################################################################################

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

#########################################################################################################################
## 
## RESOURCE SPECIFIC SCALE DATA
##
## Here you will need to gather the resource specific data you need to perform the scale operation.
## The process involves taking the Graph API query result for a given resource and mapping which values
## you need to manipulate to effectively scale... 
## 
## Using the below example for App Service Plans, we map available return values to their "Small", "Medium",
## and "Large" equivalents. 
##
#######################################################################################################################

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

#######################################################################################################################
##
## RESOURCE BASE DATA
## 
## All resources will use the $baseData variable, consisting of the scale targets Name and 
## Resource Group Name.  These values are needed as parameter values to execute the scaling
## operation. 
##
## We also create an empty Hash Table "$config", which will be used to pass all required 
## parameters to the scale command.
##
## Set the "$tags" object so that we can update with new tags after the scaling operation
##
#######################################################################################################################

    $baseData = @{
        ResourceGroupName = $graphData.resourceGroup
        Name              = $graphData.name
    }

    $config = @{ }
    $tags = $tagData.tags

#######################################################################################################################
##  
## SCALING UP
##
## You will need to switch off of the direction sent from the Engine function.  Scaling up is the easier 
## operation as it only requires parsing the values of the "saveState-" tags.
##
## $config is populated with the saved tag data and base data, providing the necessary parameters to scale.
##
## The "saveState-" tags set the original configuration of the resource. 
##  
#######################################################################################################################

    switch ($direction) {
        'up' {
            Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.saveData.Tier)'"

            $config = $baseData + $tagData.saveData
        }

#######################################################################################################################
##
## SCALING DOWN
##
## Scaling down is more challenging operation and often requires the most complex code logic.
## In the below example for App Service Plans we begin to build out the "$config" Hash Table, starting
## with the desired Tier for the ASP (the only required additional parameter).
##
## We then evaluate whether "WorkerSize" or "NumberofWorkers" has been set and if one or both has been 
## we add them to "$config".
## 
## This step also involves populating the "$saveData" variable which is used by the "Set-SaveTags" function
## to append "saveState-" to the "$saveData" tag names. So in the below example the tags would look like:
##
## - "WorkerSize" becomes "saveState-WorkerSize"
## - "Tier" becomes "saveState-Tier"
## - "NumberofWorkers" becomes "saveState-NumberofWorkers"
## 
## The "saveState" data is used to determine what values to scale the target resource back to.
##
#######################################################################################################################

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

#######################################################################################################################
##
## Scaling the Resource
##
## The PowerShell command to scale most resources is "Set-AzRESOURCENAME". This is not always the case and will need
## to be validated per Azure resource documentation. 
##
## All scaler modules should be written to accept "@config" (hashtable) and "$tags" values. These will set the new
## resource configuration and tag values.  
##
#######################################################################################################################

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

#######################################################################################################################
##
## Set-SaveTags Function
##
## Function takes tag inputs "$inTags" and then prepends "saveState-". It returns the value "$outTags"
## All scaler modules should use the Set-SaveTags function to parse resource Tag data when scaling down. This 
## is how Bellhop ensures proper scaling operations.
##
#######################################################################################################################

function Set-SaveTags {
    param (
        $inTags
    )

    $outTags = @{ }
    $inTags.keys | ForEach-Object { $outTags += @{("saveState-" + $_) = $inTags[$_] } }
    
    return $outTags
}

Export-ModuleMember -Function Update-Resource
