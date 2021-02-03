using System;
using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(azure_autoscale_func.Startup))]

namespace azure_autoscale_func
{
    class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            string appConfigUri = Environment.GetEnvironmentVariable("AppConfigEndpoint");
            builder.ConfigurationBuilder.AddAzureAppConfiguration(options =>
                options.Connect(new Uri(appConfigUri), new ManagedIdentityCredential()));
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
        }
    }
}
