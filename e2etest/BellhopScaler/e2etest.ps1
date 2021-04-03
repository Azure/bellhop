# note to self: need to figure out how to best install resourceGraph 
# Install-Module -Name Az.ResourceGraph
<<<<<<< HEAD
<<<<<<< HEAD
## example: ./e2etest/BellhopScaler/e2etest.ps1 -serviceName Microsoft.Web -templateLocation ./e2etest/BellhopScaler/microsoft.web/asp.json -settingToProjectScaledDown "sku.name" -targetSettingScaledDown "B1" -settingToProjectScaledUp "sku.name" -targetSettingScaledup "S1"
param ($Location="westus2", 
    $serviceName,
    $templateLocation,
    $settingToProjectScaledDown,
    $targetSettingScaledDown,
    $settingToProjectScaledUp,
    $targetSettingScaledup
=======

=======
## example: ./e2etest/BellhopScaler/e2etest.ps1 -serviceName Microsoft.Web -templateLocation ./e2etest/BellhopScaler/microsoft.web/asp.json -settingToProjectScaledDown "sku.name" -targetSettingScaledDown "B1" -settingToProjectScaledUp "sku.name" -targetSettingScaledup "S1"
>>>>>>> e2e test for app service
param ($Location="westus2", 
    $serviceName,
<<<<<<< HEAD
    $templateLocation
>>>>>>> testing
=======
    $templateLocation,
    $settingToProjectScaledDown,
    $targetSettingScaledDown,
    $settingToProjectScaledUp,
    $targetSettingScaledup
>>>>>>> e2etest
)

# Inputs:
# 1: ARM template of resource to create (e.g. app service plan / SQL DB / ...) --> outputs resource ID of resource that got created
# 2: Message to send to the queue
# 3: Target state object JSON resoruce graph expected result?
# 4: Resource graph properties to project? Array of strings?
# 5: Location
# Deploy Bellhop
$TimeStamp = Get-Date -Format "yyyymmddHHmm"
$AppName = "bhe2e$TimeStamp"
$ScaledServiceResourceGroupName = "bhe2e-$serviceName-$TimeStamp"
$ScaledServiceDeploymentName = "bhe2e-scalertest-$TimeStamp"
$bellhopResourceGroupName = $appname+"-rg"

<<<<<<< HEAD
<<<<<<< HEAD
write-output "##########################"
write-output "Deploying bellhop"
write-output "##########################"
New-AzDeployment `
<<<<<<< HEAD
    -Name bellhop-e2etest-$TimeStamp `
    -location $Location `
    -TemplateFile templates/azuredeploy.json `
=======
Write-Output "Deploying Bellhop"
=======
write-output "##########################"
write-output "Deploying bellhop"
write-output "##########################"
>>>>>>> e2etest
New-AzSubscriptionDeployment `
=======
>>>>>>> e2e test for app service
    -Name bellhop-e2etest-$TimeStamp `
<<<<<<< HEAD
    -Location $Location `
    -TemplateFile templates/infra.json `
>>>>>>> testing
=======
    -location $Location `
    -TemplateFile templates/azuredeploy.json `
>>>>>>> testing
    -appName $AppName
# Create new resource group for test resource
New-AzResourceGroup -Name $ScaledServiceResourceGroupName -Location $Location

# Create new resource - doesn't need tags, so engine doesn't on error pick it up.
<<<<<<< HEAD
<<<<<<< HEAD
write-output "##########################"
write-output "Deploying resource " $serviceName
write-output "##########################"
=======
Write-Output "Deploying resource"
>>>>>>> testing
=======
write-output "##########################"
write-output "Deploying resource " $serviceName
write-output "##########################"
>>>>>>> e2etest
$deploy = New-AzResourceGroupDeployment -TemplateFile $templateLocation `
    -ResourceGroupName $ScaledServiceResourceGroupName `
    -Name $ScaledServiceDeploymentName
$resourceId = $deploy.Outputs.resourceId.Value
write-output "Resource ID is $resourceId"

# Get object from resource graph
<<<<<<< HEAD
<<<<<<< HEAD
write-output "##########################"
write-output "Getting info from graph"
write-output "##########################"
$resourceGraphQuery = "resources | where id =~ '$resourceId'"
$i = 0
$max = 30
$objectInGraph = $null
=======

=======
write-output "##########################"
write-output "Getting info from graph"
write-output "##########################"
>>>>>>> e2etest
$resourceGraphQuery = "resources | where id =~ '$resourceId'"
$i = 0
$max = 30
<<<<<<< HEAD
>>>>>>> testing
=======
$objectInGraph = $null
>>>>>>> e2etest
do {
    $i+=1
    Start-Sleep -s 10
    Write-Output "Querying resource graph: $i out of $max "
    $objectInGraph = Search-AzGraph -Query $resourceGraphQuery
<<<<<<< HEAD
<<<<<<< HEAD
    Write-Output "got following object: " $objectInGraph
=======
    Write-Output "got following object: " + $objectInGraph
>>>>>>> testing
=======
    Write-Output "got following object: " $objectInGraph
>>>>>>> e2etest
}
while(($null -eq $objectInGraph) -and ($i -ne $max) )
if ($i -eq $max){
    Write-Output "Didn't get resource graph results. Cancelling test."
    Write-Output "Deleting resources"
    Remove-AzResourceGroup -Name $ScaledServiceResourceGroupName -Force
    Remove-AzResourceGroup -Name $bellhopResourceGroupName -Force
    exit 1
}

# Send message to queue to scale resource down
<<<<<<< HEAD
<<<<<<< HEAD
write-output "##########################"
write-output "Sending scale down message to queue"
write-output "##########################"
=======
>>>>>>> testing
=======
write-output "##########################"
write-output "Sending scale down message to queue"
write-output "##########################"
>>>>>>> e2etest
$staccName = $AppName+"stgacct"
$queueName = "autoscale"

$storageAccount = Get-AzStorageAccount -ResourceGroupName $bellhopResourceGroupName -Name $staccName
$ctx = $storageAccount.Context

$queue = Get-AzStorageQueue –Name $queueName –Context $ctx

$queueMessageRaw = @{ direction = "down"; debug = $False; graphResults = $objectInGraph}
$queueMessageJson = $queueMessageRaw | ConvertTo-Json

$queueMessage = [Microsoft.Azure.Storage.Queue.CloudQueueMessage]::new($queueMessageJson)

# Add a new message to the queue
Write-Output "Sending message to queue"
$queue.CloudQueue.AddMessageAsync($QueueMessage)

<<<<<<< HEAD
<<<<<<< HEAD
=======
>>>>>>> e2etest
# Keep getting state of resource until scaled down. Error after 30 minutes
# $settingToProjectScaledDown = "sku.name"
write-output "##########################"
write-output "Testing the scale down."
write-output "##########################"
<<<<<<< HEAD

$scaledDownresourceGraphQuery = "resources | where id =~ '$resourceId' | project target = $settingToProjectScaledDown"
write-output "query is: $scaledDownresourceGraphQuery"
$i = 0
$max = 30
do {
    $i+=1
    Start-Sleep -s 60
    Write-Output "Querying resource graph: $i out of $max "
    $objectInGraph = Search-AzGraph -Query $scaledDownresourceGraphQuery
    Write-Output "got following result: $objectInGraph" 
}
while(($objectInGraph.target -ne $targetSettingScaledDown) -and ($i -ne $max) )

if(($objectInGraph.target -ne $targetSettingScaledDown) -or ($i -eq $max)){
    write-output "Error comparing scaled down resource."
    write-output "Timed out after $i out of $max"
    Write-Output "SKU on resource is " $objectInGraph.target
    exit 1
}
# Wait for save-state tags to appear
write-output "##########################"
write-output "Waiting for save-state tags on resource graph"
write-output "##########################"

$saveStateQuery = "resources | where id =~ '$resourceId' | where tags contains 'saveState' "
write-output $saveStateQuery
$i=0
do {
    $i+=1
    Start-Sleep -s 60
    Write-Output "Querying resource graph: $i out of $max "
    $objectInGraph = Search-AzGraph -Query $saveStateQuery
    Write-Output "got following result: " + $objectInGraph
}
while(($null -eq $objectInGraph) -and ($i -ne $max) )

if($i -eq $max){
    write-output "savestate tags not discovered"
    exit 1
}


# Send new message to queue to scale resource up
write-output "##########################"
write-output "Scaling back up"
write-output "##########################"
$resourceGraphQuery = "resources | where id =~ '$resourceId'"
$objectInGraph = Search-AzGraph -Query $resourceGraphQuery

$queueMessageRaw = @{ direction = "up"; debug = $False; graphResults = $objectInGraph}
$queueMessageJson = $queueMessageRaw | ConvertTo-Json

$queueMessage = [Microsoft.Azure.Storage.Queue.CloudQueueMessage]::new($queueMessageJson)
Write-Output "Sending message to queue"
$queue.CloudQueue.AddMessageAsync($QueueMessage)

# Keep getting state of resource until scaled up. Error after 30 minutes.
write-output "##########################"
write-output "Testing the scale up."
write-output "##########################"

$scaledUpresourceGraphQuery = "resources | where id =~ '$resourceId' | project target = $settingToProjectScaledUp"
write-output "query is: $scaledUpresourceGraphQuery"
$i = 0
$max = 30
do {
    $i+=1
    Start-Sleep -s 60
    Write-Output "Querying resource graph: $i out of $max "
    $objectInGraph = Search-AzGraph -Query $scaledDownresourceGraphQuery
    Write-Output "got following result: " + $objectInGraph
}
while(($objectInGraph.target -ne $targetSettingScaledup) -and ($i -ne $max) )

if(($objectInGraph.target -ne $targetSettingScaledup) -or ($i -eq $max)){
    write-output "Error comparing scaled down resource."
    write-output "Timed out after $i out of $max"
    Write-Output "SKU on resource is " $objectInGraph.target
    exit 1
}
Write-Output "scaled back up succesfully"
=======
# Keep getting state of resource until scaled down. Error after 10 minutes
=======
>>>>>>> e2etest

$scaledDownresourceGraphQuery = "resources | where id =~ '$resourceId' | project target = $settingToProjectScaledDown"
write-output "query is: $scaledDownresourceGraphQuery"
$i = 0
$max = 30
do {
    $i+=1
    Start-Sleep -s 60
    Write-Output "Querying resource graph: $i out of $max "
    $objectInGraph = Search-AzGraph -Query $scaledDownresourceGraphQuery
    Write-Output "got following result: $objectInGraph" 
}
while(($objectInGraph.target -ne $targetSettingScaledDown) -and ($i -ne $max) )

if(($objectInGraph.target -ne $targetSettingScaledDown) -or ($i -eq $max)){
    write-output "Error comparing scaled down resource."
    write-output "Timed out after $i out of $max"
    Write-Output "SKU on resource is " $objectInGraph.target
    exit 1
}
# Wait for save-state tags to appear
write-output "##########################"
write-output "Waiting for save-state tags on resource graph"
write-output "##########################"

$saveStateQuery = "resources | where id =~ '$resourceId' | where tags contains 'saveState' "
write-output $saveStateQuery
$i=0
do {
    $i+=1
    Start-Sleep -s 60
    Write-Output "Querying resource graph: $i out of $max "
    $objectInGraph = Search-AzGraph -Query $saveStateQuery
    Write-Output "got following result: " + $objectInGraph
}
while(($null -eq $objectInGraph) -and ($i -ne $max) )

if($i -eq $max){
    write-output "savestate tags not discovered"
    exit 1
}


# Send new message to queue to scale resource up
write-output "##########################"
write-output "Scaling back up"
write-output "##########################"
$resourceGraphQuery = "resources | where id =~ '$resourceId'"
$objectInGraph = Search-AzGraph -Query $resourceGraphQuery

$queueMessageRaw = @{ direction = "up"; debug = $False; graphResults = $objectInGraph}
$queueMessageJson = $queueMessageRaw | ConvertTo-Json

$queueMessage = [Microsoft.Azure.Storage.Queue.CloudQueueMessage]::new($queueMessageJson)
Write-Output "Sending message to queue"
$queue.CloudQueue.AddMessageAsync($QueueMessage)

# Keep getting state of resource until scaled up. Error after 30 minutes.
write-output "##########################"
write-output "Testing the scale up."
write-output "##########################"

$scaledUpresourceGraphQuery = "resources | where id =~ '$resourceId' | project target = $settingToProjectScaledUp"
write-output "query is: $scaledUpresourceGraphQuery"
$i = 0
$max = 30
do {
    $i+=1
    Start-Sleep -s 60
    Write-Output "Querying resource graph: $i out of $max "
    $objectInGraph = Search-AzGraph -Query $scaledDownresourceGraphQuery
    Write-Output "got following result: " + $objectInGraph
}
while(($objectInGraph.target -ne $targetSettingScaledup) -and ($i -ne $max) )

<<<<<<< HEAD
>>>>>>> testing
=======
if(($objectInGraph.target -ne $targetSettingScaledup) -or ($i -eq $max)){
    write-output "Error comparing scaled down resource."
    write-output "Timed out after $i out of $max"
    Write-Output "SKU on resource is " $objectInGraph.target
    exit 1
}
<<<<<<< HEAD
>>>>>>> e2etest
=======
Write-Output "scaled back up succesfully"
>>>>>>> e2e test for app service
