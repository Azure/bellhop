#####################################################################
##
## Azure Bellhop Teardown Script
##
#####################################################################

$rgName = Read-Host "Enter name of resource group to teardown"
$logFile = "./logs/remove_$(get-date -format `"yyyyMMddhhmmsstt`").log"

# Set preference variables
$ErrorActionPreference = "Stop"

# Obtain subbuilder resource group object
$resourceGroup = Get-AzResourceGroup -Name $rgName -ErrorAction SilentlyContinue

if ($resourceGroup) {
    try {
        # Delete resource group
        Write-Host "INFO: Deleting Resource Group: $rgName" -ForegroundColor green        
        $removeRg = Remove-AzResourceGroup -Name $rgName -Force

    }
    catch {
        $_ | Out-File -FilePath $logFile -Append
        $removeRg | Out-File -FilePath $logFile -Append
        Write-Host "ERROR: Deletion of Resouce Group: $rgName has failed due to an exception, see $logFile for detailed information!" -ForegroundColor red
        exit
    }
} else {
    Write-Warning -Message "Resource Group, $rgName, no longer exists"

}

Write-Host "INFO: Bellhop infrastructure has been cleaned up!" -ForegroundColor green
