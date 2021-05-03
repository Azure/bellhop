# $container = New-PesterContainer -Path './e2etest/BellhopScaler/e2etest.Tests.ps1' -Data @{ Location = "westus2"; serviceName = "Microsoft.Web"; templateLocation = "./e2etest/BellhopScaler/microsoft.web/asp.json"; settingToProjectScaledDown = "sku.name"; targetSettingScaledDown = "B1"; settingToProjectScaledUp = "sku.name" ; targetSettingScaledup = "S1"}
# Invoke-Pester -Container $container -Output Detailed


param ($Location = "westus2", 
  $serviceName,
  $templateLocation,
  $settingToProjectScaledDown,
  $targetSettingScaledDown,
  $settingToProjectScaledUp,
  $targetSettingScaledup
)

BeforeAll {
  # your function
  function Try-ResourceGraphQuery {
    param([Parameter(Mandatory = $true)]$query, 
      [Parameter(Mandatory = $true)]$maxRetries, 
<<<<<<< HEAD
      [Parameter(Mandatory = $false)]$targetState = $null)
=======
      [Parameter(Mandatory = $false)]$targetState=$null)
>>>>>>> pester tests
    $i = 0
    $objectInGraph = $null

    do {
      $i += 1
<<<<<<< HEAD
      if ($i -ne 1) {
=======
      if ($i -ne 1){
>>>>>>> pester tests
        Start-Sleep -s 30
      }
      Write-Host "Querying resource graph: $i out of $maxRetries"
      $objectInGraph = Search-AzGraph -Query $query
      Write-Host "got following object: " $objectInGraph
      # If targetstate is set, $targetmet is the comparison of objectingraph and targetstate. If targetstate not set, targetmet is always true.
      $targetMet = ($null -ne $targetState) ? ($objectInGraph.target -ne $targetState): $false
    }
    while ((($null -eq $objectInGraph) -or $targetMet) -and ($i -ne $maxRetries))

    return $objectInGraph
  }

<<<<<<< HEAD
  function Scale-Resource {
    param([Parameter(Mandatory = $true)]$direction,
      [Parameter(Mandatory = $true)]$resourceId)

    # Getting object from graph again. Should work in 1 try after previous test succeeded:
    $resourceGraphQuery = "resources | where id =~ '$resourceId'"
    $objectInGraph = Try-ResourceGraphQuery -query $resourceGraphQuery -maxRetries 2
    # Then sending message to queue to test scale down
    $staccName = $AppName + "stgacct"
        
    $storageAccount = Get-AzStorageAccount -ResourceGroupName $bellhopResourceGroupName -Name $staccName
    $ctx = $storageAccount.Context
        
    $queue = Get-AzStorageQueue –Name $queueName –Context $ctx
        
    $queueMessageRaw = @{ direction = $direction; debug = $False; graphResults = $objectInGraph }
    $queueMessageJson = $queueMessageRaw | ConvertTo-Json
        
    $queueMessage = [Microsoft.Azure.Storage.Queue.CloudQueueMessage]::new($queueMessageJson)
        
    # Add a new message to the queue
    Write-Host "Sending message to queue"
    $queue.CloudQueue.AddMessageAsync($QueueMessage)
  }

=======
>>>>>>> pester tests
  $TimeStamp = Get-Date -Format "yyyymmddHHmm"
  $AppName = "bhe2e$TimeStamp"
  $ScaledServiceResourceGroupName = "bhe2e-$serviceName-$TimeStamp"
  $ScaledServiceDeploymentName = "bhe2e-scalertest-$TimeStamp"
  $bellhopResourceGroupName = $appname + "-rg"
  $queueName = "autoscale"

}

