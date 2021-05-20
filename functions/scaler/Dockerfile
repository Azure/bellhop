FROM mcr.microsoft.com/azure-functions/powershell:3.0-powershell7-core-tools as build

RUN mkdir /Modules && \
    pwsh -Command Save-Module Az.Accounts -Path /Modules && \
    pwsh -Command Save-Module Az.Compute -Path /Modules && \
    pwsh -Command Save-Module Az.Websites -Path /Modules && \
    pwsh -Command Save-Module Az.Sql -Path /Modules && \
    pwsh -Command Save-Module Az.Storage -Path /Modules

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/powershell:3.0-appservice
FROM mcr.microsoft.com/azure-functions/powershell:3.0-powershell7-slim
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY . /home/site/wwwroot
COPY --from=build /Modules /home/site/wwwroot/Modules