using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using App.Model;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace App.Logic
{
    public class Templater
    {
        private readonly ILogger _logger;
        private readonly IFileProvider _fileProvider;
        private readonly string _templatesFolder;
        private readonly string _outputFolder;
        private readonly Func<(string path, string contents), Task> _writeAllText;

        public Templater(string templatesFolder, string outputFolder, Func<(string path, string contents), Task> writeAllText = null, IFileProvider fileProvider = null, ILogger logger = null)
        {
            _fileProvider = fileProvider ?? new PhysicalFileProvider(@"\");
            _templatesFolder = templatesFolder ?? throw new ArgumentNullException(nameof(templatesFolder));
            _outputFolder = outputFolder ?? throw new ArgumentNullException(nameof(outputFolder));
            _logger = logger ?? Log.Logger;
            _writeAllText = writeAllText ?? WriteAllTextPhysical;
        }

        public async Task WriteAllTextPhysical((string path, string contents) input) =>
            await File.WriteAllTextAsync(input.path, input.contents);


        private void Validate(Deployment deployment)
        {
            var missingTemplateExceptions = deployment.Subscriptions
                .SelectMany(s => s.ResourceGroups)
                .SelectMany(r => r.ArmResources)
                .Select(a => _fileProvider.GetFileInfo(ToRelativeInTemplatesFolder(a.TemplateFilename)))
                .Where(f => !f.Exists)
                .Select(f => new Exception($"Template file '{f.PhysicalPath}' does not exist"));
            switch (missingTemplateExceptions.Count())
            {
                case 0: break;
                case 1: throw missingTemplateExceptions.Single();
                default: throw new AggregateException(missingTemplateExceptions);
            };
        }

        public async Task Generate(Deployment deployment)
        {
            Validate(deployment);
            foreach (var sub in deployment.Subscriptions)
            {
                _logger.Information($"Deploying to {sub.FriendlyName} ({sub.SubscriptionId})");
                foreach (var rg in sub.ResourceGroups)
                {
                    _logger.Information($"Deploying to {rg.ResourceGroup}");
                    foreach (var armRes in rg.ArmResources)
                    {
                        _logger.Information($"Templating {armRes.TemplateFilename} to {armRes.OutputFilename}");
                        var templateFile = ToRelativeInTemplatesFolder(armRes.TemplateFilename);
                        
                        var templateContents = await ReadAllText(templateFile);
                        var templateArm = JsonConvert.DeserializeObject<JObject>(templateContents);

                        foreach (var replacement in armRes.JsonPathReplacements)
                        {
                            var val = (JValue)templateArm.SelectToken(replacement.Key) ?? throw new Exception("Invalid replacement Json Path: " + replacement.Key);
                            val.Value = replacement.Value;
                        }
                        var templatedArm = JsonConvert.SerializeObject(templateArm, Formatting.Indented);
                        await _writeAllText((Path.Combine(_outputFolder, armRes.OutputFilename), templatedArm));

                    }
                }
            }
        }
        private string ToRelativeInTemplatesFolder(string filename) =>
            Path.IsPathRooted(_templatesFolder) ? 
                Path.GetRelativePath(@"\", Path.Combine(_templatesFolder, filename)) :
                Path.Combine(_templatesFolder, filename);



        private async Task<string> ReadAllText(string templateFile)
        {
            var file = _fileProvider.GetFileInfo(templateFile);
            if (!file.Exists) throw new Exception($"Template file '{file.PhysicalPath}' not found");
            using var readStream = file.CreateReadStream();
            using var reader = new StreamReader(readStream);
            return await reader.ReadToEndAsync();
        }
    }

    }