# Pester tests
Describe 'Test-Scaler' {
  Context "Deploying infrastructure" {
    It "Bellhop core infra deploys successfully in $Location" {
      $deploy = New-AzDeployment `
        -Name bellhop-e2etest-$TimeStamp `
        -location $Location `
        -TemplateFile templates/azuredeploy.json `
        -appName $AppName `
        -ErrorVariable deploymentError

      $deploy.ProvisioningState | Should -Be "Succeeded" 
    }
    It "Service infra should deploy successfully in $Location" {
      New-AzResourceGroup -Name $ScaledServiceResourceGroupName -Location $Location
      $deploy = New-AzResourceGroupDeployment -TemplateFile $templateLocation `
        -ResourceGroupName $ScaledServiceResourceGroupName `
        -Name $ScaledServiceDeploymentName
      $resourceId = $deploy.Outputs.resourceId.Value
      $resourceId | Should -Not -Be $null
    }
    It "Service infra object should be returned by ARG" {
      # First getting resourceId based on deployment
      $resourceId = (Get-AzResourceGroupDeployment -ResourceGroupName $ScaledServiceResourceGroupName `
          -Name $ScaledServiceDeploymentName).Outputs.resourceId.Value
      
      $resourceGraphQuery = "resources | where id =~ '$resourceId'"
      $objectInGraph = Try-ResourceGraphQuery -query $resourceGraphQuery -maxRetries 30
      
      $objectInGraph | Should -Not -Be $null
    }
  }
  Context "Testing scale down" {
    It "It should scale down successfully" {
      # First getting resourceId based on deployment
      $resourceId = (Get-AzResourceGroupDeployment -ResourceGroupName $ScaledServiceResourceGroupName `
          -Name $ScaledServiceDeploymentName).Outputs.resourceId.Value
<<<<<<< HEAD
      
      Scale-Resource -resourceId $resourceId -direction "down"
      
      $scaledDownresourceGraphQuery = "resources | where id =~ '$resourceId' | project target = $settingToProjectScaledDown"
      # Max retries set higher than usual to 60, since timing for scaler to connect to queue can vary
      $objectInGraph = Try-ResourceGraphQuery -query $scaledDownresourceGraphQuery -maxRetries 60 -targetState $targetSettingScaledDown
=======
      # Also, getting object from graph again. Should work in 1 try after previous test succeeded:
      $resourceGraphQuery = "resources | where id =~ '$resourceId'"
      $objectInGraph = Try-ResourceGraphQuery -query $resourceGraphQuery -maxRetries 2
      # Then sending message to queue to test scale down
      $staccName = $AppName + "stgacct"
          
      $storageAccount = Get-AzStorageAccount -ResourceGroupName $bellhopResourceGroupName -Name $staccName
      $ctx = $storageAccount.Context
          
      $queue = Get-AzStorageQueue –Name $queueName –Context $ctx
          
      $queueMessageRaw = @{ direction = "down"; debug = $False; graphResults = $objectInGraph }
      $queueMessageJson = $queueMessageRaw | ConvertTo-Json
          
      $queueMessage = [Microsoft.Azure.Storage.Queue.CloudQueueMessage]::new($queueMessageJson)
          
      # Add a new message to the queue
      Write-Host "Sending message to queue"
      $queue.CloudQueue.AddMessageAsync($QueueMessage)

      
      $scaledDownresourceGraphQuery = "resources | where id =~ '$resourceId' | project target = $settingToProjectScaledDown"
      $objectInGraph = Try-ResourceGraphQuery -query $scaledDownresourceGraphQuery -maxRetries 30 -targetState $targetSettingScaledDown
>>>>>>> pester tests
      $objectInGraph.target | Should -be $targetSettingScaledDown
    }
    It "savestate tags should appear on ARG" {
      # First getting resourceId based on deployment
      $resourceId = (Get-AzResourceGroupDeployment -ResourceGroupName $ScaledServiceResourceGroupName `
          -Name $ScaledServiceDeploymentName).Outputs.resourceId.Value

      $saveStateQuery = "resources | where id =~ '$resourceId' | where tags contains 'saveState' "
      $objectInGraph = Try-ResourceGraphQuery -query $saveStateQuery -maxRetries 30
      
      $objectInGraph | Should -Not -Be $null
    }
  }

  Context "Testing scaling back up" {
    It "It should scale back up successfully" {
      # First getting resourceId based on deployment
      $resourceId = (Get-AzResourceGroupDeployment -ResourceGroupName $ScaledServiceResourceGroupName `
          -Name $ScaledServiceDeploymentName).Outputs.resourceId.Value
<<<<<<< HEAD
      
      Scale-Resource -resourceId $resourceId -direction "up"
=======
      $resourceGraphQuery = "resources | where id =~ '$resourceId'"
      $objectInGraph = Try-ResourceGraphQuery -query $resourceGraphQuery -maxRetries 2
    
      $staccName = $AppName + "stgacct"
      $storageAccount = Get-AzStorageAccount -ResourceGroupName $bellhopResourceGroupName -Name $staccName
      $ctx = $storageAccount.Context

      $queue = Get-AzStorageQueue –Name $queueName –Context $ctx

      $queueMessageRaw = @{ direction = "up"; debug = $False; graphResults = $objectInGraph }
      $queueMessageJson = $queueMessageRaw | ConvertTo-Json

      $queueMessage = [Microsoft.Azure.Storage.Queue.CloudQueueMessage]::new($queueMessageJson)
      Write-Host "Sending message to queue"
      $queue.CloudQueue.AddMessageAsync($QueueMessage)

>>>>>>> pester tests
    
      $scaledUpresourceGraphQuery = "resources | where id =~ '$resourceId' | project target = $settingToProjectScaledUp"
    
      $objectInGraph = Try-ResourceGraphQuery -query $scaledUpresourceGraphQuery -maxRetries 30 -targetState $targetSettingScaledup
      $objectInGraph.Target | Should -be $targetSettingScaledup
    } 
  }
}

AfterAll {
  Write-Host "Deleting Bellhop"
  Remove-AzResourceGroup -Name $bellhopResourceGroupName -Force
  Write-Host "Deleting Service"
  Remove-AzResourceGroup -Name $ScaledServiceResourceGroupName -Force
  # Figure out how to delete role assignments.
}