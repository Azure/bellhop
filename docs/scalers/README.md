# Bellhop Scaler-Trigger Function
The Scaler-Trigger is a very lightweight Powershell function that is triggered when a new message is posted to the defined storage queue. When this function is invoked it will parse the queue message item, import the necessary scaler module based on resource type, and finally invoke the scaling module using the parameters collected.


## Scaler Modules?
Scaler modules are used to allow the main Scaler-Trigger function code to not need to change, or be updated moving forward. This decision makes the scaler modules most complicated part of this solution, but this intentional design affords the solution much more flexibility. Each module is developed to be specific to the service they are going to scale, so each Azure service will have its own module. This is due to the fact that each Azure managed service approaches scaling in different ways, with varying metrics to adjust. 


## Scaler Module Details
Instructions for creating new modules, as well as detailed information around the each current supported Azure Resource can be found in the [Scaler Modules](./modules/README.md) section of this documentation.

