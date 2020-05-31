using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using App.Logic;
using App.Model;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Serilog;

namespace App
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();


            var deployment = GenerateInCode();



            var templatesFolder = Path.Combine(Environment.CurrentDirectory, "Templates");
            var outputFolder = Path.Combine(Environment.CurrentDirectory, "Output");

            var templater = new Templater(templatesFolder, outputFolder);
            await templater.Generate(deployment);

            
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
                                            {"parameters.appServiceName.defaultValue","deleteme1-dev-wa"},
                                            {"parameters.appServicePlanId.defaultValue","/subscriptions/8a3810d4-2f5b-4b66-90ca-9e96ac3e45be/resourcegroups/agroid-common-preprod-rg/providers/Microsoft.Web/serverfarms/agroid-preprod-identityserver-asp"},
                                            {"parameters.cname.defaultValue","armtemplater.segestest.dk"},
                                            {"parameters.location.defaultValue","WestEurope"}
                                        }
                                    },
                                }
                            }
                        }
                    }
                }
            };
    }
}
