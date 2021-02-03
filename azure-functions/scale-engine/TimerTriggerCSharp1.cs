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

namespace Company.Function
{
    public class TimerTriggerCSharp1
    {
        private readonly IConfiguration _configuration;

        public TimerTriggerCSharp1(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName("TimerTriggerCSharp1")]
        public async void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string strQuery = "Resources | where tags['resize-Enable'] =~ 'True'";

            var resizeUpList = new List<JObject>();
            var resizeDownList = new List<JObject>();

            string storageKeyName = "storageAccount";
            string storageAppSetting = _configuration[storageKeyName];
            log.LogInformation("Storage Account: " + storageAppSetting);

            string queueKeyName = "storageQueue";
            string queueAppSetting = _configuration[queueKeyName];
            log.LogInformation("Storage Queue: " + queueAppSetting);

            string debugKeyName = "debugMode";
            string debugAppSetting = _configuration[debugKeyName];
            log.LogInformation("Debug: " + debugAppSetting);

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

            QueryRequest request = new QueryRequest
            {
                Subscriptions = subscriptionIds,
                Query = strQuery,
                Options = new QueryRequestOptions(resultFormat: ResultFormat.ObjectArray)
            };

            var response = await rgClient.ResourcesAsync(request);
            JArray resources = JArray.Parse(response.Data.ToString());

            log.LogInformation("Current Time: " + DateTime.UtcNow.ToString("dddd htt"));

            foreach (JObject resource in resources)
            {
                log.LogInformation("Target: " + resource["name"].ToString());

                Hashtable times = new Hashtable() {
                    {"StartTime", resource["tags"]["resize-StartTime"].ToString()},
                    {"EndTime", resource["tags"]["resize-EndTime"].ToString()}
                };

                foreach (string key in times.Keys)
                {
                    log.LogInformation(string.Format("{0}: {1}", key, times[key]));
                }

                Regex rg = new Regex("saveState-.*");

                if (resizeTime(times))
                {
                    // log.LogInformation("Resize Time: YES");
                    if (resource["tags"].Children<JProperty>().Any(prop => rg.IsMatch(prop.Name.ToString())))
                    {
                        log.LogInformation(resource["name"].ToString() + " Already Scaled Down...");
                    }
                    else
                    {
                        log.LogInformation(resource["name"].ToString() + " Needs to be Scaled Down...");
                        resizeDownList.Add(resource);
                    }
                }
                else
                {
                    // log.LogInformation("Resize Time: NO");
                    if (resource["tags"].Children<JProperty>().Any(prop => rg.IsMatch(prop.Name.ToString())))
                    {
                        log.LogInformation(resource["name"].ToString() + " Needs to be Scaled Up...");
                        resizeUpList.Add(resource);
                    }
                    else
                    {
                        log.LogInformation(resource["name"].ToString() + " Already Scaled Up...");
                    }
                }
            }

            foreach (var item in resizeUpList)
            {
                log.LogInformation(item["name"].ToString() + " => up");
                
                var messageData = new JObject();
                messageData.Add(new JProperty("debug", debugFlag));
                messageData.Add(new JProperty("direction", "up"));
                messageData.Add(new JProperty("graphResults", item));

                writeQueueMessage(messageData, messageQueue);
            };

            foreach (var item in resizeDownList)
            {
                log.LogInformation(item["name"].ToString() + " => down");

                var messageData = new JObject();
                messageData.Add(new JProperty("debug", debugFlag));
                messageData.Add(new JProperty("direction", "down"));
                messageData.Add(new JProperty("graphResults", item));

                writeQueueMessage(messageData, messageQueue);
            };
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

            return ((DayOfWeek)Enum.Parse(typeof(DayOfWeek), parsedStamp[0], true), Convert.ToDateTime(parsedStamp[1]).TimeOfDay);
        }

        public static bool resizeTime(Hashtable times)
        {
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

            return false;
        }
    }
}
