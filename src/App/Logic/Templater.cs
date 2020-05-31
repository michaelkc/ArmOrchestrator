using System;
using System.IO;
using System.Linq;
using App.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace App.Logic
{
    public class Templater
    {
        private readonly ILogger _logger;
        private readonly DirectoryInfo _templatesFolder;
        private readonly DirectoryInfo _outputFolder;

        public Templater(ILogger logger, DirectoryInfo templatesFolder, DirectoryInfo outputFolder)
        {
            _logger = logger;
            _templatesFolder = templatesFolder;
            _outputFolder = outputFolder;
        }

        private void Validate(Deployment deployment)
        {
            if (_templatesFolder == null || !_templatesFolder.Exists)
            {
                throw new Exception($"'Template folder {_templatesFolder}' does not exist");
            }
            if (_outputFolder == null || !_outputFolder.Exists)
            {
                throw new Exception($"Output folder '{_outputFolder}' does not exist");
            }
            var missingTemplateExceptions = deployment.Subscriptions
                .SelectMany(s => s.ResourceGroups)
                .SelectMany(r => r.ArmResources)
                .Select(a => Path.Combine(_templatesFolder.FullName, a.TemplateFilename))
                .Where(f => !File.Exists(f))
                .Select(f => new Exception($"Template file '{f}' does not exist"));
            switch (missingTemplateExceptions.Count())
            {
                case 0: break;
                case 1: throw missingTemplateExceptions.Single();
                default: throw new AggregateException(missingTemplateExceptions);
            };
        }

        public void Generate(Deployment deployment)
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
                        var templateFile = Path.Combine(_templatesFolder.FullName, armRes.TemplateFilename);
                        var templateArm = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(templateFile));

                        foreach (var replacement in armRes.JsonPathReplacements)
                        {
                            var val = (JValue)templateArm.SelectToken(replacement.Key) ?? throw new Exception("Invalid replacement Json Path: " + replacement.Key);
                            val.Value = replacement.Value;
                        }
                        var templatedArm = JsonConvert.SerializeObject(templateArm, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(Path.Combine(_outputFolder.FullName, armRes.OutputFilename), templatedArm);

                    }
                }
            }
        }
    }

    }