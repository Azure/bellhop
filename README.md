---
page_type: sample
languages:
- powershell
products:
- azure
- azure-powershell
- azure-resource-manager-templates
- azure-app-service-plans
- azure-function-apps
- azure-app-insights
description: "Bellhop allows a customer to 'hop' between service tiers, like a traditional bellhop helps you move between floors."
---

[![Deploy To Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fbellhop%2Fdeploy-to-azure%2Ftemplates%2Finfra.json%2FcreateUIDefinitionUri%2Fhttps%3A%2F%2Fraw.githubusercontent.com%2FAzure%2Fbellhop%2Fdeploy-to-azure%2Ftemplates%2FcreateUiDefinition.json)

# Bellhop

<!-- 
Guidelines on README format: https://review.docs.microsoft.com/help/onboard/admin/samples/concepts/readme-template?branch=master

Guidance on onboarding samples to docs.microsoft.com/samples: https://review.docs.microsoft.com/help/onboard/admin/samples/process/onboarding?branch=master

Taxonomies for products and languages: https://review.docs.microsoft.com/new-hope/information-architecture/metadata/taxonomies?branch=master
-->

Bellhop is a highly extensible framework providing an easy way to scale Azure managed services between their available service tiers on a schedule. The solution is completely serverless and built leveraging Azure Functions, Storage Queues, and resource tags. The modular nature of Bellhop was thoughtfully designed to make it possible for anyone to extend this solution to cover their specific needs.

## Repo Contents

| File/folder       | Description                                |
|-------------------|--------------------------------------------|
| `docs/`           | Docsify repo for web documentation.        |
| `functions/`      | Bellhop project Azure Functions. Includes Engine and Trigger.|
| `templates/`      | Bellhop Infrastructure ARM Template.       |
| `deployBellhop.ps1` | PowerShell script to deploy tool.        |
| `removeBellhop.ps1` | PowerShell script to decommission the tool. Makes testing and experimentation easy.|
| `updateScaler.ps1` | PowerShell script to easily update the Scaler Function. |
| `README.md`       | This README file.                          |
| `LICENSE`         | MIT license for the project                |
| `CODE_OF_CONDUCT.md` | Expected code of conduct for this repo  |
| `.gitignore`      | Define what to ignore at commit time.      |

## Documentation
The Bellhop project leverages [Docsify](https://docsify.js.org/#/) and [GitHub Pages](https://docs.github.com/en/github/working-with-github-pages) to present the project documentation, which can be found here:

- **[Welcome to Bellhop!](https://azure.github.io/bellhop/#/)**

## FAQ

**Why would I use Bellhop?**

You realize that by "turning down" your resources in Dev/Test environments you could save a lot on the cost of your infrastructure. You also realize there is not currently an easy way to scale between service tiers on Azure services. Bellhop to the rescue!!!! Tag your resources with scale times and desired service tier settings and let Bellhop take care of the rest!

**What does the roadmap for Bellhop look like?**

We would like this to become a SaaS/PaaS product that will help to keep our customers costs under control in Dev/Test Environments. 

**Who are the awesome people that built this solution??**

Matt, Nills, and Tyler are Cloud Solutions Architects at Microsoft, and are always looking for interesting ways to help our customers solve problems!

**Want to know more about Bellhop??**

Please feel free to reach out to bellhop@microsoft.com with any questions or feedback! 

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
