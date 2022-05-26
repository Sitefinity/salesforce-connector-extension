using System;
using Telerik.OpenAccess.DataAdapter.Converter;
using Telerik.OpenAccess.DataAdapter.Sync;

namespace Sitefinity.SalesforceConnector.Extension
{
    public class CustomDateTimeToDateConverter : DateTimeToDateConverter
    {
        protected override void Copy(SyncAdapterBase sourceAdapter, SyncAdapterBase destinationAdapter, object sourceObject, object destinationObject, string sourceFieldName, string destinationFieldName)
        {
            DateTime? destinationValue = null;

            var sourceValue = sourceAdapter.GetValue(sourceObject, sourceFieldName);
            if (sourceValue != null)
            {
                if (sourceValue is DateTime sourceDateTime)
                {
                    destinationValue = sourceDateTime.Date;
                    destinationValue = DateTime.SpecifyKind(destinationValue.Value, DateTimeKind.Utc);
                }
                else if(sourceValue is string sourceString)
                {
                    if (DateTime.TryParse(sourceString, out DateTime parsedDateTime))
                    {
                        destinationValue = parsedDateTime.Date;
                        destinationValue = DateTime.SpecifyKind(destinationValue.Value, DateTimeKind.Utc);
                    }
                }
            }

            destinationAdapter.SetValue(destinationObject, destinationValue, destinationFieldName);
        }
    }
}