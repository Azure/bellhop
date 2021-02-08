#
# SQL ELASTIC POOL SCALE FUNCTION
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

    $editionMap = @{
        "Basic"             = "DTU"
        "Standard"          = "DTU"
        "Premium"           = "DTU"
        "GeneralPurpose"    = "vCore"
        "BusinessCritical"  = "vCore"
    }

    $baseData = @{
        ResourceGroupName   = $graphData.resourceGroup
        ServerName          = $($graphData.id | Select-String -Pattern '(?<=servers/).*(?=/elasticpools)').Matches.Value
        ElasticPoolName     = $graphData.name
    }

    $config = @{}
    $saveData = @{}
    $tags = $tagData.tags

    switch ($direction) {
        'up' {
            Write-Host "Scaling UP SQL Elastic Pool: '$($graphData.name)' to Tier: '$($tagData.saveData.Edition)'"

            $config = $baseData + $tagData.saveData
        }

        'down' {
            Write-Host "Scaling DOWN SQL Elastic Pool: '$($graphData.name)' to Tier: '$($tagData.setData.Edition)'"

            Write-Host "Compiling config data..."
            switch ( $editionMap[$tagData.setData.Edition] ) {
                'DTU' {
                    Write-Host "SetData DTU Mode"
                    $config = @{
                        Edition = $tagData.setData.Edition
                        Dtu     = $tagData.setData.Dtu
                    }

                    if ( $tagData.setData.DatabaseDtuMin ) { $config.Add("DatabaseDtuMin", $tagData.setData.DatabaseDtuMin) }
                    if ( $tagData.setData.DatabaseDtuMax ) { $config.Add("DatabaseDtuMax", $tagData.setData.DatabaseDtuMax) }
                    if ( $tagData.setData.StorageMB ) { $config.Add("StorageMB", $tagData.setData.StorageMB) }

                    # $tagData.setData.DatabaseDtuMin ? $config.Add("DatabaseDtuMin", $tagData.setData.DatabaseDtuMin) : $null | Out-Null
                    # $tagData.setData.DatabaseDtuMax ? $config.Add("DatabaseDtuMax", $tagData.setData.DatabaseDtuMax) : $null | Out-Null
                    # $tagData.setData.StorageMB ? $config.Add("StorageMB", $tagData.setData.StorageMB) : $null | Out-Null
                }
                'vCore' {
                    Write-Host "SetData vCore Mode"
                    $config = @{
                        Edition             = $tagData.setData.Edition
                        ComputeGeneration   = $tagData.setData.ComputeGeneration
                        VCore               = $tagData.setData.VCore
                    }

                    if ( $tagData.setData.DatabaseVCoreMin ) { $config.Add("DatabaseCapacityMin", $tagData.setData.DatabaseCapacityMin) }
                    if ( $tagData.setData.DatabaseVCoreMax ) { $config.Add("DatabaseCapacityMax", $tagData.setData.DatabaseCapacityMax) }
                    if ( $tagData.setData.StorageMB ) { $config.Add("StorageMB", $tagData.setData.StorageMB) }
                    if ( $tagData.setData.LicenseType ) { $config.Add("LicenseType", $tagData.setData.LicenseType) }

                    # $tagData.setData.DatabaseVCoreMin ? $config.Add("DatabaseCapacityMin", $tagData.setData.DatabaseCapacityMin) : $null | Out-Null
                    # $tagData.setData.DatabaseVCoreMax ? $config.Add("DatabaseCapacityMax", $tagData.setData.DatabaseCapacityMax) : $null | Out-Null
                    # $tagData.setData.StorageMB ? $config.Add("StorageMB", $tagData.setData.StorageMB) : $null | Out-Null
                    # $tagData.setData.LicenseType ? $config.Add("LicenseType", $tagData.setData.LicenseType) : $null | Out-Null
                }
                Default {
                    Write-Host "Error - Config"
                    Exit
                }
            }

            Write-Host "Compiling save data..."
            switch ( $editionMap[$graphData.sku.tier] ) {
                'DTU' {
                    Write-Host "SaveData DTU Mode"
                    $saveData = @{
                        Edition             = $graphData.sku.tier
                        Dtu                 = $graphData.sku.capacity
                        DatabaseDtuMin      = $graphData.properties.perDatabaseSettings.minCapacity -as [int]
                        DatabaseDtuMax      = $graphData.properties.perDatabaseSettings.maxCapacity -as [int]
                        StorageMB           = $graphData.properties.maxSizeBytes / 1MB
                    }
                }
                'vCore' {
                    Write-Host "SaveData vCore Mode"
                    $saveData = @{
                        Edition             = $graphData.sku.tier
                        ComputeGeneration   = $graphData.sku.family
                        VCore               = $graphData.sku.capacity
                        DatabaseVCoreMin    = $graphData.properties.perDatabaseSettings.minCapacity -as [int]
                        DatabaseVCoreMax    = $graphData.properties.perDatabaseSettings.maxCapacity -as [int]
                        StorageMB           = $graphData.properties.maxSizeBytes / 1MB
                        LicenseType         = $graphData.properties.licenseType
                    }
                }
                Default {
                    Write-Host "Error - SaveData"
                    Exit
                }
            }

            Write-Host "Folding in base data..."
            $config += $baseData
            $tags += Set-SaveTags $saveData
        }
    }

    # Scale the SQL Database
    try {
        Set-AzSqlElasticPool @config -Tags $tags
    }
    catch {
        Write-Host "Error scaling SQL Elastic Pool: $($graphData.name)"
        Write-Host "($($Error.exception.GetType().fullname)) - $($PSItem.ToString())"
        # throw $PSItem
        Exit
    }
    
    Write-Host "Scaler function has completed successfully!"
}

function Set-SaveTags {
    param (
        $inTags
    )

    $outTags = @{ }
    $inTags.keys | ForEach-Object { $outTags += @{("saveState-" + $_) = $inTags[$_] } }
    
    return $outTags
}

Export-ModuleMember -Function Update-Resource
