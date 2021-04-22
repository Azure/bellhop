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

    # Import required supporting modules
    Import-Module Az.Compute

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

            $vmData = @{
                VM = Get-VMObject $graphData.name $graphData.location $tagData.saveData.VMSize
            }

            $config = $baseData + $vmData
        }

        'down' {
            Write-Host "Scaling VM Size: '$($graphData.properties.hardwareProfile.vmSize)' to Size: '$($tagData.setData.VMSize)'"

            $config = @{
                VM = Get-VMObject $graphData.name $graphData.location $tagData.setData.VMSize
            }

            $saveData = @{
                VMSize = $graphData.properties.hardwareProfile.vmSize
            }

            $config += $baseData
            $tags += Set-SaveTags $saveData $tagData.map
        }
    }

    # Scale the Virtual Machine Size
    try {
        Update-AzVM @config -Tag $tags
    }
    catch {
        throw $PSItem
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
        $inTags,
        $tagMap
    )

    $outTags = @{ }
    $inTags.keys | ForEach-Object { $outTags += @{($tagMap.save + $_) = $inTags[$_] } }

    return $outTags
}

Export-ModuleMember -Function Update-Resource
