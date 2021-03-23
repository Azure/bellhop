# note to self: need to figure out how to best install resourceGraph 
# Install-Module -Name Az.ResourceGraph

param ($Location="westus2", 
    $serviceName,
    $templateLocation
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

Write-Output "Deploying Bellhop"
New-AzSubscriptionDeployment `
    -Name bellhop-e2etest-$TimeStamp `
    -Location $Location `
    -TemplateFile templates/infra.json `
    -appName $AppName
# Create new resource group for test resource
New-AzResourceGroup -Name $ScaledServiceResourceGroupName -Location $Location

# Create new resource - doesn't need tags, so engine doesn't on error pick it up.
Write-Output "Deploying resource"
$deploy = New-AzResourceGroupDeployment -TemplateFile $templateLocation `
    -ResourceGroupName $ScaledServiceResourceGroupName `
    -Name $ScaledServiceDeploymentName
$resourceId = $deploy.Outputs.resourceId.Value
write-output "Resource ID is $resourceId"

# Get object from resource graph

$resourceGraphQuery = "resources | where id =~ '$resourceId'"
#question: do we want this to time out?
$i = 0
$max = 30
do {
    $i+=1
    Start-Sleep -s 10
    Write-Output "Querying resource graph: $i out of $max "
    $objectInGraph = Search-AzGraph -Query $resourceGraphQuery
    Write-Output "got following object: " + $objectInGraph
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

# Keep getting state of resource until scaled down. Error after 10 minutes





# Send new message to queue to scale resource up

# Keep getting state of resource until scaled up. Error after 10 minutes.


