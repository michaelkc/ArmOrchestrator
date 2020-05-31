using System.Collections.Generic;

namespace App.Model
{
    public class AzureSubscription
        {
            public string FriendlyName { get; set; }
            public string SubscriptionId { get; set; }
            public IEnumerable<AzureResourceGroup> ResourceGroups { get; set; } = new List<AzureResourceGroup>();
        }

    }