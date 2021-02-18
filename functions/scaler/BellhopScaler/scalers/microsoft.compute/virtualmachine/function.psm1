#
# AZURE VIRTUAL MACHINE SCALE FUNCTION
#

function Update-Resource {
    param (
        [Parameter(Mandatory = $true)]
        [Object]
        $graphData,

        [Parameter(Mandatory = $true)]
        [Hashtable]
        $tagData,

        [Parameter(Mandatory = $true)]
        [String]
        $direction
    )

    # Set preference variables
    $ErrorActionPreference = "Stop"

    $baseData = @{
        ResourceGroupName = $graphData.resourceGroup
    }

    $config = @{ }
    $tags = $tagData.tags

    switch ($direction) {
        'up' {
            Write-Host "Scaling VM Size: '$($graphData.properties.hardwareProfile.vmSize)' to Size: '$($tagData.saveData.VMSize)'"

            $vmName = $graphData.name
            $vmLocation = $graphData.location
            $vmSize = $tagData.saveData.VMSize

            $vmData = @{
                VM = Get-VMObject $vmName $vmLocation $vmSize
            }
            
            $config = $baseData + $vmData
        }

        'down' {
            Write-Host "Scaling VM Size: '$($graphData.properties.hardwareProfile.vmSize)' to Tier: '$($tagData.setData.VMSize)'"

            $vmName = $graphData.name
            $vmLocation = $graphData.location
            $vmSize = $tagData.setData.VMSize

            $config = @{
                VM = Get-VMObject $vmName $vmLocation $vmSize
            }

            $saveData = @{
                VMSize = $graphData.properties.hardwareProfile.vmSize
            }

            $config += $baseData
            $tags += Set-SaveTags $saveData
        }
    }

    # Scale the Virtual Machine Size
    try {
        Update-AzVM @config -Tag $tags
    }
    catch {
        Write-Host "Error scaling Virtual Machine Size: $($graphData.name)"
        Write-Host "($($Error.exception.GetType().fullname)) - $($PSItem.ToString())"
        # throw $PSItem
        Exit
    }
    
    Write-Host "Scaler function has completed successfully!"
}

function Get-VMObject {
    param(
        $vmName,
        $vmLocation,
        $vmSize
    )

    $newVMObj = New-Object -TypeName Microsoft.Azure.Commands.Compute.Models.PSVirtualMachine
    $newVMObj.HardwareProfile = New-Object -TypeName Microsoft.Azure.Management.Compute.Models.HardwareProfile
    $newVMObj.Name = $vmName
    $newVMObj.Location = $vmLocation
    $newVMObj.HardwareProfile.VmSize = $vmSize

    return $newVMObj
}

function Set-SaveTags {
    param (
        $inTags
    )

    $outTags = @{ }
    $inTags.keys | ForEach-Object { $outTags += @{("saveState-" + $_) = $inTags[$_] } }
    
    return $outTags
}

Export-ModuleMember -Function Update-Resource
