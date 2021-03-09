# Bellhop for Azure Virtual Machines
This README will describe how to use Bellhop to help scale your Azure Virtual Machine Resources.

## Scaling Considerations
**MORE INFORMATION NEEDED AROUND SCALING THIS SERVICE SPECIFICALLY, THIS IS JUST A PLACE HOLDER**

## Required Tags for Azure Virtual Machines
```
setState-VMSize = <String> (VirtualMachineSizeType)
```

For more information on tag values for Azure Virtual Machines please see the Microsoft documentaion: [Update-AzVM](https://docs.microsoft.com/en-us/powershell/module/az.compute/update-azvm?view=azps-5.6.0).

For a list of VM size definitions, please reference the **VirtualMachineSizeTypes** section of the [VM Update API](https://docs.microsoft.com/en-us/rest/api/compute/virtualmachines/update#definitions) documentation.

## Sample scale message
An example of the message sent to the queue by the engine function 

- virtualmachine.json
```
{
    "direction": "down",
    "graphResults": {
        "id": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Compute/virtualMachines/vm-test",
        "name": "vm-test",
        "type": "microsoft.compute/virtualmachines",
        "tenantId": "<TENANT-ID>",
        "kind": "",
        "location": "westus2",
        "resourceGroup": "<RESOURCE-GROUP-NAME>",
        "subscriptionId": "<SUBSCRIPTION-ID>",
        "managedBy": "",
        "sku": null,
        "plan": null,
        "properties": {
            "provisioningState": "Succeeded",
            "hardwareProfile": {
                "vmSize": "Standard_B2ms"
            },
            "storageProfile": {
                "imageReference": {
                    "exactVersion": "18.04.202101290",
                    "publisher": "Canonical",
                    "version": "latest",
                    "offer": "UbuntuServer",
                    "sku": "18.04-LTS"
                },
                "dataDisks": [],
                "osDisk": {
                    "name": "vm-test_OsDisk_1_d32417bff8934e1b819813ca59e8e3bf",
                    "createOption": "FromImage",
                    "managedDisk": {
                        "id": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Compute/disks/vm-test_OsDisk_1_d32417bff8934e1b819813ca59e8e3bf"
                    },
                    "caching": "ReadWrite",
                    "osType": "Linux"
                }
            },
            "networkProfile": {
                "networkInterfaces": [
                    {
                        "id": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Network/networkInterfaces/vm-test629"
                    }
                ]
            },
            "extended": {
                "instanceView": {
                    "powerState": {
                        "displayStatus": "VM deallocated",
                        "level": "Info",
                        "code": "PowerState/deallocated"
                    }
                }
            },
            "osProfile": {
                "computerName": "vm-test",
                "linuxConfiguration": {
                    "disablePasswordAuthentication": false,
                    "patchSettings": {
                        "patchMode": "ImageDefault"
                    },
                    "provisionVMAgent": true
                },
                "adminUsername": "AzureAdmin",
                "requireGuestProvisionSignal": true,
                "allowExtensionOperations": true,
                "secrets": []
            },
            "vmId": "cb742c65-ce84-4a1f-86e7-5a9ddfdc7a5a"
        },
        "tags": {
            "resize-StartTime": "Friday 7PM",
            "resize-EndTime": "Monday 6AM",
            "resize-Enable": "False",
            "setState-VMSize": "Standard_B1ms"
        },
        "identity": null,
        "zones": null,
        "extendedLocation": null,
        "ResourceId": "/subscriptions/<SUBSCRIPTION-ID>/resourceGroups/<RESOURCE-GROUP-NAME>/providers/Microsoft.Compute/virtualMachines/vm-test"
    }
}
```
