using System.Collections.Generic;
using Serilog.Core;

namespace App.Model
{
    public class AzureArmResource
    {
        public string TemplateFilename { get; set; }
        public string OutputFilename { get; set; }
        public Dictionary<string, string> JsonPathReplacements { get; set; } = new Dictionary<string, string>();
        public string[] PostDeployFunctions { get; set; } = new string[0];
    }

}