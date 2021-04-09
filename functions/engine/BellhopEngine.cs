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

        public static JObject TagMapObject
        {
            get
            {
                return JObject.FromObject(TagMap);
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
        // private Hashtable errors = new Hashtable();
        List<KeyValuePair<object, string>> errors = new List<KeyValuePair<object, string>>();

        public ResizeObject(JObject data)
        {
            graphData = data;
        }

        public string Name
        {
            get
            {
                return graphData["name"].ToString();
            }
        }

        public JObject GraphData
        {
            get
            {
                return graphData;
            }
        }

        public bool IsValid
        {
            get
            {
                if(errors.Count > 0)
                {
                    return false;
                }

                return true;
            }
        }

        public List<KeyValuePair<object, string>>  Errors
        {
            get
            {
                return errors;
            }
        }

        public string CurrentState
        {
            get
            {
                if(IsScaledDown())
                {
                    return "down";
                }

                return "up";
            }
        }

        public string TargetState
        {
            get
            {
                try
                {
                    if(TimeToScale())
                    {
                        return "down";
                    }

                    return "up";
                }
                catch (Exception ex)
                {
                    errors.Add(new KeyValuePair<object, string>(ex.GetType(), ex.Message));
                    return null;
                }
            }
        }

        public List<string> Debug()
        {
            Dictionary<string, string> times = new Dictionary<string, string>()
            {
                {"StartTime", (string)graphData["tags"][Settings.GetTag("start")]},
                {"EndTime", (string)graphData["tags"][Settings.GetTag("end")]}
            };

            List<string> debugMessage = new List<string>();
            string scaleMessage;

            debugMessage.Add("=========================");
            debugMessage.Add("Resource: " + graphData["name"].ToString());
            debugMessage.Add("Scale Down: " + times["StartTime"]);
            debugMessage.Add("Scale Up: " + times["EndTime"]);

            if (ResizeAction != null)
            {
                scaleMessage = "(Scale Scheduled)";
            }
            else
            {
                scaleMessage = "(Already Scaled)";
            }

            debugMessage.Add($"Currently within 'scale {TargetState}' period {scaleMessage}");
            debugMessage.Add("=========================");

            return debugMessage;
        }

        public string DebugString
        {
            get
            {
                string debugString = string.Join("\n", Debug());

                return debugString;
            }
        }

        public string ResizeAction
        {
            get
            {
                try
                {
                    if(CurrentState != TargetState)
                    {
                        return TargetState;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    errors.Add(new KeyValuePair<object, string>(ex.GetType(), ex.Message));
                    return null;
                }
            }
        }

        private bool IsScaledDown()
        {
            Regex rg = new Regex($"{Settings.GetTag("save")}.*");

            if (graphData["tags"].Children<JProperty>().Any(prop => rg.IsMatch(prop.Name.ToString())))
            {
                return true;
            }

            return false;
        }

        private bool TimeToScale()
        {
            DateTime now = DateTime.UtcNow;
            var currentDay = now.DayOfWeek;
            const string dailyStr = "daily";

            Dictionary<string, string> times = new Dictionary<string, string>()
            {
                {"StartTime", (string)graphData["tags"][Settings.GetTag("start")]},
                {"EndTime", (string)graphData["tags"][Settings.GetTag("end")]}
            };

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

            if(((now > fromDate) && (now > toDate)) || ((now < fromDate) && (now < toDate)))
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

            List<ResizeObject> resizeList = new List<ResizeObject>();

            foreach (JObject resource in resources)
            {
                ResizeObject obj = new ResizeObject(resource);

                resizeList.Add(obj);

                if (Settings.Debug)
                {
                    foreach (var line in obj.Debug())
                    {
                        log.LogInformation(line);
                    }
                }
            }

            log.LogInformation("Processing scale items...");
            if (Settings.Debug) log.LogInformation("=========================");

            var scaleItems = resizeList.Where(x => x.ResizeAction != null);

            foreach (var item in scaleItems)
            {
                if (Settings.Debug) log.LogInformation("-------------------------");
                if (Settings.Debug) log.LogInformation(item.Name + " => " + item.ResizeAction);

                var messageData = new JObject();
                messageData.Add(new JProperty("debug", Settings.Debug));
                messageData.Add(new JProperty("direction", item.ResizeAction));
                messageData.Add(new JProperty("tagMap", Settings.TagMapObject));
                messageData.Add(new JProperty("graphResults", item.GraphData));

                if (Settings.Debug) log.LogInformation("Queue Message:");
                if (Settings.Debug) log.LogInformation(messageData.ToString(Formatting.None));
                if (Settings.Debug) log.LogInformation("-------------------------");

                writeQueueMessage(messageData, messageQueue);
            };

            if (Settings.Debug) log.LogInformation("=========================");
            log.LogInformation("Done processing scale items!");

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
    }
}
