# BELLHOP

<!-- 
Guidelines on README format: https://review.docs.microsoft.com/help/onboard/admin/samples/concepts/readme-template?branch=master

Guidance on onboarding samples to docs.microsoft.com/samples: https://review.docs.microsoft.com/help/onboard/admin/samples/process/onboarding?branch=master

Taxonomies for products and languages: https://review.docs.microsoft.com/new-hope/information-architecture/metadata/taxonomies?branch=master
-->

This project was born out of the customer need to save money, and a gap in Azure's ability to easily "turn down" Managed services in Dev/Test environments. Particularly when referring to scaling between service tiers. We put our heads together and came up with a serverless option to adress this issue built almost entirely around Azure Functions. Bellhop is comprised of 2 separate Azure Functions; one is the Engine (.NET), and the other is the Scaler (PowerShell). Users will need to tag their resources with the required tags (covered below), and the Engine Function will then use those tags to determine which resources need to be scaled, to which tiers, and when. The Engine will then post a scale message in the Storage Queue, at which time the Scaler-Trigger Function will pull the message from the queue and begin processing the scale request. The Scaler function leverages custom scaler modules per resource type to fufill the scale request.


## Repo Contents

| File/folder       | Description                                |
|-------------------|--------------------------------------------|
| `docs/`           | Docsify repo for web documentation.        |
| `functions/`      | Bellhop project Azure Functions. Includes Engine and Trigger.|
| `templates/`      | Bellhop Infrastructure ARM Template.       |
| `deployBellop.ps1` | PowerShell script to deploy tool.         |
| `removeBellhop.ps1` | PowerShell script to decommission the tool. Makes testing and experimentation easy.|
| `updateScaler.ps1` | PowerShell script to easily update the Scaler Function. |
| `README.md`       | This README file.                          |
| `LICENSE`         | MIT license for the project                |
| `CODE_OF_CONDUCT.md` | Expected code of conduct for this repo  |
| `.gitignore`      | Define what to ignore at commit time.      |


## Bellhop Architecture and Workflow

![Bellhop Architecture](./images/bellhop.png)


## Prerequisites

To successfully deploy this project, it requires the Azure user have the following:

- Azure AD Role allowing user to assign roles (Global Admin, App Admin, Cloud App Admin)
    - *Necessary to assign proper scope to managed identity*
- Azure RBAC role of Owner or Contributor at the Subscription scope
- Azure Subscription
- Powershell 7.0+
- Azure PowerShell 5.6+
- .NET 3.1 SDK


## Current Supported Azure Services

The list of scalers currently supported by Bellhop:
- App Service Plan
- SQL Database
- SQL Elastic Pool
- Virtual Machine

## Deploying/Updating/Deleting Bellhop

### Steps to deploy infrastructure:

- Clone the [GitHub repo](https://github.com/Azure/Bellhop) down to your local machine
- Run `deployBellhop.ps1` from project root

The deployment script will ask the user to input a unique name for their deployment, as well as their desired Azure region. These will be passed to the script as parameters. 

Example:
```
PS /User/github/azure-autoscale> ./deployBellhop.ps1
Enter a unique name for your deployment: bellhop
Enter Azure Region to deploy to: westus2
```

### Steps to update the Scaler Function when adding custom scaler modules:

- Run `updateScaler.ps1` from project root

The update script will ask user for a Resource Group name, and then zip deploy the updates to the Scaler function deployed in the given resource group.

Example:
```
PS /User/github/Azure/bellhop> ./updateScaler.ps1
Enter resource group name where function is deployed: bellhop-rg 
```

### Steps to tear down the deployment:
- Run `removeBellhop.ps1` from project root

The teardown script will ask user for a Resource Group name, and then delete that Resource Group and all associated resources. 

Example:
```
PS /User/github/Azure/bellhop> ./removeBellhop.ps1
Enter name of resource group to teardown: bellhop-rg
``` 

## Running Bellhop
Bellhop is currently configured to run in the context of a single subscription, and relies on the Graph API and certian Tags on resources to handle service tier scaling for you! The Engine will query Graph API every 5 min (by default) and perform a get on resources tagged with `resize-Enable = True`. If resize has been enabled, and times have been configured, the Engine will determine which direction the resource would like to scale and send a message to the storage queue. 

All you need to do to run Bellhop is deploy the solution and ensure you have the proper tags set, and Bellhop will take care of the rest! 


## Required Tags for all services
Bellhop operates based on resource tags. Some of the required tags will be common between Azure services, and some tags will be specific to the resource you would like Bellhop to scale. Resource specific tags will be discussed in detail in the [Scaler Modules](/scaler/modules/README.md) section.

Bellhop Common Tags:
```
resize-Enable = <bool> (True/False)
resize-StartTime = <DateTime> (Friday 7PM)
resize-EndTime = <DateTime> (Monday 7:30AM)
```

_**NOTE: StartTime and EndTime are currently in UTC**_

## Bellhop Infrastructure

### So, what does this solution actually deploy?

The included deploy script, `deployBellhop.ps1`, will build the following infrastructure:
- **Resource Group** 
    - You _can_ bring an existing resource group
    - Deployment will create a new resource group if one does not already exist
- **System Assigned Managed Identity**
    - Managed Identity for the App Service Plan will have Contributor rights to the Subscription
- **Azure Storage Account**
    - Storage for Azure Function App Files
    - Storage Queue for Function Trigger
- **Azure App Service Plan**
    - Windows App Service Plan to host Function Apps
        - Scaler Function App (PowerShell)
            - Scaler modules
        - Engine Function App (.NET)
- **Zip Deploy Function Package** 
    - Deploy Function Zip packages to the Function Apps
- **Azure Application Insights**
    - App Insights for App Service Plan


### Security considerations
For the purpose of this project we have not integrated security features into Bellhop as is being deployed through this workflow. This solution is a Proof Of Concept and is not secure, it is only recommended for testing. To use this service in a production deployment it is recommended to review the following documentation from Azure. It walks though best practices on securing Azure Functions: 
[Securing Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/security-concepts)

**_IT IS RECOMMENDED TO USE AVAILABLE SECURITY CONTROLS IN A PRODUCTION DEPLOYMENT_**

## FAQ

**Why would I use Bellhop?**

You realize that by "turning down" your resources in Dev/Test environments you could save a lot of money. You also realize there is not currently an an easy way to scale service tiers on Azure PaaS services. Bellhop to the rescue!!!! Tag your resources with scale times and desired "simmer" settings and Bellhop will take care of the rest!

**What does the roadmap for Bellhop look like?**

We would like this to become a SaaS/PaaS product that will help to keep our customers costs under control in Dev/Test Environments. 

**Who are the awesome people that built this solution??**

Matt, Nills, and Tyler are Cloud Solutions Architects at Microsoft, and are always looking for interesting ways to help our customers solve problems!

**Want to know more about Bellhop??**

Email us HERE

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
