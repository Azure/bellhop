# Input bindings are passed in via param block.
param($QueueItem, $TriggerMetadata)

function Initialize-TagData {
    param (
        $inTags
    )

    $tags = @{}
    $setData = @{}
    $saveData = @{}

    foreach ($key in $inTags.Keys) {
        if ($key -Match "setState-") {
            $setKey = $key -replace "setState-", ""
            $setData += @{$setKey = $inTags[$key] }
            $tags += @{$key = $inTags[$key] }
        }
        elseif ($key -Match "saveState-") {
            $saveKey = $key -replace "saveState-", ""
            $saveData += @{$saveKey = $inTags[$key] }
        }
        else {
            $tags += @{$key = $inTags[$key] }
        }
    }

    $tagData = @{
        "tags"     = $tags
        "setData"  = $setData
        "saveData" = $saveData
    }

    return $tagData
}

# Set preference variables
$ErrorActionPreference = "Stop"

# Write out the queue message and insertion time to the information log.
Write-Host "PowerShell queue trigger function processed work item: $QueueItem"
Write-Host "Queue item insertion time: $($TriggerMetadata.InsertionTime)"

# Importing correct powershell module based on resource type
Write-Host "Importing scaler for: $($QueueItem.graphResults.type)"

try {
    Import-Module -Name "./scalers/$($QueueItem.graphResults.type)/function.psm1" #-ErrorAction Stop #SilentlyContinue -ErrorVariable abc
}
catch {
    Write-Host "Error loading the target scaler!"
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
    $tagData = Initialize-TagData $QueueItem.graphResults.tags
    Update-Resource $QueueItem.graphResults $tagData $QueueItem.direction
}
catch {
    Write-Host "Error scaling resource!"
    Write-Host "($($Error.exception.GetType().fullname)) - $($PSItem.ToString())"
    # throw $PSItem
    Exit
}

Write-Host "Scaling operation has completed successfully for resource: '$($QueueItem.graphResults.id)'."
