# Bellhop Scaler Function
The Scaler Function is a very lightweight Powershell function that is triggered when a new message is posted to the defined storage queue. When this function is invoked it will parse the queue message item, import the necessary scaler module based on resource type, and finally invoke the scaling module using the parameters collected.

## Bellhop Scaler Function Workflow
- Message is recieved in the queue after being passed from the Engine
- Scaler parses the Resource Type, and loads the appropriate Scaler Module (Import function.psm1)
- Resize parameters are passed to the Scaler Module function:
    - Resize direction (Up/Down)
    - Azure Resource Graph results
    - Extracted Tag data (Save/Set state)
- Entire resize operation is executed by the Scaler Module

## Scaler Module Details
Instructions for creating new modules, as well as detailed information around the each current supported Azure Resource can be found in the [Scaler Modules](/scaler/modules/README.md) section of this documentation.
