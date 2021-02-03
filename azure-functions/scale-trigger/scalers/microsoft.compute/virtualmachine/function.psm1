#
# AZURE VIRTUAL MACHINE SCALE FUNCTION
# DOES NOT CURRENTLY WORK
#
function update-resource {
    param (
        [Object[]]$inputParams    
    )

    # Gather parameters from the input object
    Write-Host "Pulling required parameters from param object"
    $ResourceGroup = $inputParams.ResourceGroup
    $Name = $inputParams.Name
    $TargetSize = $inputParams.TargetSize

    # Scale VM
    try {
        Write-Host "INFO: Scaling VM: '$Name' to Size: '$TargetSize'" -ForegroundColor Green
        $vm = Get-AzVM -ResourceGroupName $resourceGroup -Name $Name
        $vm.HardwareProfile.VmSize = $TargetSize
        update-azvm -VM $vm 
    }
    catch {
        Write-Host "ERROR: Could not scale VM: $VM"
        Exit
    }
    Write-Host "INFO: Scaling operation has completed" -ForegroundColor Green
}