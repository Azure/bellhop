using System;
using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Bellhop.Startup))]

namespace Bellhop
{
    class Startup : FunctionsStartup
    {
        public IConfigurationRefresher ConfigurationRefresher { get; private set; }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            string appConfigUri = Environment.GetEnvironmentVariable("AppConfigEndpoint");
            builder.ConfigurationBuilder.AddAzureAppConfiguration(options => {
                options.Connect(new Uri(appConfigUri), new ManagedIdentityCredential())
                .ConfigureRefresh(ro =>
                {
                    ro.Register("GLOBAL:debugMode", LabelFilter.Null, refreshAll: true);
                    ro.Register("CORE:storageAccount", LabelFilter.Null, refreshAll: true);
                    ro.Register("CORE:storageQueue", LabelFilter.Null, refreshAll: true);
                    ro.Register("CONFIG:tagPrefix", LabelFilter.Null, refreshAll: true);
                    ro.Register("CONFIG:enableTag", LabelFilter.Null, refreshAll: true);
                    ro.Register("CONFIG:startTimeTag", LabelFilter.Null, refreshAll: true);
                    ro.Register("CONFIG:endTimeTag", LabelFilter.Null, refreshAll: true);
                    ro.Register("CONFIG:setStatePrefix", LabelFilter.Null, refreshAll: true);
                    ro.Register("CONFIG:saveStatePrefix", LabelFilter.Null, refreshAll: true);
                    ro.SetCacheExpiration(TimeSpan.FromSeconds(60));
                });

                ConfigurationRefresher = options.GetRefresher();
            });
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (ConfigurationRefresher != null) {
                builder.Services.AddSingleton(ConfigurationRefresher);
            }
        }
    }
}
