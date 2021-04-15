# Bellhop Scaler Function
The Scaler Function is a very lightweight Powershell function that is triggered when a new message is posted to the defined storage queue. When this function is invoked it will parse the queue message item, import the necessary scaler module based on resource type, and finally invoke the scaling module using the parameters collected.

## Bellhop Scaler Function Workflow
- Message is recieved in the storage queue after being passed from the Engine
- Scaler parses the message for the value of `graphResults.type`, and then imports the appropriate Scaler module (function.psm1)
- Scaler function invokes the appropriate Scaler module to begin a scale operation based on the queue message, which includes the following parameters:
    - **Debug Flag**
        - If value is set to `true` logging will be much more verbose for troubleshooting purposes
        - _Can be changed after deployment within App Configuration_
    - **Resize Direction**
        - Valid options are _'up'_ or _'down'_
    - **Tag Map Data**
        - This is the map data to inform Bellhop which tags to use for scale operations
        - _All Tag Keys are customizable now from the "Advanced" tab when using the "Deploy to Azure" button_
    - **Azure Resource Graph Results**
        - Results of the Azure Resource Graph API query for the target resource
- Entire resize operation is executed by the Scaler module

## Scaler Module Details
Instructions for creating new modules, as well as detailed information around the each currently supported Azure Resource can be found in the [Scaler Modules](/scaler/modules/README.md) section of the documentation.