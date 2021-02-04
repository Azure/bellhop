# Bellhop Engine Function
The Bellhop Engine Function is the brains of the operation, and is responsible for orchestrating all of the operations Bellhop performs. 

The engine is written in C#, and requires .NET Core 3.1 be installed in order to deploy this solution. By default the Engine will trigger and run on a schedule repeating every 5 minutes. When triggered the Engine will query Azure Graph API for any Azure resources, within the subscription, that have the `"resize-Enable": "true"` tag set. 

If the Engine finds resources that have the tag set to `true`, it then evaluates the remainder of the resources tags to determine:
- Resource Type
- Current State (UP/DOWN)
- If a scaling operation should occur
    - _determined via `"resize-StartTime"` and `resize-EndTime"` tags_ 
- Format and send the proper scale message to the storage queue


## Engine Dependencies
**TODO: Document specific Engine Function dependencies**


## Maintaining the Engine
**TODO: Document what will need to be done with the Engine when adding new scalers**