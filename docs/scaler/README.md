# Bellhop Scaler Function
The Scaler Function is a very lightweight Powershell function that is triggered when a new message is posted to the defined storage queue. When this function is invoked it will parse the queue message item, import the necessary scaler module based on resource type, and finally invoke the scaling module using the parameters collected.

## Bellhop Scaler Function Workflow
- Message is recieved in the queue after being passed from the Engine
- Scaler parses the message for the value of `graphResults.type`, and then imports the appropriate Scaler Module (function.psm1)
- Scaler Function triggers the appropriate Scaler Module to begin a scale operation via an event message including the following data:
    - **Debug Flag**
        - If value is set to `true` logging will be much more verbose for troubleshooting purposes
        - _Can be changed after deployment via the App Configuration_
    - **Resize direction**
        - Valid options are _UP_ or _DOWN_
    - **Tag Map Data**
        - This is the map data to inform Bellhop which tags to use for scale operations
        - _All Tag Keys are customizable now, per the "Advanced" tab when using the Deploy-to-Azure button_
    - **Azure Resource Graph results**
        - Results of the Azure Graph API query for the target resource
- Entire resize operation is executed by the Scaler Module

## Scaler Module Details
Instructions for creating new modules, as well as detailed information around the each current supported Azure Resource can be found in the [Scaler Modules](/scaler/modules/README.md) section of this documentation.