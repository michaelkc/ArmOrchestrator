using App.Model;

namespace App
{
    internal static partial class DeploymentDefinition
    {
        public static Deployment Active() => CvrServices();
    }
}
