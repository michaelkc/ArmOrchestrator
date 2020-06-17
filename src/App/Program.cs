using System;
using System.IO;
using System.Threading.Tasks;
using App.Logic;
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

            var deployment = DeploymentDefinition.Active();

            var templatesFolder = Path.Combine(Environment.CurrentDirectory, "Templates");
            var outputFolder = Path.Combine(Environment.CurrentDirectory, "Output");

            var templater = new Templater(templatesFolder, outputFolder);
            await templater.Generate(deployment);
            
            File.WriteAllText("deploy.json", JsonConvert.SerializeObject(deployment, Formatting.Indented));
        }
    }
}
