using App.Logic;
using App.Model;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class TemplaterTests
    {
        [Fact]
        public async Task Can_Load_Embedded_Resource()
        {
            var provider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            var templatePath = @"TestData\Templates\appServiceWithCName.json";
            var template = provider.GetFileInfo(templatePath);
            Assert.True(template.Exists);

            var contents = await ReadAllText(provider, templatePath);
            Assert.NotNull(contents);
            Assert.Contains("WestEurope", contents);
        }

        [Fact]
        public async Task Can_Read_Deployment()
        {
            var provider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            var deploymentJsonPath = @"TestData\Deployments\deploy1.json";
            var deployment = JsonConvert.DeserializeObject<Deployment>(await ReadAllText(provider, deploymentJsonPath));
            Assert.NotNull(deployment);
        }

        [Fact]
        public async Task Can_Execute_Templater()
        {
            var provider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());
            var deploymentJsonPath = @"TestData\Deployments\deploy1.json";
            var deployment = JsonConvert.DeserializeObject<Deployment>(await ReadAllText(provider, deploymentJsonPath));
            var output = new Dictionary<string, string>();
            Task WriteToDictionary((string path, string contents) content)
            {
                output.Add(content.path, content.contents);
                return Task.CompletedTask;
            }

            var templater = new Templater(@"TestData\Templates\", @"TestData\Output\", WriteToDictionary, provider);
            await templater.Generate(deployment);

            Assert.NotEmpty(output.Keys);
        }

        
        private async Task<string> ReadAllText(IFileProvider provider, string file)
        {
            var f = provider.GetFileInfo(file);
            if (!f.Exists) throw new Exception($"File '{f.PhysicalPath}' not found");
            using var readStream = f.CreateReadStream();
            using var reader = new StreamReader(readStream);
            return await reader.ReadToEndAsync();
        }

    }
}
