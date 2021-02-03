# Bellhop Scaler-Trigger Function
The main Scaler Function is the Trigger function, it is a very lightweight Powershell function that is triggered by a storage queue message. When invoked, the main function will then parse the queue message, import the service specific scaler module, and invoke the scaling module using the collected parameters. 

Scalers modules are the most complicated part of this design, but this also affords the solution to be as flexible as possible. They are developed to be specific to the service they are going to scale, meaning that each service will need to have its own scaler function Powershell module. This is due to the fact that each Azure managed service approaches scaling in different ways. 

Each Scaler Module has a dedicated information page in the [Scaler Modules](./modules/README.md) section

