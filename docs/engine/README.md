# Bellhop Engine Function
The Bellhop Engine function is written in C#. It is configured by default to run every 5 minutes and will query Azure Graph API for any resources with the `"resize-Enable": "true"` tag. If it finds resources it evaluates the rest of the tags and then will format and send the scale message to the storage queue. 

## More Engine Resources

## Im sure there is something interesting to say

## Anything else to add?