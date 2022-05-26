using System;
using System.Linq;
using System.Text.RegularExpressions;
using Telerik.OpenAccess.DataAdapter.Converter;
using Telerik.OpenAccess.DataAdapter.SalesForce.Sync;
using Telerik.Sitefinity.Connectivity.Adapters;
using Telerik.Sitefinity.DynamicModules.Model;

namespace Sitefinity.SalesforceConnector.Extension
{
    public class PicklistConverter : ConverterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PicklistConverter" /> class.
        /// </summary>
        public PicklistConverter() : base()
        {
            // An empty constructor is required
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PicklistConverter" /> class.
        /// </summary>
        /// <param name="isMultiChoice">Indicates if it is a multiple choice field.</param>
        public PicklistConverter(ConverterConfiguration converterConfiguration) : base()
        {
            var match = Regex.Match(converterConfiguration.Configuration, PicklistConverter.ConfigurationRegexFormat);
            if (match.Success)
            {
                this.isMultiChoice = bool.Parse(match.Groups["IsMultiChoice"].Value);
            }
        }

        /// <inheritdoc />
        public override bool CanCompare
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc />
        public override bool CanSort
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc />
        protected override int Compare(Telerik.OpenAccess.DataAdapter.Sync.SyncAdapterBase sourceAdapter, Telerik.OpenAccess.DataAdapter.Sync.SyncAdapterBase destinationAdapter, object sourceObject, object destinationObject, string sourceFieldName, string destinationFieldName)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        protected override void Copy(Telerik.OpenAccess.DataAdapter.Sync.SyncAdapterBase sourceAdapter, Telerik.OpenAccess.DataAdapter.Sync.SyncAdapterBase destinationAdapter, object sourceObject, object destinationObject, string sourceFieldName, string destinationFieldName)
        {
            object destinationValue = null;

            // Salesforce -> Sitefinity
            if (sourceAdapter is SalesForceSyncAdapter && destinationAdapter is SitefinitySyncAdapterBase)
            {
                if (isMultiChoice)
                {
                    var sourceValue = sourceAdapter.GetValue(sourceObject, sourceFieldName) as string;
                    if (sourceValue != null)
                    {
                        destinationValue = sourceValue.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                else
                {
                    destinationValue = sourceAdapter.GetValue(sourceObject, sourceFieldName) as string;
                }
            }
            // Sitefinity -> Salesforce
            else if (destinationAdapter is SalesForceSyncAdapter && sourceAdapter is SitefinitySyncAdapterBase)
            {
                if (isMultiChoice)
                {
                    var sourceValue = sourceAdapter.GetValue(sourceObject, sourceFieldName);
                    if(sourceValue != null)
                    {
                        if (sourceValue is ChoiceOption[] choicesArray)
                        {
                            destinationValue = string.Join(";", choicesArray.Select(co => co.PersistedValue));
                        }
                        else if (sourceValue is string sourceString)
                        {
                            destinationValue = sourceString.Replace(",", ";");
                        }
                        else
                        {
                            destinationValue = sourceValue;
                        }
                    }
                }
                else
                {
                    var sourceValue = sourceAdapter.GetValue(sourceObject, sourceFieldName);

                    if (sourceValue != null)
                    {
                        if (sourceValue is ChoiceOption choiceOption)
                        { 
                            destinationValue = choiceOption.PersistedValue;
                        }
                        else
                        {
                            destinationValue = sourceValue;
                        }
                    }
                }
            }

            destinationAdapter.SetValue(destinationObject, destinationValue, destinationFieldName);
        }

        internal const string ConfigurationStringFormat = "IsMultiChoice={0};;";
        private const string ConfigurationRegexFormat = @"IsMultiChoice=(?<IsMultiChoice>.+?);;";
        private bool isMultiChoice;
    }
}