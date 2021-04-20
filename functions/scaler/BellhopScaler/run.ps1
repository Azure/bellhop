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

function Assert-Error {
    param (
        $err
    )

    $errorDetails = @{
        Type        = $err.Exception.GetType().fullname
        Message     = $err.Exception.Message
        StackTrace  = $err.Exception.StackTrace
    }

    $resourceDetails = @{
        Name                = $QueueItem.graphResults.name
        ResourceGroup       = $QueueItem.graphResults.resourceGroup
        SubscriptionId      = $QueueItem.graphResults.subscriptionId
        SubscriptionName    = $QueueItem.graphResults.subscriptionName
        Error               = $errorDetails
    }

    $errorMessage = $(@{ Exception = $resourceDetails } | ConvertTo-Json -Depth 4)
    Write-Host "ERRORDATA:" $errorMessage
    throw $err
}

# Set preference variables
$ErrorActionPreference = "Stop"

# Write out the queue message and insertion time to the information log
Write-Host "PowerShell queue trigger function processed work item: $QueueItem"
Write-Host "Queue item insertion time: $($TriggerMetadata.InsertionTime)"

# Set the current context to that of the target resources subscription
Write-Host "Setting the Subscription context: $($QueueItem.graphResults.subscriptionId)"

try {
    $Context = Set-AzContext -SubscriptionId $QueueItem.graphResults.subscriptionId

    if ( $QueueItem.debug ) {
        Write-Host "Context Detals:"
        Write-Host "========================="
        Write-Host "Subscription ID:" $Context.Subscription.Id
        Write-Host "Subscription Name:" $Context.Subscription.Name
        Write-Host "========================="
    }
}
catch {
    Write-Host "ERROR: Cannot set the deployment context!"
    Assert-Error $PSItem
}

# Importing correct powershell module based on resource type
Write-Host "Importing scaler for: $($QueueItem.graphResults.type)"

try {
    $modulePath = Join-Path $PSScriptRoot -ChildPath "scalers\$($QueueItem.graphResults.type)\function.psm1"
    Import-Module -Name $modulePath
}
catch {
    Write-Host "ERROR: Cannot load the target scaler!"

    if ( $QueueItem.debug ) {
        Write-Host "Available Scalers:"
        Write-Host "========================="
        $dirs = Get-ChildItem $PSScriptRoot -Recurse | Where-Object FullName -Like "*function.psm1" | Select-Object FullName
        foreach ($dir in $dirs) { Write-Host $dir.FullName }
        Write-Host "========================="
    }

    Assert-Error $PSItem
}

# Set Target and call correct powershell module based on resource type
Write-Host "Beginning operation to scale: '$($QueueItem.graphResults.id)' - ($($QueueItem.direction.ToUpper()))"

try {
    $tagData = Initialize-TagData $QueueItem.graphResults.tags $QueueItem.tagMap
    Update-Resource $QueueItem.graphResults $tagData $QueueItem.direction
}
catch {
    Write-Host "ERROR: Cannot scale the target resource!"
    Assert-Error $PSItem
}

Write-Host "Scaling operation has completed successfully for resource: '$($QueueItem.graphResults.id)'."
