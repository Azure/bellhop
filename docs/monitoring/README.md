# Monitoring and Alerting
## What's included by default?
As part of the Bellhop solution, basic monitoring of both the Engine and Scaler functions are included with the deployment by default. This basic monitoring consists of sending all configured logs and outputs from both of the functions to the included Application Insights instance. Using the Kusto Query Language (KQL), users can craft meaningful queries to gather deep insight into Bellhop performance and begin alerting on undesireable behavior.

_additional information_

- [Azure Functions Monitoring with Application Insights](https://docs.microsoft.com/en-us/azure/azure-functions/functions-monitoring)
- [Getting Started with Kusto (KQL)](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/concepts/)

## Optional Email Alerting
In addition to the included monitoring discussed above, Bellhop offers users the ability to automatically recieve email notifications when either the Engine or Scaler functions fail. If the user chooses to enable Email Alerting the following additional resources will be deployed with Bellhop:
- **Action Group**
    - Email based notifications
    - Names and Email Address will be configured via the options on the `Advanced` tab during deployment
        - _Option to add multiple users, or group DL's_
    - [Azure Monitor Action Groups](https://docs.microsoft.com/en-us/azure/azure-monitor/alerts/action-groups)
- **2 default Alert Rules**
     -  Alert Rules both include all of the following information in each message:
        - OperationID: _Unique GUID for Scale Operation_
        - SubscriptionID: _Subscription ID where the error occured_
        - SubscriptionName: _Friendly name of the subscription where the error occured_
        - ResourceGroup: _Resource Group of the failed Target Resource to scale_
        - ResourceName: _Name of resource that failed to scale_
        - ErrorType: _Type of error thrown_
        - ErrorMessage: _Readable error message_
        - StackTrace: _Included StackTrace information_
    - [Azure Monitor Alert Rules](https://docs.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-overview#manage-alert-rules)

    - `Scaler Errors`
        - Alerts if the Scaler function throws any errors
        - **Scaler Error Rule KQL Query**
        ```
        union traces
        | where timestamp > ago(30m)
        | where cloud_RoleName =~ 'bellhop-function-scaler' and  operation_Name =~ 'BellhopScaler'
        | order by timestamp desc
        | parse message with * "ERRORDATA: " errorObject
        | extend errObj = todynamic(errorObject).Exception
        | where isnotnull(errObj)
        | project OperationID = operation_Id, SubscriptionID = errObj.SubscriptionId, SubscriptionName = errObj.SubscriptionName, ResourceGroup = errObj.ResourceGroup, ResourceName = errObj.Name, ErrorType = errObj.Error.Type, ErrorMessage = errObj.Error.Message, StackTrace = errObj.Error.StackTrace
        ```

    - `Engine Errors`
        - Alerts if the Engine function throws any errors
        - **Engine Failure Rule KQL Query**
        ```
        union traces
        | where timestamp > ago(30m)
        | where cloud_RoleName =~ 'bellhop-function-engine' and operation_Name =~ 'BellhopEngine'
        | order by timestamp desc
        | parse message with * "ERRORDATA: " errorData
        | where isnotempty(errorData)
        | extend errorObj = todynamic(errorData)
        | mv-expand error = errorObj.Errors
        | project OperationID = operation_Id, SubscriptionID = errorObj.SubscriptionID, SubscriptionName = errorObj.SubscriptionName, ResourceGroup = errorObj.ResourceGroup, ResourceName = errorObj.Name, ErrorType = error.Type, ErrorMessage = error.Message, StackTrace = error.StackTrace
        ```

