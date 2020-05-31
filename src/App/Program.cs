using System;
using System.IO;
using App.Logic;
using App.Model;
using Newtonsoft.Json;
using Serilog;

namespace App
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();


            var deployment = GenerateInCode();
            var templatesFolder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Templates"));
            var outputFolder = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "Output"));
            var templater = new Templater(Log.Logger, templatesFolder, outputFolder);
            templater.Generate(deployment);

            
            File.WriteAllText("deploy.json", JsonConvert.SerializeObject(deployment, Formatting.Indented));
            //var deployment2 = JsonConvert.DeserializeObject<Deployment>(File.ReadAllText("deploy.json"));
            //var templater2 = new Templater(deployment2, Log.Logger);
            //templater2.Generate();
        }

        private static Deployment GenerateInCode() =>
            new Deployment
            {
                Subscriptions = new[]
                {
                    new AzureSubscription
                    {
                        SubscriptionId = "9bb11523-611b-4341-8af2-56368f08b597",
                        FriendlyName = "MAC MSDN",
                        ResourceGroups = new []
                        {
                            new AzureResourceGroup
                            {
                                ResourceGroup = "test-deleteme42-rg",
                                ArmResources = new []
                                {
                                    new AzureArmResource
                                    {
                                        OutputFilename = "arm1.json",
                                        TemplateFilename = "appServiceWithCName.json",
                                        JsonPathReplacements =
                                        {
                                            {"parameters.appServiceName.defaultValue","myappservice.segestest.dk"},
                                            {"resources[0].properties.serverFarmId","somefarmid"}
                                        }
                                    },
                                    new AzureArmResource
                                    {
                                        OutputFilename = "arm2.json",
                                        TemplateFilename = "appServiceWithCName.json",
                                        JsonPathReplacements =
                                        {
                                            {"parameters.appServiceName.defaultValue","myappservice.segestest.dk"},
                                            {"resources[0].properties.serverFarmId","somefarmid"}
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
    }
}
