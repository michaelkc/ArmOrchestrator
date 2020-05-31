using System.Collections.Generic;

namespace App.Model
{
    public class Deployment
        {

            public IEnumerable<AzureSubscription> Subscriptions { get; set; } = new List<AzureSubscription>();

        }

    }