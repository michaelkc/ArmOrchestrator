using System.Collections.Generic;

namespace App.Model
{
    public class AzureResourceGroup
    {
        public string ResourceGroup { get; set; }
        public string Location { get; set; } = "WestEurope";
        public IEnumerable<AzureArmResource> ArmResources { get; set; } = new List<AzureArmResource>();

    }

}