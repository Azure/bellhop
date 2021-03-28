# Input bindings are passed in via param block.
param($QueueItem, $TriggerMetadata)

function Initialize-TagData {
    param (
        $inTags,
        $tagMap
    )

    $tags = @{}
    $setData = @{}
    $saveData = @{}

    foreach ($key in $inTags.Keys) {
        if ($key -Match $tagMap.set) {
            $setKey = $key -replace $tagMap.set, ""
            $setData += @{$setKey = $inTags[$key] }
            $tags += @{$key = $inTags[$key] }
        }
        elseif ($key -Match $tagMap.save) {
            $saveKey = $key -replace $tagMap.save, ""
            $saveData += @{$saveKey = $inTags[$key] }
        }
        else {
            $tags += @{$key = $inTags[$key] }
        }
    }

    $tagData = @{
        "tags"      = $tags
        "map"       = $tagMap
        "setData"   = $setData
        "saveData"  = $saveData
    }

    return $tagData
}

# Set preference variables
$ErrorActionPreference = "Stop"

# Write out the queue message and insertion time to the information log
Write-Host "PowerShell queue trigger function processed work item: $QueueItem"
Write-Host "Queue item insertion time: $($TriggerMetadata.InsertionTime)"

# Importing correct powershell module based on resource type
Write-Host "Importing scaler for: $($QueueItem.graphResults.type)"

try {
    $modulePath = Join-Path $PSScriptRoot -ChildPath "scalers\$($QueueItem.graphResults.type)\function.psm1"
    Import-Module -Name $modulePath
}
catch {
    Write-Host "Error loading the target scaler!"

    if ( $QueueItem.debug ) {
        Write-Host "Content of Scalers Folder"
        Write-Host "========================="
        $dirs = Get-ChildItem $PSScriptRoot -Recurse | Where-Object FullName -Like "*function.psm1" | Select-Object FullName
        foreach ($dir in $dirs) { Write-Host $dir.FullName }
        Write-Host "========================="
    }

    Write-Host "($($Error.exception.GetType().fullname)) - $($PSItem.ToString())"
    # throw $PSItem
    Exit
}

# Set the current context to that of the target resources subscription
Write-Host "Setting the Subscription context: $($QueueItem.graphResults.subscriptionId)"

try {
    Set-AzContext -SubscriptionId $QueueItem.graphResults.subscriptionId | Out-Null
}
catch {
    Write-Host "Error setting the subscription context!"
    Write-Host "($($Error.exception.GetType().fullname)) - $($PSItem.ToString())"
    # throw $PSItem
    Exit
}

# Set Target and call correct powershell module based on resource type
Write-Host "Beginning operation to scale: '$($QueueItem.graphResults.id)' - ($($QueueItem.direction.ToUpper()))"

try {
    $tagData = Initialize-TagData $QueueItem.graphResults.tags $QueueItem.tagMap
    Update-Resource $QueueItem.graphResults $tagData $QueueItem.direction
}
catch {
    Write-Host "Error scaling resource!"
    Write-Host "($($Error.exception.GetType().fullname)) - $($PSItem.ToString())"
    # throw $PSItem
    Exit
}

Write-Host "Scaling operation has completed successfully for resource: '$($QueueItem.graphResults.id)'."
