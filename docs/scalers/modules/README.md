# Bellhop Scaler Modules
Each Azure Service that you wish to scale using Bellhop, will require it's own Powershell Module in the form of a `psm1` file. These modules are kept in the `./azure-functions/scale-trigger/scalers` directory. The sub folders in that directory need to follow the Microsoft.TYPE format in order for the Scaler Function to import the correct Module. 

Resource Types: [Azure Resource Graph Type Reference](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)


## Creating a new Scaler Module
How can I extend this myself????

You need to:
1) Create new folder in the `/azure-functions/scale-trigger/scalers` folder that follows the pattern based on [Azure Resource Type](https://docs.microsoft.com/en-us/azure/governance/resource-graph/reference/supported-tables-resources)
    - **the format of these folders is important because the main Scaler-Trigger function uses the resource type returned from the Graph API query to determine the path to the correct Powershell Module to import**
2) Create new psm1 powershell module
    - Named: `function.psm1`
    - This module will contain all of the logic to scale the new resource type, as well as be where the API call to the service will be made.
    - Developed to accept the message format sent to the `autoscale` storage queue.
        - Scale Direction + Resource Graph Query. 
3) Create new `servicename.md` page to document how to use the new scaler.
    - Created in the `./docs/scalers/modules/` folder
    - Also need to update the `_sidebar.md` with path to new document