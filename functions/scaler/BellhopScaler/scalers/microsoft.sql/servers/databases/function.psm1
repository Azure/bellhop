#
# SQL DATABASE SCALE FUNCTION
#
function Update-Resource {
    param (
        [Parameter(Mandatory=$true)]
        [Object]
        $graphData,

        [Parameter(Mandatory=$true)]
        [Hashtable]
        $tagData,

        [Parameter(Mandatory=$true)]
        [String]
        $direction
    )

    # Set preference variables
    $ErrorActionPreference = "Stop"
			
    $baseData = @{
        ResourceGroupName   = $graphData.resourceGroup
        ServerName          = $($graphData.id | Select-String -Pattern '(?<=servers/).*(?=/databases)').Matches.Value
        DatabaseName        = $graphData.name
    }

    $config = @{}
    $saveData = @{}
    $tags = $tagData.tags

    switch ( $direction ) {
        'up' {
            Write-Host "Scaling SQL Databse: '$($graphData.name)' to Service Objective: '$($tagData.saveData.RequestedServiceObjectiveName)'"

            $config = $baseData + $tagData.saveData
        }

        'down' {
            Write-Host "Scaling SQL Databse: '$($graphData.name)' to Service Objective: '$($tagData.setData.RequestedServiceObjectiveName)'"

            # Required configuration tags
            $config = @{
                RequestedServiceObjectiveName = $tagData.setData.RequestedServiceObjectiveName
            }

            # Optional configuration tags
            if ( $tagData.setData.LicenseType ) { $config.Add("LicenseType", $tagData.setData.LicenseType) }
            if ( $tagData.setData.MaxSizeBytes ) { $config.Add("MaxSizeBytes", $tagData.setData.MaxSizeBytes) }

            # Data to be stored as Tags to remember the current stats of the object
            $saveData = @{
                RequestedServiceObjectiveName = $graphData.properties.requestedServiceObjectiveName
                MaxSizeBytes = $graphData.properties.maxSizeBytes
            }

            if ( $graphData.properties.licenseType ) { $saveData.Add("LicenseType", $graphData.properties.licenseType) }

            $config += $baseData
            $tags += Set-SaveTags $saveData $tagData.map
        }
    }

    # Scale the SQL Database
    try {
        Set-AzSqlDatabase @config -Tags $tags
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

    $outTags = @{}
    $inTags.keys | ForEach-Object {$outTags += @{($tagMap.save + $_)=$inTags[$_]}}
    
    return $outTags
}

Export-ModuleMember -Function Update-Resource
