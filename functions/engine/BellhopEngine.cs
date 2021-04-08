using System;
using System.Linq;
using System.Threading.Tasks;
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
    public class Settings
    {
        private static bool _debug = false;
        private static bool _isValid = false;
        private static List<string> _errors = new List<string>();

        private static Dictionary<string, string> _configData = new Dictionary<string, string>()
        {
            { "storageAccount", null },
            { "storageQueue", null },
            { "tagPrefix", null },
            { "enableTag", null },
            { "startTimeTag", null },
            { "endTimeTag", null },
            { "setStatePrefix", null },
            { "saveStatePrefix", null }
        };

        private static Dictionary<string, string> _tagMap = new Dictionary<string, string>();

        public static bool Debug
        {
            get => _debug;
            set => _debug = value;
        }

        public static Dictionary<string, string> ConfigData
        {
            get => _configData;
        }

        public static Dictionary<string, string> TagMap
        {
            get
            {
                if (_tagMap.Count == 0)
                {
                    GenerateTagMap();
                }

                return _tagMap;
            }
        }

        public static bool IsValid
        {
            get => _isValid;
        }

        public static List<string> Errors
        {
            get => _errors;
        }

        public Settings()
        {
            ValidateConfig();
        }

        public static void SetConfig(string key, string value)
        {
            if (_configData.ContainsKey(key))
            {
                _configData[key] = value;
                ValidateConfig();
            }
        }

        public static string GetConfig(string key)
        {
            string result = null;

            if (_configData.ContainsKey(key))
            {
                result = _configData[key];
            }

            return result;
        }

        public static string GetTag(string key)
        {
            string result = null;

            if (_tagMap.Count == 0)
            {
                GenerateTagMap();
            }

            if (_tagMap.ContainsKey(key))
            {
                result = _tagMap[key];
            }

            return result;
        }

        private static void GenerateTagMap()
        {
            _tagMap.Clear();
            _tagMap.Add("enable", (_configData["tagPrefix"] + _configData["enableTag"]));
            _tagMap.Add("start", (_configData["tagPrefix"] + _configData["startTimeTag"]));
            _tagMap.Add("end", (_configData["tagPrefix"] + _configData["endTimeTag"]));
            _tagMap.Add("set", (_configData["tagPrefix"] + _configData["setStatePrefix"]));
            _tagMap.Add("save", (_configData["tagPrefix"] + _configData["saveStatePrefix"]));
        }

        public static void ValidateConfig()
        {
            _errors.Clear();

            var matches = _configData.Where(pair => String.IsNullOrEmpty(pair.Value))
                    .Select(pair => pair.Key);

            foreach (var match in matches)
            {
                _errors.Add(match);
            }

            _isValid = matches.Count() != 0 ? false : true;
        }
    }

    class ResizeObject
    {
        private JObject graphData;
        private string resizeDirection;
        private Hashtable errors = new Hashtable();

        public ResizeObject(JObject data)
        {
            graphData = data;

            if (Settings.Debug) Console.WriteLine("=========================");
            if (Settings.Debug) Console.WriteLine("Resource: " + graphData["name"].ToString());
            // if (Settings.Debug) log.LogInformation("=========================");
            // if (Settings.Debug) log.LogInformation("Resource: " + graphData["name"].ToString());

            resizeDirection = getResizeDirection();

            if (Settings.Debug) Console.WriteLine("=========================");
            // if (Settings.Debug) log.LogInformation("=========================");
        }

        public string Name()
        {
            return graphData["name"].ToString();
        }

        public bool IsValid()
        {
            if(errors.Count > 0)
            {
                return false;
            }
            
            return true;
        }

        public Hashtable Errors()
        {
            return errors;	
        }

        public string CurrentState()
        {
            if(scaledDown())
            {
                return "down";
            }
            
            return "up";
        }

        public string TargetState()
        {
            if(timeToScale())
            {
                return "down";
            }
            
            return "up";
        }

        public string ResizeDirection()
        {
            return resizeDirection;
        }

        private string getResizeDirection()
        {
            bool inScaleWindow = false;
            string resizeDir;
            string scaleMessage;

            try
            {
                inScaleWindow = timeToScale();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().ToString());
                Console.WriteLine(ex.Message);
                // log.LogError(0, ex, ex.GetType().ToString());
                // log.LogError(0, ex, ex.Message);
            }

            if(inScaleWindow)
            {
                scaleMessage = "Currently within 'scale down' period ";

                if (scaledDown())
                {
                    scaleMessage += "(Already Scaled)";
                    resizeDir = null;
                }
                else
                {
                    scaleMessage += "(Scale Scheduled)";
                    resizeDir = "down";
                }
            }
            else
            {
                scaleMessage = "Currently within 'scale up' period ";

                if (scaledDown())
                {
                    scaleMessage += "(Scale Scheduled)";
                    resizeDir = "up";
                }
                else
                {
                    scaleMessage += "(Already Scaled)";
                    resizeDir = null;
                }
            }

            if (Settings.Debug) Console.WriteLine(scaleMessage);
            //if (Settings.Debug) log.LogInformation(scaleMessage);

            return resizeDir;
        }

        private bool scaledDown()
        {
            Regex rg = new Regex($"{Settings.GetTag("save")}.*");

            if (graphData["tags"].Children<JProperty>().Any(prop => rg.IsMatch(prop.Name.ToString())))
            {
                return true;
            }

            return false;
        }

        private bool timeToScale()
        {
            DateTime now = DateTime.UtcNow;
            var currentDay = now.DayOfWeek;
            const string dailyStr = "daily";

            Dictionary<string, string> times = new Dictionary<string, string>()
            {
                {"StartTime", (string)graphData["tags"][Settings.GetTag("start")]},
                {"EndTime", (string)graphData["tags"][Settings.GetTag("end")]}
            };

            if (Settings.Debug)
            {
                Console.WriteLine("Scale Down: " + times["StartTime"]);
                Console.WriteLine("Scale Up: " + times["EndTime"]);
                // log.LogInformation("Scale Down: " + times["StartTime"]);
                // log.LogInformation("Scale Up: " + times["EndTime"]);
            }

            string[] fromStamp;
            string[] toStamp;

            (var fromDay, var fromTime) = (new DayOfWeek(), new TimeSpan());

            try
            {
                fromStamp = times["StartTime"].Split(' ');

                // If stamp contains "Daily" in lieu of a day of the week, resolve as today
                if (fromStamp[0].ToLower().Equals(dailyStr))
                {
                    fromDay = DateTime.UtcNow.DayOfWeek;
                }
                else
                {
                    fromDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), fromStamp[0], true);
                }

                fromTime = Convert.ToDateTime(fromStamp[1]).TimeOfDay;
            }
            catch (Exception)
            {
                throw new ArgumentException("StartTime has an invalid format", "StartTime");
            }

            (var toDay, var toTime) = (new DayOfWeek(), new TimeSpan());

            try
            {
                toStamp = times["EndTime"].Split(' ');

                // If stamp contains "Daily" in lieu of a day of the week, resolve as today
                if (toStamp[0].ToLower().Equals(dailyStr))
                {
                    toDay = DateTime.UtcNow.DayOfWeek;
                }
                else
                {
                    toDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), toStamp[0], true);
                }

                toTime = Convert.ToDateTime(toStamp[1]).TimeOfDay;
            }
            catch (Exception)
            {
                throw new ArgumentException("EndTime has an invalid format", "EndTime");
            }

            if((fromStamp[0].ToLower() == dailyStr || toStamp[0].ToLower() == dailyStr) && (fromStamp[0].ToLower() != toStamp[0].ToLower()))
            {
                if(fromStamp[0].ToLower() != dailyStr)
                {
                    throw new ArgumentException("'Daily' identifier must be use on both start and end tags", "StartTime");
                }
                else
                {
                    throw new ArgumentException("'Daily' identifier must be use on both start and end tags", "EndTime");
                }
            }

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

            if((now > fromDate) && (now > toDate))
            {
                if(toDate > fromDate)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

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
        public async Task Run([TimerTrigger("%ENGINE_TIMER_EXPRESSION%")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("Bellhop engine starting up...");
            log.LogInformation("Current UTC Time: " + DateTime.UtcNow.ToString("dddd hh:mm:ss tt"));

            log.LogInformation("Fetching app configuration settings...");

            await _refresher.RefreshAsync();

            string debugKeyName = "debugMode";
            string debugAppSetting = _configuration.GetSection("GLOBAL")[debugKeyName];
            log.LogInformation("Debug Flag: " + debugAppSetting);

            try {
			    Settings.Debug = Boolean.Parse(debugAppSetting);
		    }
            catch {
			    Settings.Debug = false;
		    }

            Settings.SetConfig("storageAccount", _configuration.GetSection("CORE")["storageAccount"]);
            Settings.SetConfig("storageQueue", _configuration.GetSection("CORE")["storageQueue"]);
            Settings.SetConfig("tagPrefix", _configuration.GetSection("CONFIG")["tagPrefix"]);
            Settings.SetConfig("enableTag", _configuration.GetSection("CONFIG")["enableTag"]);
            Settings.SetConfig("startTimeTag", _configuration.GetSection("CONFIG")["startTimeTag"]);
            Settings.SetConfig("endTimeTag", _configuration.GetSection("CONFIG")["endTimeTag"]);
            Settings.SetConfig("setStatePrefix", _configuration.GetSection("CONFIG")["setStatePrefix"]);
            Settings.SetConfig("saveStatePrefix", _configuration.GetSection("CONFIG")["saveStatePrefix"]);

            if (Settings.Debug)
            {
                string configDataJson = JsonConvert.SerializeObject(Settings.ConfigData);
                string tagMapJson = JsonConvert.SerializeObject(Settings.TagMap);

                log.LogInformation("Config Data:");
                log.LogInformation(configDataJson);

                log.LogInformation("Tag Map:");
                log.LogInformation(tagMapJson);
            }

            if(!Settings.IsValid)
            {
                var errorString = string.Join(",", Settings.Errors.ToArray());
                var ex = new ValidationException("Missing App Configuration Settings");
                log.LogError(0, ex, $"Missing App Configuration Settings: [{errorString}]");
            }

            string strQuery = $"Resources | where tags['{Settings.GetTag("enable")}'] =~ 'True'";

            var resizeUpList = new List<JObject>();
            var resizeDownList = new List<JObject>();

            QueueClient messageQueue = getQueueClient(Settings.GetConfig("storageAccount"), Settings.GetConfig("storageQueue"));

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

            if (Settings.Debug)
            {
                string subscriptionJson = JsonConvert.SerializeObject(subscriptionIds);

                log.LogInformation("Target Subscriptions:");
                log.LogInformation(subscriptionJson);
            }

            QueryRequest request = new QueryRequest {
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
                if (Settings.Debug) log.LogInformation("=========================");
                if (Settings.Debug) log.LogInformation("Resource: " + resource["name"].ToString());

                Hashtable times = new Hashtable() {
                    {"StartTime", resource["tags"][Settings.GetTag("start")].ToString()},
                    {"EndTime", resource["tags"][Settings.GetTag("end")].ToString()}
                };

                if (Settings.Debug)
                {
                    log.LogInformation("Scale Down: " + times["StartTime"]);
                    log.LogInformation("Scale Up: " + times["EndTime"]);
                }

                Regex rg = new Regex($"{Settings.GetTag("save")}.*");

                try {
                    if (resizeTime(times))
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

                        if (Settings.Debug) log.LogInformation(scaleMessage);
                    }
                    else
                    {
                        string scaleMessage = "Currently within 'scale up' period ";

                        if (resource["tags"].Children<JProperty>().Any(prop => rg.IsMatch(prop.Name.ToString())))
                        {
                            scaleMessage += "(Scale Scheduled)";
                            resizeUpList.Add(resource);
                        }
                        else
                        {
                            scaleMessage += "(Already Scaled)";
                        }

                        if (Settings.Debug) log.LogInformation(scaleMessage);
                    }
                } catch (Exception ex) {
                    // log.LogError(0, ex, $"Error calculating resize time -- StartTime: {(string)times["StartTime"]}  EndTime: {(string)times["EndTime"]}");
                    log.LogError(0, ex, ex.GetType().ToString());
                    log.LogError(0, ex, ex.Message);
                }
            }

            if (Settings.Debug) log.LogInformation("=========================");
            log.LogInformation("Processing scale up queue...");

            foreach (var item in resizeUpList)
            {
                if (Settings.Debug) log.LogInformation("-------------------------");
                if (Settings.Debug) log.LogInformation(item["name"].ToString() + " => up");
                
                var messageData = new JObject();
                messageData.Add(new JProperty("debug", Settings.Debug));
                messageData.Add(new JProperty("direction", "up"));
                messageData.Add(new JProperty("tagMap", JObject.FromObject(Settings.TagMap)));
                messageData.Add(new JProperty("graphResults", item));

                if (Settings.Debug) log.LogInformation("Queue Message:");
                if (Settings.Debug) log.LogInformation(messageData.ToString(Formatting.None));
                if (Settings.Debug) log.LogInformation("-------------------------");

                writeQueueMessage(messageData, messageQueue);
            };

            log.LogInformation("Processing scale down queue...");

            foreach (var item in resizeDownList)
            {
                if (Settings.Debug) log.LogInformation("-------------------------");
                if (Settings.Debug) log.LogInformation(item["name"].ToString() + " => down");

                var messageData = new JObject();
                messageData.Add(new JProperty("debug", Settings.Debug));
                messageData.Add(new JProperty("direction", "down"));
                messageData.Add(new JProperty("tagMap", JObject.FromObject(Settings.TagMap)));
                messageData.Add(new JProperty("graphResults", item));

                if (Settings.Debug) log.LogInformation("Queue Message:");
                if (Settings.Debug) log.LogInformation(messageData.ToString(Formatting.None));
                if (Settings.Debug) log.LogInformation("-------------------------");

                writeQueueMessage(messageData, messageQueue);
            };

            if (Settings.Debug) log.LogInformation("=========================");
            log.LogInformation("Done processing scale queues!");

            log.LogInformation("Bellhop engine execution complete!");
        }

        public static QueueClient getQueueClient(string storName, string queueName)
        {
            ManagedIdentityCredential managedIdentityCredential = new ManagedIdentityCredential();

            QueueClientOptions queueOptions = new QueueClientOptions {
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

            (var fromDay, var fromTime) = (new DayOfWeek(), new TimeSpan());

            try {
                (fromDay, fromTime) = getActionTime((string)times["StartTime"]);
            } catch (Exception) {
                var startTimeStr = (string)times["StartTime"];
                throw new ArgumentException("StartTime has an invalid format", $"StartTime: {startTimeStr}");
            }

            (var toDay, var toTime) = (new DayOfWeek(), new TimeSpan());

            try {
                (toDay, toTime) = getActionTime((string)times["EndTime"]);
            } catch (Exception) {
                var endTimeStr = (string)times["StartTime"];
                throw new ArgumentException("EndTime has an invalid format", $"EndTime: {endTimeStr}");
            }

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
