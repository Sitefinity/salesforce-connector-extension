using System;
using Telerik.OpenAccess.DataAdapter;
using Telerik.Sitefinity.Abstractions;

namespace Sitefinity.SalesforceConnector.Extension
{
    public sealed class Installer
    {
        private Installer()
        {
        }

        /// <summary>
        /// Subscribe to Sitefinity events
        /// </summary>
        public static void PreApplicationStart()
        {
            Bootstrapper.Bootstrapped += Bootstrapper_Bootstrapped;
        }

        private static void Bootstrapper_Bootstrapped(object sender, EventArgs e)
        {
            if (!Factory.ContainsMetadataAdapter("UIMetadata"))
                Factory.RegisterMetadataAdapter("UIMetadata", SalesForceMetadataAdapter.CreateInstance);

            if (!Factory.ContainsMetadataAdapter(Factory.SalesForce))
                Factory.RegisterMetadataAdapter(Factory.SalesForce, SalesForceMetadataAdapter.CreateInstance);
        }
    }
}
