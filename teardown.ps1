#####################################################################
##
## Azure Custom Autoscaler Teardown Script
##
#####################################################################

# $parameters = Get-Content ./deployParams.json | ConvertFrom-Json
# $Name = $parameters.Name.ToLower()
$name = Read-Host "Enter name of resource group to teardown"
$logFile = "./teardown_$(get-date -format `"yyyyMMddhhmmsstt`").log"

# Set preference variables
$ErrorActionPreference = "Stop"

# Obtain subbuilder resource group object
$rg = Get-AzResourceGroup -Name $name -ErrorAction SilentlyContinue
if ($rg) {
    try {
        # Delete resource group
        Write-Host "INFO: Deleting Resource Group: $name" -ForegroundColor green        
        $rg | Remove-AzResourceGroup -Force

    }
    catch {
        $_ | Out-File -FilePath $logFile -Append
        Write-Host "ERROR: Deletion of Resouce Group: $name has failed due to an exception, see $logFile for detailed information!" -ForegroundColor red
        exit 

    }
} else {
    Write-Warning -Message "Resource Group, $name, no longer exists"

}

Write-Host "INFO: Autoscale infrastructure has been cleaned up!" -ForegroundColor green
