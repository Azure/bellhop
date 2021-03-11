# Bellhop Engine Function
The Bellhop Engine Function is the brains of the operation, and is responsible for orchestrating all of the operations Bellhop performs. 

The engine is written in C# (.NET Core 3.1). By default the Engine will trigger and run on a schedule repeating every 5 minutes. When triggered the Engine will query Azure Resource Graph API for any Azure resources, within the subscription, that have the `"resize-Enable": "True"` tag set. 

If the Engine finds resources that have the tag set to `True`, it then evaluates the remainder of the resources tags to determine:
- Resource Type
- Current Scale State (Up/Down)
- If a scaling operation should occur
    - _determined via `"resize-StartTime"` and `resize-EndTime"` tags_ 
- Sends the proper scale message to the storage queue


## Engine Dependencies
**TODO: Document specific Engine Function dependencies**


## Maintaining the Engine
**TODO: Document what will need to be done with the Engine when adding new scalers**
