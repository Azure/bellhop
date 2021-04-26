# Monitoring & Alerting

## What's Included Out-of-the-Box
As part of the Bellhop solution, basic monitoring of both the Engine and Scaler functions are included with the deployment by default. This basic monitoring consists of sending all configured logs and outputs from both of the functions to the included Application Insights instance. Using the Kusto Query Language (KQL), users can craft meaningful queries to gather deep insight into Bellhop performance and begin alerting on undesireable behavior.

**Additional information:**
- [Azure Functions Monitoring with Application Insights](https://docs.microsoft.com/en-us/azure/azure-functions/functions-monitoring)
- [Getting Started with Kusto (KQL)](https://docs.microsoft.com/en-us/azure/data-explorer/kusto/concepts/)

## Alert Configuration and Details
In addition to the included monitoring discussed above, Bellhop offers users the ability to automatically recieve email notifications when either the Engine or Scaler functions fail. If the user chooses to enable Email Alerting the following additional resources will be deployed with Bellhop:

### Action Groups

An [Azure Monitor Action Group](https://docs.microsoft.com/en-us/azure/azure-monitor/alerts/action-groups) is a collection of notification preferences defined by the owner of an Azure subscription. Azure Monitor and Service Health alerts use action groups to notify users that an alert has been triggered. Various alerts may use the same action group or different action groups depending on the user's requirements.

Bellhop includes (1) default action group that is configured to use the `Email Notification` option. During deployment recipient names and email addresses will be added to the group via the options on the `Advanced` tab in the portal. 
- _These are the users to receive email notification if Bellhop experiences any errors during Engine or Scaler invokations_
- _Deployer has option to add multiple users, or group DL's_

### Alert Rules

[Azure Monitor Alert Rules](https://docs.microsoft.com/en-us/azure/azure-monitor/alerts/alerts-overview#manage-alert-rules) are separated from alerts and the actions taken when an alert fires in Azure. The alert rule captures the target resource and the criteria for alerting. The alert rule can be in an enabled or a disabled state and alerts only fire when enabled.  

Bellhop includes (2) alert rules by default, one for errors thrown by the Engine Function and the other for errors thrown by the Scalers. These built-in alert rules leverage a common format and will output all of the following information and details in the event of an error:
- OperationID: _Unique GUID for Function Operation_
- SubscriptionID: _Subscription ID of the resource where the error occured_
- SubscriptionName: _Subscription friendly name of the resource where the error occured_
- ResourceGroup: _Resource Group of the failed Target Resource to scale_
- ResourceName: _Name of resource that failed to scale_
- ErrorType: _Type of error thrown_
- ErrorMessage: _Readable error message_
- StackTrace: _Error StackTrace information_

#### Engine Error Alert Rule
The Engine Error alert rule is configured to alert the action group anytime the Engine Function throws an error. The rule is configured to check for errors every 30 min, at which time an alert will be sent including any errors returned via the KQL query
- _Engine Failure Rule KQL Query:_
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

#### Scaler Error Alert Rule
The Scaler Error alert rule is configured to alert the action group anytime the Scaler Function throws an error. The rule is configured to check for errors every 30 min, at which time an alert will be sent including any errors returned via the KQL query
- _Scaler Error Rule KQL Query:_
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
