# Bellhop Scaler Function
The Scaler Function is a very lightweight Powershell function that is triggered when a new message is posted to the defined storage queue. When this function is invoked it will parse the queue message item, import the necessary scaler module based on resource type, and finally invoke the scaling module using the parameters collected.


## Scaler Modules
Scaler modules are built separately to allow the main Scaler Function code to remain lightweight and rarely change. This makes the scaler modules most complicated part of this solution, but this intentional design affords the solution much more flexibility. Each module is developed to be specific to the service they are going to scale, so each Azure resource type will have its own scaler module. This is a result of the fact that each Azure service approaches scaling in different ways, with varying metrics to adjust. 


## Scaler Module Details
Instructions for creating new modules, as well as detailed information around the each current supported Azure Resource can be found in the [Scaler Modules](/scaler/modules/README.md) section of this documentation.
