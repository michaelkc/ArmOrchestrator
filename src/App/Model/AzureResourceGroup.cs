using System.Collections.Generic;

namespace App.Model
{
    public class AzureResourceGroup
        {
            public string ResourceGroup { get; set; }
            public IEnumerable<AzureArmResource> ArmResources { get; set; } = new List<AzureArmResource>();

        }

    }