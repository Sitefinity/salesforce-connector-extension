using System;
using Telerik.OpenAccess.DataAdapter.Metadata;

namespace Sitefinity.SalesforceConnector.Extension
{
    public class SalesForceMetadataTransform : IMetadataTransform
    {
        SalesForceDefaultMapCapabilities capabilities;

        public SalesForceMetadataTransform(DefaultMapCapabilities capabilities)
        {
            if (capabilities == null)
            {
                throw new ArgumentNullException("capabilities");
            }

            this.capabilities = capabilities as SalesForceDefaultMapCapabilities;

            if (this.capabilities == null)
            {
                throw new InvalidOperationException("SalesForces metadata transform initialized with wrong capabilities");
            }
        }

        public Description TransformMetaList(Description sourceList)
        {
            throw new NotImplementedException();
        }

        public FieldDescription TransformMetaField(FieldDescription sourceField)
        {
            throw new NotImplementedException();
        }

        public FieldDescription GetCCField()
        {
            throw new NotImplementedException();
        }

        public FieldDescription GetPkField()
        {
            throw new NotImplementedException();
        }
    }
}