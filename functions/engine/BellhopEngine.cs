using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.Azure.Management.ResourceGraph;
using Microsoft.Azure.Management.ResourceGraph.Models;
using Microsoft.Azure.Management.Subscription;
using Microsoft.Azure.Management.Subscription.Models;

using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Bellhop.Function
{
  	public static class Debug
    {
        private static bool _enabled = false;
        
        public static bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }
    }

    public class BellhopEngine
    {
        private readonly IConfiguration _configuration;
        private static IConfigurationRefresher _refresher;

        public BellhopEngine(IConfiguration configuration, IConfigurationRefresher refresher)
        {
            _configuration = configuration;
            _refresher = refresher;
        }

        [FunctionName("BellhopEngine")]
        public async void Run([TimerTrigger("%ENGINE_TIMER_EXPRESSION%")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("Bellhop engine starting up...");
            log.LogInformation("Current UTC Time: " + DateTime.UtcNow.ToString("dddd hh:mm:ss tt"));

            const string strQuery = "Resources | where tags['resize-Enable'] =~ 'True'";

            var resizeUpList = new List<JObject>();
            var resizeDownList = new List<JObject>();

            log.LogInformation("Fetching app configuration settings...");

            await _refresher.RefreshAsync();

            string debugKeyName = "debugMode";
            string debugAppSetting = _configuration[debugKeyName];
            log.LogInformation("Debug Flag: " + debugAppSetting);

            try {
			    Debug.Enabled = Boolean.Parse(debugAppSetting);
		    }
            catch {
			    Debug.Enabled = false;
		    }

            const string storageKeyName = "storageAccount";
            string storageAppSetting = _configuration[storageKeyName];
            if (Debug.Enabled) log.LogInformation("Storage Account: " + storageAppSetting);

            const string queueKeyName = "storageQueue";
            string queueAppSetting = _configuration[queueKeyName];
            if (Debug.Enabled) log.LogInformation("Storage Queue: " + queueAppSetting);

            bool debugFlag = bool.Parse(debugAppSetting);

            QueueClient messageQueue = getQueueClient(storageAppSetting, queueAppSetting);

            ManagedIdentityCredential managedIdentityCredential = new ManagedIdentityCredential();
            string[] scope = new string[] { "https://management.azure.com/.default" };
            var accessToken = (await managedIdentityCredential.GetTokenAsync(new Azure.Core.TokenRequestContext(scope))).Token;

            TokenCredentials serviceClientCreds = new TokenCredentials(accessToken);
            ResourceGraphClient rgClient = new ResourceGraphClient(serviceClientCreds);
            SubscriptionClient subscriptionClient = new SubscriptionClient(serviceClientCreds);

            IEnumerable<SubscriptionModel> subscriptions = await subscriptionClient.Subscriptions.ListAsync();
            var subscriptionIds = subscriptions
                .Where(s => s.State == SubscriptionState.Enabled)
                .Select(s => s.SubscriptionId)
                .ToList();

            if (Debug.Enabled) {
                string subscriptionJson = JsonConvert.SerializeObject(subscriptionIds);

                log.LogInformation("Target Subscriptions:");
                log.LogInformation(subscriptionJson);
            }

            QueryRequest request = new QueryRequest
            {
                Subscriptions = subscriptionIds,
                Query = strQuery,
                Options = new QueryRequestOptions(resultFormat: ResultFormat.ObjectArray)
            };

            log.LogInformation("Querying for enabled resources...");

            var response = await rgClient.ResourcesAsync(request);
            JArray resources = JArray.Parse(response.Data.ToString());

            log.LogInformation("Examining resources...");

            foreach (JObject resource in resources)
            {
                if (Debug.Enabled) log.LogInformation("=========================");
                if (Debug.Enabled) log.LogInformation("Resource: " + resource["name"].ToString());

                Hashtable times = new Hashtable() {
                    {"StartTime", resource["tags"]["resize-StartTime"].ToString()},
                    {"EndTime", resource["tags"]["resize-EndTime"].ToString()}
                };

                if (Debug.Enabled){
                    log.LogInformation("Scale Down: " + times["StartTime"]);
                    log.LogInformation("Scale Up: " + times["EndTime"]);
                }

                Regex rg = new Regex("saveState-.*");

                if (resizeTime(times, log))
                {
                    string scaleMessage = "Currently within 'scale down' period ";

                    if (resource["tags"].Children<JProperty>().Any(prop => rg.IsMatch(prop.Name.ToString())))
                    {
                        scaleMessage += "(Already Scaled)";
                    }
                    else
                    {
                        scaleMessage += "(Scale Scheduled)";
                        resizeDownList.Add(resource);
                    }

                    if (Debug.Enabled) log.LogInformation(scaleMessage);
                }
                else
                {
                    string scaleMessage = "=> Currently within 'scale up' period ";

                    if (resource["tags"].Children<JProperty>().Any(prop => rg.IsMatch(prop.Name.ToString())))
                    {
                        scaleMessage += "(Scale Scheduled)";
                        resizeUpList.Add(resource);
                    }
                    else
                    {
                        scaleMessage += "(Already Scaled)";
                    }

                    if (Debug.Enabled) log.LogInformation(scaleMessage);
                }
            }

            if (Debug.Enabled) log.LogInformation("=========================");
            log.LogInformation("Processing scale up queue...");

            foreach (var item in resizeUpList)
            {
                if (Debug.Enabled) log.LogInformation("=========================");
                if (Debug.Enabled) log.LogInformation(item["name"].ToString() + " => up");
                
                var messageData = new JObject();
                messageData.Add(new JProperty("debug", debugFlag));
                messageData.Add(new JProperty("direction", "up"));
                messageData.Add(new JProperty("graphResults", item));

                if (Debug.Enabled) log.LogInformation("Queue Message:");
                if (Debug.Enabled) log.LogInformation(messageData.ToString(Formatting.None));
                if (Debug.Enabled) log.LogInformation("=========================");

                writeQueueMessage(messageData, messageQueue);
            };

            log.LogInformation("Processing scale down queue...");

            foreach (var item in resizeDownList)
            {
                if (Debug.Enabled) log.LogInformation("=========================");
                if (Debug.Enabled) log.LogInformation(item["name"].ToString() + " => down");

                var messageData = new JObject();
                messageData.Add(new JProperty("debug", debugFlag));
                messageData.Add(new JProperty("direction", "down"));
                messageData.Add(new JProperty("graphResults", item));

                if (Debug.Enabled) log.LogInformation("Queue Message:");
                if (Debug.Enabled) log.LogInformation(messageData.ToString(Formatting.None));
                if (Debug.Enabled) log.LogInformation("=========================");

                writeQueueMessage(messageData, messageQueue);
            };

            log.LogInformation("Bellhop engine execution complete!");
        }

        public static QueueClient getQueueClient(string storName, string queueName)
        {
            ManagedIdentityCredential managedIdentityCredential = new ManagedIdentityCredential();

            QueueClientOptions queueOptions = new QueueClientOptions
            {
                Retry = {
                  MaxRetries = 5,
                  Mode = 0
                },
                MessageEncoding = QueueMessageEncoding.Base64
            };

            string queueEndpoint = string.Format("https://{0}.queue.core.windows.net/{1}", storName, queueName);
            QueueClient queueClient = new QueueClient(new Uri(queueEndpoint), managedIdentityCredential, queueOptions);

            return queueClient;
        }

        public static void writeQueueMessage(JObject target, QueueClient queue)
        {
            var jTarget = target.ToString(Formatting.None);

            queue.SendMessageAsync(jTarget);
        }

        
        public static (System.DayOfWeek, TimeSpan) getActionTime(string stamp)
        {
            string[] parsedStamp = stamp.Split(" ");
            const string dailyStr = "daily";

            //todo: add support for "Daily" keyword
            System.DayOfWeek day;
            
            //if stamp contains "Daily" then resolve to today
            // this assumes that a one-part stamp sets the time and not the day
            if (parsedStamp[0].ToLower().Equals(dailyStr))
                day = DateTime.UtcNow.DayOfWeek;
            else
                day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), parsedStamp[0], true);

            TimeSpan time = Convert.ToDateTime(parsedStamp[1]).TimeOfDay;
            
            return (day, time);
        }

        public static bool resizeTime(Hashtable times, ILogger log)
        {
            //wrap in try/catch to avoid failing across the board if one tag cannot be correctly parsed
            try{
                DateTime now = DateTime.UtcNow;
                var currentDay = now.DayOfWeek;

                (var fromDay, var fromTime) = getActionTime((string)times["StartTime"]);
                (var toDay, var toTime) = getActionTime((string)times["EndTime"]);

                if (toDay < fromDay)
                {
                    toDay += 7;

                    if (currentDay < fromDay)
                    {
                        currentDay += 7;
                    }
                }

                var fromUpdate = (fromDay - currentDay);
                var toUpdate = (toDay - currentDay);

                var fromDate = DateTime.Parse(fromTime.ToString()).AddDays(fromUpdate);
                var toDate = DateTime.Parse(toTime.ToString()).AddDays(toUpdate);

                if (now > toDate)
                {
                    toDate = toDate.AddDays(7);
                    fromDate = fromDate.AddDays(7);
                }

                if ((now > fromDate) && (now < toDate))
                {
                    return true;
                }

            }catch (Exception ex){
                log.LogError(0, ex, $"Error calculating resize time -- StartTime: {(string)times["StartTime"]}  EndTime: {(string)times["EndTime"]}");
            }
            
            return false;
        }
    }
}
