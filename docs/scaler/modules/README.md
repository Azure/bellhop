# Bellhop Scaler Modules

Scaler modules are built separately to allow the main Scaler Function code to remain lightweight and rarely change. This makes the scaler modules most complicated part of this solution, but this intentional design affords the solution much more flexibility. Each module is developed to be specific to the service they are going to scale, so each Azure resource type will have its own scaler module.

Each Azure resource that you wish to scale using Bellhop, will require its own Powershell Module in the form of a `.psm1`. These modules will be named `function.psm1` and are created under the `./functions/scaler/BellhopScaler/scalers` directory. The sub folders in that directory **must** follow the Microsoft.TYPE format in order for the Scaler Function to import the correct module. 

_You can reference these resource types when creating new scalers: [Azure Resource Graph Type Reference](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)_

**For example**
When creating the `App Service Plan` scaler module, the folder structure looks like this:
```
./functions/scaler/BellhopScaler/scalers/microsoft.web/serverfarms/function.psm1
```


## Creating a New Scaler Module
How can I extend this myself for a new Azure Resource Type?

This solution was designed to be extensible from the beginning, with the idea being that to scale a new resource you only need to write a new module. There are a few steps to this process:

1) Create new folder in the `/functions/scaler/BellhopScaler/scalers` folder that follows the pattern based on [Azure Resource Type](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)
    - **The format of these folders is important because the main Scaler-Trigger function uses the resource type returned from the Graph API query to determine the path to the correct Powershell Module to import**
    - **SEE EXAMPLE ABOVE**

2) Create new .psm1 PowerShell Module
    - Named: `function.psm1`
    - This module will contain all of the logic to scale the new resource type, including the Azure PowerShell call to resize the rarget resource.
    - Developed to accept the message format sent to the `autoscale` storage queue.
        - Scale Direction + Azure Resource Graph Query.

***template.psm1 example:***
_Sample-scaler module code can be found in the [development](./development/sample-scaler/) folder in the GitHub repo._

3) Run `updateScaler.ps1` script from project root to Zip-Deploy new Scaler code.

Example:
```
PS /User/github/Azure/bellhop> ./updateScaler.ps1
Enter resource group name where function is deployed: bellhop-rg 
```

4) Create new `servicename.md` page to document how to use the new scaler.
    - Create this file in the `./docs/scaler/modules/` folder
    - Update the `./docs/_sidebar.md` with path to new scaler document
    - New page will be displayed on the documentation site
