#
# APP SERVICE PLAN SCALE FUNCTION
#
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

    $workerSizeMap = @{
        D1          = "Small"
        D2          = "Medium"
        D3          = "Large"
        Default     = "Small"
        Medium      = "Medium"
        Large       = "Large"
        SmallV3     = "Small"
        MediumV3    = "Medium"
        LargeV3     = "Large"
    }

    $baseData = @{
        ResourceGroupName = $graphData.resourceGroup
        Name              = $graphData.name
    }

    $config = @{}
    $tags = $tagData.tags

    switch ($direction) {
        'up' {
            Write-Host "Scaling App Service Plan: '$($graphData.name)' to Tier: '$($tagData.saveData.Tier)'"

            $config = $baseData + $tagData.saveData
        }

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
            $tags += Set-SaveTags $saveData $tagData.map
        }
    }

    # Scale the App Service Plan
    try {
        Set-AzAppServicePlan @config -Tag $tags
    }
    catch {
        throw $PSItem
    }
    
    Write-Host "Scaler function has completed successfully!"
}

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
